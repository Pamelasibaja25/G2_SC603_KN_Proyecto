using System;
using System.Collections.Generic;

namespace G2_SC603_KN_Proyecto.Models;

public partial class Usuario
{
    public int idUsuario { get; set; }

    public string username { get; set; } = null!;

    public string contrasena { get; set; } = null!;

    public string rol { get; set; } = null!;

    public virtual ICollection<Administrador> administradors { get; set; } = new List<Administrador>();

    public virtual ICollection<Cliente> clientes { get; set; } = new List<Cliente>();

    public virtual ICollection<Entrenador> entrenadors { get; set; } = new List<Entrenador>();
}
