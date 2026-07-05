using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class MembresiaProximaVencer
{

    public int IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Membresia { get; set; } = string.Empty;
    public DateOnly? FechaFin { get; set; }
    public int DiasRestantes { get; set; }
}
