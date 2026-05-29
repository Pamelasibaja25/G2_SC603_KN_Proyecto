using G2_SC603_KN_Proyecto.Models;
using Microsoft.AspNetCore.Mvc;

public class EquiposController : Controller
{
    private readonly DbOrionFitContext _context;

    public EquiposController(DbOrionFitContext context)
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
                e.Nombre.ToLower().Contains(buscar) ||
                e.Estado.ToLower().Contains(buscar) ||
                "equipamiento".Contains(buscar));
        }

        return View(equipos.ToList());
    }
}
