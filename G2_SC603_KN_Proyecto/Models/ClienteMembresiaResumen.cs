using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2_SC603_KN_Proyecto.Models;

public partial class ClienteMembresiaResumen
{
    public string Cliente { get; set; }

    public string TipoPlan { get; set; }


    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public decimal? Precio { get; set; }

    /// <summary>Lo que realmente pagó el cliente en su último pago verificado
    /// (puede ser distinto de Precio si pagó varios meses de una vez). Se
    /// completa aparte en el controller, no viene del SP.</summary>
    [NotMapped]
    public decimal? MontoPagado { get; set; }

    public string Estado { get; set; } = null!;

    public int? IdCliente { get; set; }

    public int? IdMembresia { get; set; }

    /// <summary>Cuántos meses se pagaron de una — solo se usa al crear una
    /// mensualidad nueva, para registrar el pago correspondiente.</summary>
    [NotMapped]
    public int? Meses { get; set; }

}
