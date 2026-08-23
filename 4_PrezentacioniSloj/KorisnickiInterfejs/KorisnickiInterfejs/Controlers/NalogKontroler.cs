using System.Web.Mvc;
using DBUtils.Repozitorijumi;
using KlasePodataka;
using PrezentacionaLogika.PogledModeli;

namespace SportskiKlub.Controllers
{
    public class NalogController : Controller
    {
        private readonly IKorisnikRepozitorijum korisnikRepozitorijum;

        public NalogController()
        {
            korisnikRepozitorijum = new KorisnikRepozitorijum();
        }

        public ActionResult Prijava()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Prijava(PrijavaPrikazModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Korisnik korisnik = korisnikRepozitorijum.Prijavi(
                model.KorisnickoIme,
                model.Sifra);

            if (korisnik == null)
            {
                ModelState.AddModelError("", "Pogrešno korisničko ime ili lozinka.");
                return View(model);
            }

            Session["IDKorisnika"] = korisnik.IDKorisnika;
            Session["KorisnickoIme"] = korisnik.KorisnickoIme;
            Session["Ime"] = korisnik.Ime;
            Session["Prezime"] = korisnik.Prezime;
            Session["Uloga"] = korisnik.Uloga;

            return RedirectToAction("Pocetna", "Pocetna");
        }

        public ActionResult Odjava()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Prijava");
        }
    }
}
