using System.ComponentModel.DataAnnotations;
namespace G2_SC603_KN_Proyecto.Models
{
    public class Equipo
    {
        [Key]
        public int id_equipo { get; set; }

        public string nombre { get; set; }

        public string estado { get; set; }

        public DateTime? fecha_compra { get; set; }

        public decimal costo { get; set; }
    }
}
