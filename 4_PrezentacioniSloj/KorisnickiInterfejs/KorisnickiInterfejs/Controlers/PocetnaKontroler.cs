using System.Web.Mvc;

namespace SportskiKlub.Controllers
{
    public class PocetnaController : Controller
    {
        public ActionResult Pocetna()
        {
            return View();
        }

        public ActionResult OAplikaciji()
        {
            ViewBag.Message = "Stranica sa osnovnim informacijama o aplikaciji.";

            return View();
        }

        public ActionResult Kontakt()
        {
            ViewBag.Message = "Stranica sa kontakt informacijama.";

            return View();
        }
    }
}
