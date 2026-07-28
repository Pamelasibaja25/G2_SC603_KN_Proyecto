using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Equipo
{
    public int idEquipo { get; set; }

    public string nombre { get; set; } = null!;

    public string estado { get; set; } = null!;

    public DateOnly? fechaCompra { get; set; }

    public decimal? costo { get; set; }

    public virtual ICollection<Ejercicio> ejercicios { get; set; } = new List<Ejercicio>();

    public virtual ICollection<Mantenimiento> mantenimientos { get; set; } = new List<Mantenimiento>();
}
