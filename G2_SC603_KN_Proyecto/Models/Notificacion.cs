namespace G2_SC603_KN_Proyecto.Models
{
    public partial class Notificacion
    {
        public int IdNotificacion { get; set; }

        public int IdCliente { get; set; }

        public string Tipo { get; set; } = null!;

        public string Titulo { get; set; } = null!;

        public string Mensaje { get; set; } = null!;

        public DateTime Fecha { get; set; }

        public bool Leida { get; set; }

    }
}
