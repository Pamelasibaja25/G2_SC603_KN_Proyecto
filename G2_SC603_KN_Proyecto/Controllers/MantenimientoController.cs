using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Controllers;

public class MantenimientoController : Controller
{
    private readonly DbOrionFitContext _context;

    private static readonly string[] TiposValidos =
    {
        "Preventivo", "Correctivo", "Calibracion", "Limpieza"
    };

    public MantenimientoController(DbOrionFitContext context)
    {
        _context = context;
    }

    public IActionResult MostrarMantenimiento()
    {
        var mantenimientos = _context.Mantenimientos
            .Include(m => m.IdEquipoNavigation)
            .OrderByDescending(m => m.Fecha)
            .ToList();

        ViewBag.Equipos = _context.Equipo
            .OrderBy(e => e.Nombre)
            .ToList();

        ViewBag.Responsable = HttpContext.Session.GetString("Usuario") ?? "—";

        return View(mantenimientos);
    }

    [HttpPost]
    public IActionResult Programar(int idEquipo, DateOnly fecha, string tipo, string? descripcion, decimal? costo, bool confirmarConflicto = false)
    {
        var equipo = _context.Equipo.FirstOrDefault(e => e.IdEquipo == idEquipo);
        if (equipo == null)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un equipo valido.";
            return RedirectToAction("MostrarMantenimiento");
        }

