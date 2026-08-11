using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public class ReservasViewModel
{
    public bool EsAdmin { get; set; }
    public bool EsCliente { get; set; }

    /// <summary>Historial de confirmaciones del cliente logueado (pasadas y futuras).</summary>
    public List<ConfirmacionWodVM> MisConfirmaciones { get; set; } = new();

    /// <summary>Admin: clientes que aceptaron el WOD de hoy, con estado de asistencia física.</summary>
    public List<ConfirmacionWodVM> ConfirmadosHoy { get; set; } = new();

    /// <summary>Admin: historial completo de confirmaciones (con filtro de estado).</summary>
    public List<ConfirmacionWodVM> TodasConfirmaciones { get; set; } = new();

    /// <summary>Admin: vista tipo calendario, un bloque por día (hoy + próximos 6 días).</summary>
    public List<ConfirmadosDiaVM> Calendario { get; set; } = new();
}

public class ConfirmacionWodVM
{
    public int IdClienteRutina { get; set; }
    public int IdRutina { get; set; }
    public string NombreWod { get; set; } = "";
    public string? Imagen { get; set; }
    public string? NombreCliente { get; set; }
    public DateOnly Fecha { get; set; }

    /// <summary>PENDIENTE, ACEPTADO o NO_ASISTE.</summary>
    public string Estado { get; set; } = "";

    /// <summary>Si ya hizo check-in físico hoy (tabla Asistencia).</summary>
    public bool AsistioHoy { get; set; }
}
