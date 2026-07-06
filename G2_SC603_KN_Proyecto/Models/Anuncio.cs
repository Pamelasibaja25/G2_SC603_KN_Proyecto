using System.ComponentModel.DataAnnotations;

namespace G2_SC603_KN_Proyecto.Models
{
    public class Anuncio
    {
        [Key]
        public int IdAnuncio { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
    }
}
