using ClosedXML.Excel;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class ReportesController : Controller
    {
        private readonly DbOrionFitContext _context;

        public ReportesController(DbOrionFitContext context)
        {
            _context = context;
        }

        public ActionResult Index(DateOnly? reportStartDate, DateOnly? reportEndDate,
            string? membresiaFilter, string? metodoFilter, string? exportar)
        {
            var listamembresias = _context.Membresia.ToList();
            ViewBag.Membresias = listamembresias;

            var reporte = (from c in _context.Clientes
                           join cm in _context.ClienteMembresia on c.IdCliente equals cm.IdCliente
                           join m in _context.Membresia on cm.IdMembresia equals m.IdMembresia
                           join p in _context.Pagos on cm.IdClienteMembresia equals p.IdClienteMembresia
                           select new ReporteAnalisis
                           {
                               IdCliente = c.IdCliente,
                               Cedula = c.Cedula,
                               NombreCliente = c.Nombre,
                               Telefono = c.Telefono,
                               Correo = c.Correo,
                               Membresia = m.Nombre,
                               Precio = m.Precio,
                               FechaInicio = cm.FechaInicio,
                               FechaFin = cm.FechaFin,
                               Estado = cm.Estado,
                               MetodoPago = p.MetodoPago,
                               FechaPago = p.FechaPago,
                               Monto = p.Monto
                           });

            if (!string.IsNullOrEmpty(membresiaFilter) && membresiaFilter != "Todos")
                reporte = reporte.Where(x => x.Membresia == membresiaFilter);

            if (!string.IsNullOrEmpty(metodoFilter) && metodoFilter != "Todos")
                reporte = reporte.Where(x => x.MetodoPago == metodoFilter);

            if (reportStartDate.HasValue)
                reporte = reporte.Where(x => x.FechaPago >= reportStartDate.Value);

            if (reportEndDate.HasValue)
                reporte = reporte.Where(x => x.FechaPago <= reportEndDate.Value);

            var datos = reporte.ToList();

            ViewBag.MembresiasActivas = _context.ClienteMembresia
                .Count(x => x.Estado == "Activa");

            ViewBag.MembresiasVencidas = _context.ClienteMembresia
                .Count(x => x.Estado == "Vencida");

            if ((exportar == "excel" || exportar == "pdf") && !datos.Any())
            {
                TempData["WarningMessage"] = "No hay datos para exportar con los filtros seleccionados.";
                return RedirectToAction("Index");
            }

            if (exportar == "excel")
                return ExportarIngresosExcel(datos);

            if (exportar == "pdf")
                return ExportarIngresosPdf(datos);

            return View(datos);
        }

        private FileResult ExportarIngresosExcel(List<ReporteAnalisis> datos)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte Ingresos");
            ws.Cell(1, 1).Value = "Cédula";
            ws.Cell(1, 2).Value = "Cliente";
            ws.Cell(1, 3).Value = "Teléfono";
            ws.Cell(1, 4).Value = "Correo";
            ws.Cell(1, 5).Value = "Membresía";
            ws.Cell(1, 6).Value = "Precio";
            ws.Cell(1, 7).Value = "Fecha Inicio";
            ws.Cell(1, 8).Value = "Fecha Fin";
            ws.Cell(1, 9).Value = "Estado";
            ws.Cell(1, 10).Value = "Método Pago";
            ws.Cell(1, 11).Value = "Fecha Pago";
            ws.Cell(1, 12).Value = "Monto";

            int fila = 2;
            foreach (var item in datos)
            {
                ws.Cell(fila, 1).Value = item.Cedula;
                ws.Cell(fila, 2).Value = item.NombreCliente;
                ws.Cell(fila, 3).Value = item.Telefono;
                ws.Cell(fila, 4).Value = item.Correo;
                ws.Cell(fila, 5).Value = item.Membresia;
                ws.Cell(fila, 6).Value = item.Precio;
                ws.Cell(fila, 7).Value = item.FechaInicio.ToString();
                ws.Cell(fila, 8).Value = item.FechaFin.ToString();
                ws.Cell(fila, 9).Value = item.Estado;
                ws.Cell(fila, 10).Value = item.MetodoPago;
                ws.Cell(fila, 11).Value = item.FechaPago.ToString();
                ws.Cell(fila, 12).Value = item.Monto;
                fila++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_Ingresos_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private FileResult ExportarIngresosPdf(List<ReporteAnalisis> datos)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text("Reporte de Ingresos - OrionFit")
                        .SemiBold().FontSize(14).AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1.5f);
                            cols.RelativeColumn(1.5f);
                        });

                        // Encabezados
                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Cliente", "Membresía", "Método", "Fecha Pago", "Monto", "Estado" })
                            {
                                header.Cell()
                                    .Background("#333333")
                                    .Padding(4)
                                    .Text(h)
                                    .FontColor("#ffffff")
                                    .SemiBold();
                            }
                        });

                        // Filas
                        bool alt = false;
                        foreach (var item in datos)
                        {
                            var bg = alt ? "#f5f5f5" : "#ffffff";
                            table.Cell().Background(bg).Padding(4).Text(item.NombreCliente);
                            table.Cell().Background(bg).Padding(4).Text(item.Membresia);
                            table.Cell().Background(bg).Padding(4).Text(item.MetodoPago);
                            table.Cell().Background(bg).Padding(4).Text(item.FechaPago.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bg).Padding(4).Text($"₡{item.Monto:N2}");
                            table.Cell().Background(bg).Padding(4).Text(item.Estado);
                            alt = !alt;
                        }
                    });

                    page.Footer().AlignRight()
                        .Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf",
                $"Reporte_Ingresos_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }

        public IActionResult Inventario(string? exportar)
        {
            var equipos = _context.Equipo.ToList();

            if (!equipos.Any())
            {
                ViewBag.Vacio = true;
                return View(equipos);
            }

            if (exportar == "excel")
                return ExportarInventarioExcel(equipos);

            if (exportar == "pdf")
                return ExportarInventarioPdf(equipos);

            return View(equipos);
        }

        private FileResult ExportarInventarioExcel(List<Equipo> equipos)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Inventario Equipos");
            ws.Cell(1, 1).Value = "Nombre";
            ws.Cell(1, 2).Value = "Estado";
            ws.Cell(1, 3).Value = "Fecha Compra";
            ws.Cell(1, 4).Value = "Costo";

            int fila = 2;
            foreach (var eq in equipos)
            {
                ws.Cell(fila, 1).Value = eq.Nombre;
                ws.Cell(fila, 2).Value = eq.Estado;
                ws.Cell(fila, 3).Value = eq.FechaCompra?.ToString() ?? "";
                ws.Cell(fila, 4).Value = eq.Costo ?? 0;
                fila++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reporte_Inventario_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private FileResult ExportarInventarioPdf(List<Equipo> equipos)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var activos = equipos.Count(e => e.Estado == "Disponible");
            var inactivos = equipos.Count(e => e.Estado == "No Disponible");
            var enMant = equipos.Count(e => e.Estado != "Disponible" && e.Estado != "No Disponible");

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Reporte de Inventario - OrionFit")
                            .SemiBold().FontSize(16).AlignCenter();
                        col.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).AlignCenter();
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingVertical(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Activos: {activos}").SemiBold();
                            row.RelativeItem().Text($"Inactivos: {inactivos}").SemiBold();
                            row.RelativeItem().Text($"En Mantenimiento: {enMant}").SemiBold();
                            row.RelativeItem().Text($"Total: {equipos.Count}").SemiBold();
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Nombre", "Estado", "Fecha Compra", "Costo" })
                                {
                                    header.Cell()
                                        .Background("#333333")
                                        .Padding(5)
                                        .Text(h)
                                        .FontColor("#ffffff")
                                        .SemiBold();
                                }
                            });

                            bool alt = false;
                            foreach (var eq in equipos)
                            {
                                var bg = alt ? "#f5f5f5" : "#ffffff";
                                table.Cell().Background(bg).Padding(4).Text(eq.Nombre);
                                table.Cell().Background(bg).Padding(4).Text(eq.Estado);
                                table.Cell().Background(bg).Padding(4).Text(eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "—");
                                table.Cell().Background(bg).Padding(4).Text(eq.Costo.HasValue ? $"₡{eq.Costo:N2}" : "—");
                                alt = !alt;
                            }
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf",
                $"Reporte_Inventario_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }

        // ===================== Reportes existentes =====================
        #region Reporte Membresia
        public IActionResult Membresia(string estado = "")
        {
            var reporte = (from c in _context.Clientes
                           join cm in _context.ClienteMembresia on c.IdCliente equals cm.IdCliente
                           join m in _context.Membresia on cm.IdMembresia equals m.IdMembresia
                           select new ReporteMembresiaViewModel
                           {
                               IdCliente = c.IdCliente,
                               Cedula = c.Cedula,
                               NombreCliente = c.Nombre,
                               Telefono = c.Telefono,
                               Correo = c.Correo,
                               Membresia = m.Nombre,
                               Precio = m.Precio,
                               FechaInicio = cm.FechaInicio,
                               FechaFin = cm.FechaFin,
                               Estado = cm.Estado
                           });

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                reporte = reporte.Where(x => x.Estado == estado);

            if (!string.IsNullOrEmpty(estado))
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Reporte Membresías");
                ws.Cell(1, 1).Value = "Cédula"; ws.Cell(1, 2).Value = "Cliente";
                ws.Cell(1, 3).Value = "Teléfono"; ws.Cell(1, 4).Value = "Correo";
                ws.Cell(1, 5).Value = "Membresía"; ws.Cell(1, 6).Value = "Precio";
                ws.Cell(1, 7).Value = "Fecha Inicio"; ws.Cell(1, 8).Value = "Fecha Fin";
                ws.Cell(1, 9).Value = "Estado";

                int fila = 2;
                foreach (var item in reporte)
                {
                    ws.Cell(fila, 1).Value = item.Cedula;
                    ws.Cell(fila, 2).Value = item.NombreCliente;
                    ws.Cell(fila, 3).Value = item.Telefono;
                    ws.Cell(fila, 4).Value = item.Correo;
                    ws.Cell(fila, 5).Value = item.Membresia;
                    ws.Cell(fila, 6).Value = item.Precio;
                    ws.Cell(fila, 7).Value = item.FechaInicio.ToString();
                    ws.Cell(fila, 8).Value = item.FechaFin.ToString();
                    ws.Cell(fila, 9).Value = item.Estado;
                    fila++;
                }
                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Reporte_Membresias_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }

            return View(reporte.ToList());
        }
        #endregion

        #region Reporte Cliente
        public IActionResult Cliente(string estado = "")
        {
            var reporte = (from c in _context.Clientes
                           select new Cliente
                           {
                               IdCliente = c.IdCliente,
                               Cedula = c.Cedula,
                               Nombre = c.Nombre,
                               Telefono = c.Telefono,
                               Correo = c.Correo,
                               Estado = c.Estado
                           });

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                reporte = reporte.Where(x => x.Estado == estado);

            if (!string.IsNullOrEmpty(estado))
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Reporte Clientes");
                ws.Cell(1, 1).Value = "Cédula"; ws.Cell(1, 2).Value = "Cliente";
                ws.Cell(1, 3).Value = "Teléfono"; ws.Cell(1, 4).Value = "Correo";
                ws.Cell(1, 5).Value = "Estado";

                int fila = 2;
                foreach (var item in reporte)
                {
                    ws.Cell(fila, 1).Value = item.Cedula;
                    ws.Cell(fila, 2).Value = item.Nombre;
                    ws.Cell(fila, 3).Value = item.Telefono;
                    ws.Cell(fila, 4).Value = item.Correo;
                    ws.Cell(fila, 5).Value = item.Estado;
                    fila++;
                }
                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Reporte_Clientes_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }

            return View(reporte.ToList());
        }
        #endregion
    }
}