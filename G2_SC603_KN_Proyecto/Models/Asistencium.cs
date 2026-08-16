using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Asistencium
{
    public int IdAsistencia { get; set; }

    public int IdCliente { get; set; }

    /// <summary>A qué confirmación de WOD/horario corresponde este check-in.
    /// Un cliente puede tener varios check-in el mismo día si confirmó más
    /// de un WOD (ej: mañana y tarde).</summary>
    public int? IdClienteRutina { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly HoraEntrada { get; set; }

    public TimeOnly? HoraSalida { get; set; }

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual ClienteRutina? IdClienteRutinaNavigation { get; set; }
}
