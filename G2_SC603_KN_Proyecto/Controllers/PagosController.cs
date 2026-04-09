using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace G2_SC603_KN_Proyecto.Controllers
{
    public class PagosController : Controller
    {
        // GET: PagosController
        public ActionResult Index()
        {
            ViewData["Mensaje"] = "Control de Pagos";
            return View();

        }
        public IActionResult Registrar()
        {
            ViewData["Mensaje"] = "Registrar Pago";
            return View();
        }
     
        }

    }


