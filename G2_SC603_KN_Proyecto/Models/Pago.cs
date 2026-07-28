using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Pago
{
    public int idPago { get; set; }

    public int idClienteMembresia { get; set; }

    public decimal monto { get; set; }

    public DateOnly fechaPago { get; set; }

    public string? metodoPago { get; set; }

    public string? descripcion { get; set; }
    public string? comprobante { get; set; }

    public virtual ClienteMembresium idClienteMembresiaNavigation { get; set; } = null!;
}
