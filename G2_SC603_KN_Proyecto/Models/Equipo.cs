<<<<<<< HEAD
﻿using System.ComponentModel.DataAnnotations;
namespace G2_SC603_KN_Proyecto.Models
{
    public class Equipo
    {
        [Key]
        public int id_equipo { get; set; }

        public string nombre { get; set; }

        public string estado { get; set; }

        public DateTime? fecha_compra { get; set; }

        public decimal costo { get; set; }
    }
=======
﻿using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Equipo
{
    public int IdEquipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateOnly? FechaCompra { get; set; }

    public decimal? Costo { get; set; }

    public virtual ICollection<Ejercicio> Ejercicios { get; set; } = new List<Ejercicio>();

    public virtual ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
>>>>>>> a0d6b9f7ad3afdbd119de867bbd9e7725ddd9f4b
}
