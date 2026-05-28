using G2_SC603_KN_Proyecto.Data;
using Microsoft.AspNetCore.Mvc;

public class EquiposController : Controller
{
    private readonly ApplicationDbContext _context;

    public EquiposController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string buscar)
    {
        var equipos = _context.Equipo.AsQueryable();

        if (!string.IsNullOrEmpty(buscar))
        {
            buscar = buscar.ToLower();

            equipos = equipos.Where(e =>
                e.nombre.ToLower().Contains(buscar) ||
                e.estado.ToLower().Contains(buscar) ||
                "equipamiento".Contains(buscar));
        }

        return View(equipos.ToList());
    }
}
