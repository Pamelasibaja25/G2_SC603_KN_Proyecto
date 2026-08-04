using System;

namespace G2_SC603_KN_Proyecto.Models;

public partial class ConfiguracionSinpe
{
    public int IdConfiguracion { get; set; }

    public string ImagenQr { get; set; } = null!;

    public DateTime ActualizadoEn { get; set; }
}
