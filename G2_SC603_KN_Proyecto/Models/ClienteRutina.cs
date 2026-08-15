using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class ClienteRutina
{
    public int IdClienteRutina { get; set; }

    public int IdCliente { get; set; }

    public int IdRutina { get; set; }

    public DateOnly FechaAsignacion { get; set; }

    public string EstadoAsistencia { get; set; } = "PENDIENTE";

    /// <summary>Horarios elegidos por el cliente para asistir, separados por coma
    /// (ej: "6 AM,7 PM"). Puede elegir más de uno, igual que en la encuesta de WhatsApp.</summary>
    public string? Horarios { get; set; }

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual Rutina IdRutinaNavigation { get; set; } = null!;
}
