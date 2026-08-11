using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Pago
{
    public int IdPago { get; set; }

    public int IdClienteMembresia { get; set; }

    public decimal Monto { get; set; }

    public DateOnly FechaPago { get; set; }

    public string? MetodoPago { get; set; }

    public string? Descripcion { get; set; }

    public string? ComprobantePago { get; set; }

    /// <summary>Pendiente, Verificado o Rechazado. Los pagos del admin nacen Verificados; los del cliente (SINPE/Efectivo) nacen Pendientes.</summary>
    public string EstadoVerificacion { get; set; } = "Verificado";

    public virtual ClienteMembresium IdClienteMembresiaNavigation { get; set; } = null!;
}
