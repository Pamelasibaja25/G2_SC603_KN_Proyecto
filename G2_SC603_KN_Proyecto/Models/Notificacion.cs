namespace G2_SC603_KN_Proyecto.Models
{
    public partial class Notificacion
    {
        public int idNotificacion { get; set; }

        public int idCliente { get; set; }

        public string tipo { get; set; } = null!;

        public string titulo { get; set; } = null!;

        public string mensaje { get; set; } = null!;

        public DateTime fecha { get; set; }

        public bool leida { get; set; }

    }
}
