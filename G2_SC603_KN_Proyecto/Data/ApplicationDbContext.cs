using G2_SC603_KN_Proyecto.Models;
using Microsoft.EntityFrameworkCore;

namespace G2_SC603_KN_Proyecto.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Equipo> Equipo { get; set; }
    }
}