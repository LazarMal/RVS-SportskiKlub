using System.Web.Mvc;
using KlaseMapiranja;
using RESTServis.Servisi;

namespace RESTServis.Controllers
{
    public class PocetnaController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "REST servis";
            ParametriPoslovnihPravilaDto parametri =
                ParametriPoslovnihPravilaDatoteka.Ucitaj();

            return View("~/Views/Pocetna/Index.cshtml", parametri);
        }
    }
}
