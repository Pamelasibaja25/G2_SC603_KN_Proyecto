using System.ComponentModel.DataAnnotations;

namespace G2_SC603_KN_Proyecto.Models
{
    public class Anuncio
    {
        [Key]
        public int idAnuncio { get; set; }
        public string titulo { get; set; }
        public string mensaje { get; set; }
        public DateTime fecha { get; set; }
    }
}