        if (string.IsNullOrWhiteSpace(tipo) || !TiposValidos.Contains(tipo))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un tipo de mantenimiento valido.";
            return RedirectToAction("MostrarMantenimiento");
        }

        if (fecha < DateOnly.FromDateTime(DateTime.Today))
        {
            TempData["ErrorMessage"] = "La fecha programada no es valida: no puede ser anterior a la fecha actual.";
            return RedirectToAction("MostrarMantenimiento");
        }

        bool existeConflicto = _context.Mantenimientos.Any(m =>
            m.IdEquipo == idEquipo &&
            m.Fecha == fecha &&
            m.Estado == "Programado");

        if (existeConflicto && !confirmarConflicto)
        {
            TempData["ConflictoMantenimiento"] = "true";
            TempData["ConflictoEquipo"] = idEquipo;
            TempData["ConflictoFecha"] = fecha.ToString("yyyy-MM-dd");
            TempData["ConflictoTipo"] = tipo;
            TempData["ConflictoDescripcion"] = descripcion;
            TempData["ConflictoCosto"] = costo?.ToString() ?? "";
            TempData["WarningMessage"] =
                $"Ya existe un mantenimiento programado para \"{equipo.Nombre}\" el {fecha:dd/MM/yyyy}. " +
                "Confirma si deseas continuar de todos modos.";
            return RedirectToAction("MostrarMantenimiento");
        }

        var responsable = HttpContext.Session.GetString("Usuario") ?? "Administrador";

        var descripcionFinal = string.IsNullOrWhiteSpace(descripcion)
            ? $"Programado por: {responsable}"
            : $"{descripcion} (Programado por: {responsable})";

        try
        {
            _context.Database.ExecuteSqlRaw(
                "CALL SP_AgregarMantenimiento({0}, {1}, {2}, {3}, {4}, {5})",
                idEquipo,
                tipo,
                fecha.ToDateTime(TimeOnly.MinValue),
                descripcionFinal,
                costo.HasValue ? costo.Value : (object)DBNull.Value,
                "Programado"
            );

            TempData["MensajeExito"] = $"Mantenimiento programado correctamente para \"{equipo.Nombre}\".";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al programar el mantenimiento: " + ex.Message;
        }

        return RedirectToAction("MostrarMantenimiento");
    }

    [HttpGet]
    public IActionResult VerificarConflicto(int idEquipo, DateOnly fecha)
    {
        bool tieneConflicto = _context.Mantenimientos.Any(m =>
            m.IdEquipo == idEquipo &&
            m.Fecha == fecha &&
            m.Estado == "Programado");

        return Json(new { conflicto = tieneConflicto });
    }


    [HttpPost]
    public IActionResult RegistrarRealizado(int? idMantenimientoProgramado, int idEquipo, DateOnly fecha, string tipo, string? descripcion, decimal? costo)
    {
        var equipo = _context.Equipo.FirstOrDefault(e => e.IdEquipo == idEquipo);
        if (equipo == null)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un equipo valido.";
            return RedirectToAction("MostrarMantenimiento");
        }

        if (string.IsNullOrWhiteSpace(tipo) || !TiposValidos.Contains(tipo))
        {
            TempData["ErrorMessage"] = "Debe indicar la fecha y el tipo de mantenimiento realizado.";
            return RedirectToAction("MostrarMantenimiento");
        }

        var responsable = HttpContext.Session.GetString("Usuario") ?? "Administrador";
        var descripcionFinal = string.IsNullOrWhiteSpace(descripcion)
            ? $"Completado por: {responsable}"
            : $"{descripcion} (Completado por: {responsable})";

        if (idMantenimientoProgramado.HasValue)
        {
            var programado = _context.Mantenimientos.FirstOrDefault(m => m.IdMantenimiento == idMantenimientoProgramado.Value);
            if (programado != null)
            {
                try
                {
                    _context.Database.ExecuteSqlRaw(
                        "CALL SP_CompletarMantenimiento({0}, {1}, {2}, {3}, {4})",
                        idMantenimientoProgramado.Value,
                        tipo,
                        fecha.ToDateTime(TimeOnly.MinValue),
                        descripcionFinal,
                        costo.HasValue ? costo.Value : (object)DBNull.Value
                    );

                    TempData["MensajeExito"] = $"Mantenimiento programado para \"{equipo.Nombre}\" marcado como completado.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error al completar el mantenimiento: " + ex.Message;
                }

                return RedirectToAction("MostrarMantenimiento");
            }
        }

        try
        {
            _context.Database.ExecuteSqlRaw(
                "CALL SP_AgregarMantenimiento({0}, {1}, {2}, {3}, {4}, {5})",
                idEquipo,
                tipo,
                fecha.ToDateTime(TimeOnly.MinValue),
                descripcionFinal,
                costo.HasValue ? costo.Value : (object)DBNull.Value,
                "Completado"
            );

            TempData["MensajeExito"] = $"Mantenimiento registrado en el historial de \"{equipo.Nombre}\".";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al registrar el mantenimiento: " + ex.Message;
        }

        return RedirectToAction("MostrarMantenimiento");
    }

    [HttpGet]
    public IActionResult Historial(int id)
    {
        var equipo = _context.Equipo.FirstOrDefault(e => e.IdEquipo == id);
        if (equipo == null)
        {
            TempData["ErrorMessage"] = "Equipo no encontrado.";
            return RedirectToAction("MostrarMantenimiento");
        }

        var historial = _context.Mantenimientos
            .Where(m => m.IdEquipo == id)
            .OrderByDescending(m => m.Fecha)
            .ToList();

        ViewBag.Equipo = equipo;
        return View(historial);
    }

    [HttpGet]
    public IActionResult Detalle(int id)
    {
        var mantenimiento = _context.Mantenimientos
            .Include(m => m.IdEquipoNavigation)
            .FirstOrDefault(m => m.IdMantenimiento == id);

        if (mantenimiento == null)
        {
            TempData["ErrorMessage"] = "Mantenimiento no encontrado.";
            return RedirectToAction("MostrarMantenimiento");
        }

        return Json(new
        {
            equipo = mantenimiento.IdEquipoNavigation?.Nombre,
            tipo = mantenimiento.Tipo,
            fecha = mantenimiento.Fecha.ToString("dd/MM/yyyy"),
            estado = mantenimiento.Estado,
            costo = mantenimiento.Costo,
            descripcion = mantenimiento.Descripcion
        });
    }

    [HttpGet]
    public IActionResult Disponibilidad(DateOnly? fecha)
    {
        var fechaConsulta = fecha ?? DateOnly.FromDateTime(DateTime.Today);

        var equipos = _context.Equipo.ToList();

        var idsConMantenimiento = _context.Mantenimientos
            .Where(m => m.Fecha == fechaConsulta && m.Estado == "Programado")
            .Select(m => m.IdEquipo)
            .ToHashSet();

        var disponibles = equipos
            .Where(e => e.Estado == "Disponible" && !idsConMantenimiento.Contains(e.IdEquipo))
            .ToList();

        var noDisponibles = equipos
            .Where(e => e.Estado != "Disponible" || idsConMantenimiento.Contains(e.IdEquipo))
            .Select(e => new EquipoNoDisponibleViewModel
            {
                Equipo = e,
                Motivo = e.Estado != "Disponible"
                    ? "Equipo inactivo (" + e.Estado + ")"
                    : "Mantenimiento programado para esta fecha"
            })
            .ToList();

        ViewBag.FechaConsulta = fechaConsulta;
        ViewBag.Disponibles = disponibles;
        ViewBag.NoDisponibles = noDisponibles;

        return View();
    }

    [HttpPost]
    public IActionResult EditarMantenimiento(int idMantenimiento, int idEquipo, DateOnly fecha, string tipo, string? descripcion, decimal? costo, string estado)
    {
        var mantenimiento = _context.Mantenimientos.FirstOrDefault(m => m.IdMantenimiento == idMantenimiento);
        if (mantenimiento == null)
        {
            TempData["ErrorMessage"] = "Mantenimiento no encontrado.";
            return RedirectToAction("MostrarMantenimiento");
        }

        var equipo = _context.Equipo.FirstOrDefault(e => e.IdEquipo == idEquipo);
        if (equipo == null)
        {
            TempData["ErrorMessage"] = "Debe seleccionar un equipo valido.";
            return RedirectToAction("MostrarMantenimiento");
        }

        if (string.IsNullOrWhiteSpace(tipo) || !TiposValidos.Contains(tipo))
        {
            TempData["ErrorMessage"] = "Debe seleccionar un tipo de mantenimiento valido.";
            return RedirectToAction("MostrarMantenimiento");
        }

        try
        {
            _context.Database.ExecuteSqlRaw(
                "CALL SP_EditarMantenimiento({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                idMantenimiento,
                idEquipo,
                tipo,
                fecha.ToDateTime(TimeOnly.MinValue),
                descripcion ?? (object)DBNull.Value,
                costo.HasValue ? costo.Value : (object)DBNull.Value,
                estado
            );

            TempData["MensajeExito"] = "Mantenimiento actualizado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al actualizar el mantenimiento: " + ex.Message;
        }

        return RedirectToAction("MostrarMantenimiento");
    }

    [HttpPost]
    public IActionResult EliminarMantenimiento(int idMantenimiento)
    {
        var mantenimiento = _context.Mantenimientos.FirstOrDefault(m => m.IdMantenimiento == idMantenimiento);
        if (mantenimiento == null)
        {
            TempData["ErrorMessage"] = "Mantenimiento no encontrado.";
            return RedirectToAction("MostrarMantenimiento");
        }

        try
        {
            _context.Database.ExecuteSqlRaw(
                "CALL SP_EliminarMantenimiento({0})",
                idMantenimiento
            );

            TempData["MensajeExito"] = "Mantenimiento eliminado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error al eliminar el mantenimiento: " + ex.Message;
        }

        return RedirectToAction("MostrarMantenimiento");
    }
}

public class EquipoNoDisponibleViewModel
{
    public Equipo Equipo { get; set; } = null!;
    public string Motivo { get; set; } = "";
}