using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2_SC603_KN_Proyecto.Models;

public partial class HistorialMembresia
{
    [Key]
    [Column("id_historial")]
    public int IdHistorial { get; set; }

    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [Column("id_membresia")]
    public int IdMembresia { get; set; }
    public string Membresia { get; set; } = string.Empty;

    [Column("fecha_inicio")]
    public DateOnly FechaInicio { get; set; }

    [Column("fecha_fin")]
    public DateOnly FechaFin { get; set; }

    public virtual Cliente Cliente { get; set; }

}
