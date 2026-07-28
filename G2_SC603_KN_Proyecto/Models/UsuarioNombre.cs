using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class UsuarioNombre
{

    public string username { get; set; } = null!;

    public string? nombre { get; set; } = null!;
    public string rol { get; set; } = null!;
    public string? telefono { get; set; } = null!;

    public string? correo { get; set; } = null!;
}
