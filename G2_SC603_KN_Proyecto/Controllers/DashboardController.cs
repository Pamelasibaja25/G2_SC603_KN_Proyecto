using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class DashboardController : Controller
{
    private readonly DbOrionFitContext _context;

    public DashboardController(DbOrionFitContext context)
    {
        _context = context;
    }

    public IActionResult Dashboard(DateOnly? fechaInicio, DateOnly? fechaFin,
        DateOnly? rankingInicio, DateOnly? rankingFin)
    {
        DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
        DateOnly inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

        DashboardViewModel model = new DashboardViewModel();

        model.ClientesActivos = _context.Clientes.Count(c => c.Estado == "Activo");

        model.IngresosMes = _context.Pagos
            .Where(p => p.FechaPago >= inicioMes && p.EstadoVerificacion == "Verificado")
            .Sum(p => (decimal?)p.Monto) ?? 0;

        model.AsistenciaHoy = _context.Asistencia.Count(a => a.Fecha == hoy);

        DateOnly manana = hoy.AddDays(1);
        model.ConfirmadosWodManana = _context.ClienteRutinas
            .Count(cr => cr.FechaAsignacion == manana && cr.EstadoAsistencia == "ACEPTADO");

        var confirmacionesFlat = _context.ClienteRutinas
            .Include(cr => cr.IdRutinaNavigation)
            .Include(cr => cr.IdClienteNavigation)
            .Where(cr => cr.FechaAsignacion >= hoy && cr.EstadoAsistencia == "ACEPTADO"
                && cr.IdClienteNavigation.Estado == "Activo")
            .Select(cr => new
            {
                cr.FechaAsignacion,
                NombreWod = cr.IdRutinaNavigation.Nombre,
                NombreCliente = cr.IdClienteNavigation.Nombre,
                cr.IdCliente
            })
            .ToList();

        var idsClientesHoy = confirmacionesFlat
            .Where(c => c.FechaAsignacion == hoy)
            .Select(c => c.IdCliente)
            .Distinct()
            .ToList();

        var idsClientesQueYaIngresaronHoy = idsClientesHoy.Count == 0
            ? new List<int>()
            : _context.Asistencia
                .Where(a => a.Fecha == hoy && idsClientesHoy.Contains(a.IdCliente))
                .Select(a => a.IdCliente)
                .ToList();

        model.ConfirmadosPorDia = confirmacionesFlat
            .GroupBy(c => c.FechaAsignacion)
            .Select(g =>
            {
                List<string> wods = g.Select(c => c.NombreWod).Distinct().ToList();

                List<ClienteConfirmadoVM> clientes = g
                    .GroupBy(c => new { c.IdCliente, c.NombreCliente })
                    .Select(cg => new ClienteConfirmadoVM
                    {
                        Nombre = cg.Key.NombreCliente,
                        YaIngreso = g.Key == hoy && idsClientesQueYaIngresaronHoy.Contains(cg.Key.IdCliente),
                        ConfirmoPorWod = wods.Select(w => cg.Any(x => x.NombreWod == w)).ToList()
                    })
                    .OrderBy(c => c.Nombre)
                    .ToList();

                return new ConfirmadosDiaVM
                {
                    Fecha = g.Key,
                    Wods = wods,
                    Clientes = clientes
                };
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        model.MembresiasPorVencer = _context.ClienteMembresia
            .Count(c => c.FechaFin >= hoy && c.FechaFin <= hoy.AddDays(7));

        model.MembresiasPendientesDePago = _context.ClienteMembresia
            .Count(c => c.Estado == "Pendiente");

        model.AsistenciaSemanal = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var fecha = hoy.AddDays(-6 + i);
                return new AsistenciaSemanalVM
                {
                    Dia = fecha.ToString("ddd"),
                    Cantidad = _context.Asistencia.Count(a => a.Fecha == fecha)
                };
            }).ToList();

        model.AsistenciaMensual = Enumerable.Range(0, 30)
            .Select(i =>
            {
                var fecha = hoy.AddDays(-29 + i);
                return new AsistenciaSemanalVM
                {
                    Dia = fecha.ToString("dd/MM"),
                    Cantidad = _context.Asistencia.Count(a => a.Fecha == fecha)
                };
            }).ToList();

        model.FechaInicio = fechaInicio;
        model.FechaFin = fechaFin;
        model.AsistenciaRango = new List<AsistenciaSemanalVM>();

        if (fechaInicio.HasValue && fechaFin.HasValue)
        {
            if (fechaInicio > fechaFin)
                model.RangoInvalido = true;
            else
            {
                int dias = fechaFin.Value.DayNumber - fechaInicio.Value.DayNumber + 1;
                model.AsistenciaRango = Enumerable.Range(0, dias)
                    .Select(i =>
                    {
                        var fecha = fechaInicio.Value.AddDays(i);
                        return new AsistenciaSemanalVM
                        {
                            Dia = fecha.ToString("dd/MM"),
                            Cantidad = _context.Asistencia.Count(a => a.Fecha == fecha)
                        };
                    }).ToList();
            }
        }

        model.Vencimientos = _context.ClienteMembresia
            .Include(x => x.IdClienteNavigation)
            .Where(x => x.FechaFin >= hoy && x.FechaFin <= hoy.AddDays(7))
            .OrderBy(x => x.FechaFin)
            .Select(x => new VencimientoVM
            {
                IdCliente = x.IdClienteNavigation.IdCliente,
                Cliente = x.IdClienteNavigation.Nombre,
                FechaFin = x.FechaFin,
                DiasRestantes = EF.Functions.DateDiffDay(hoy, x.FechaFin)
            }).ToList();

        model.RankingInicio = rankingInicio;
        model.RankingFin = rankingFin;

        IQueryable<Asistencium> queryRanking = _context.Asistencia
            .Include(a => a.IdClienteNavigation);

        if (rankingInicio.HasValue && rankingFin.HasValue && rankingInicio <= rankingFin)
            queryRanking = queryRanking.Where(a => a.Fecha >= rankingInicio && a.Fecha <= rankingFin);

        model.RankingClientes = queryRanking
            .GroupBy(a => new { a.IdClienteNavigation.IdCliente, a.IdClienteNavigation.Nombre })
            .Select(x => new RankingClienteVM
            {
                Cliente = x.Key.Nombre,
                Asistencias = x.Count()
            })
            .OrderByDescending(x => x.Asistencias)
            .Take(10)
            .ToList();

        model.IngresosHoy = _context.Pagos
            .Where(x => x.FechaPago == hoy && x.EstadoVerificacion == "Verificado")
            .Sum(x => (decimal?)x.Monto) ?? 0;

        model.PagosHoy = _context.Pagos
            .Include(p => p.IdClienteMembresiaNavigation)
                .ThenInclude(cm => cm.IdClienteNavigation)
            .Where(p => p.FechaPago == hoy && p.EstadoVerificacion == "Verificado")
            .Select(p => new PagoHoyVM
            {
                Cliente = p.IdClienteMembresiaNavigation.IdClienteNavigation.Nombre,
                Monto = p.Monto,
                Metodo = p.MetodoPago
            }).ToList();

        model.AlertasStock = _context.Inventarios
            .Where(i => i.Cantidad <= i.StockMinimo)
            .Select(i => new AlertaStockVM
            {
                Producto = i.NombreProducto,
                CantidadActual = i.Cantidad,
                StockMinimo = i.StockMinimo
            }).ToList();

        return View(model);
    }
}