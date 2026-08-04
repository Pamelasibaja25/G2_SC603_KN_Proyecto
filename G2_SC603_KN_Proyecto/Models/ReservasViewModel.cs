using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public class ReservasViewModel
{
    public bool EsAdmin { get; set; }
    public bool EsCliente { get; set; }

    /// eHistorial de confirmaciones del cliente logueado (pasadas y futuras).
    public List<ConfirmacionWodVM> MisConfirmaciones { get; set; } = new();

    /// Admin: clientes que aceptaron el WOD de hoy, con estado de asistencia física.
    public List<ConfirmacionWodVM> ConfirmadosHoy { get; set; } = new();

    /// Admin: historial completo de confirmaciones (con filtro de estado).
    public List<ConfirmacionWodVM> TodasConfirmaciones { get; set; } = new();
}

public class ConfirmacionWodVM
{
    public int IdClienteRutina { get; set; }
    public int IdRutina { get; set; }
    public string NombreWod { get; set; } = "";
    public string? Imagen { get; set; }
    public string? NombreCliente { get; set; }
    public DateOnly Fecha { get; set; }

    /// PENDIENTE, ACEPTADO o NO_ASISTE.
    public string Estado { get; set; } = "";

    /// Si ya hizo check-in físico hoy (tabla Asistencia)
    public bool AsistioHoy { get; set; }
}
