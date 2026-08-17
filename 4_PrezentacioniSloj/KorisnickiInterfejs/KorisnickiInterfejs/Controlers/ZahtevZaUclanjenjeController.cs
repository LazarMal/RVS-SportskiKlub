using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using DBUtils.Repozitorijumi;
using KlasePodataka;
using SportskiKlub.Filteri;
using PoslovnaLogika;
using PrezentacionaLogika.PogledModeli;

namespace SportskiKlub.Controllers
{
    [AutorizacijaSesijeAtribut]
    public class ZahtevZaUclanjenjeController : Controller
    {
        private static readonly string[] NaziviDokumentacije =
        {
            "Fotografija kandidata",
            "Dokaz identiteta",
            "Potvrda o sportskom pregledu",
            "Evidencija o položenom testu sposobnosti",
            "Saglasnost roditelja/staratelja",
            "Drugi dokument"
        };

        private readonly IZahtevZaUclanjenjeRepozitorijum zahtevRepozitorijum;
        private readonly ISportskaDisciplinaRepozitorijum disciplinaRepozitorijum;
        private readonly IPoslovnaPravilaServisi poslovnaPravila;
        private readonly IOdobravanjeZahtevaServis odobravanje;

        public ZahtevZaUclanjenjeController()
        {
            zahtevRepozitorijum = new ZahtevZaUclanjenjeRepozitorijum();
            disciplinaRepozitorijum = new SportskaDisciplinaRepozitorijum();

            var parametri = new RestParametriPoslovnihPravilaServis(
                ConfigurationManager.AppSettings["RestServisOsnovnaAdresa"]);

            poslovnaPravila = new PoslovnaPravilaServisi(parametri);
            odobravanje = new OdobravanjeZahtevaServis(zahtevRepozitorijum, parametri);
        }

        public ActionResult Spisak(string pretraga)
        {
            ViewBag.Filter = pretraga;
            return View(zahtevRepozitorijum.DajSve(pretraga));
        }

        public ActionResult Dodaj()
        {
            var model = new ZahtevZaUclanjenjePrikazModel
            {
                DatumPodnosenja = DateTime.Today,
                DatumRodjenja = DateTime.Today.AddYears(-18),
                DatumSportskogPregleda = DateTime.Today,
                Sezona = DajTekucuSezonu(),
                MestoKluba = "Zrenjanin",
                RezultatTestaSposobnosti = "Nije realizovan",
                StatusZahteva = "U obradi",
                Dokumentacija = KreirajPraznuDokumentaciju()
            };

            PopuniDiscipline(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Dodaj(ZahtevZaUclanjenjePrikazModel model)
        {
            Validiraj(model, false);

            if (!ModelState.IsValid)
            {
                PopuniDiscipline(model);
                OsigurajDokumentaciju(model);
                return View(model);
            }

            try
            {
                int id = zahtevRepozitorijum.DodajSaDetaljima(
                    UKandidata(model),
                    UZahtev(model),
                    UDokumentaciju(model),
                    URoditelja(model),
                    DajKorisnickoIme());

                TempData["Poruka"] = "Zahtev je uspešno evidentiran.";
                return RedirectToAction("Detalji", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Čuvanje nije uspelo: " + ex.Message);
                PopuniDiscipline(model);
                OsigurajDokumentaciju(model);
                return View(model);
            }
        }

        public ActionResult Detalji(int id)
        {
            ZahtevZaUclanjenje zahtev = zahtevRepozitorijum.DajPoId(id);

            if (zahtev == null)
            {
                return HttpNotFound();
            }

            return View(zahtev);
        }

        public ActionResult Izmeni(int id)
        {
            ZahtevZaUclanjenje zahtev = zahtevRepozitorijum.DajPoId(id);

            if (zahtev == null)
            {
                return HttpNotFound();
            }

            ZahtevZaUclanjenjePrikazModel model = UPrikazModel(zahtev);

            if (model.StatusZahteva == "Odobren")
            {
                model.StatusZahteva = "Na proveri";
                ViewBag.NapomenaStatusa =
                    "Izmena odobrenog zahteva vraća status na proveru. Posle čuvanja ponovo pokrenite poslovno pravilo.";
            }

            PopuniDiscipline(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Izmeni(ZahtevZaUclanjenjePrikazModel model)
        {
            ZahtevZaUclanjenje postojeci = zahtevRepozitorijum.DajPoId(model.IDZahteva);

            if (postojeci == null)
            {
                return HttpNotFound();
            }

            model.BrojZahteva = postojeci.BrojZahteva;
            model.DatumPodnosenja = postojeci.DatumPodnosenja;
            Validiraj(model, true);

            if (!ModelState.IsValid)
            {
                PopuniDiscipline(model);
                OsigurajDokumentaciju(model);
                return View(model);
            }

            try
            {
                zahtevRepozitorijum.IzmeniSaDetaljima(
                    UKandidata(model),
                    UZahtev(model),
                    UDokumentaciju(model),
                    URoditelja(model),
                    DajKorisnickoIme());

                TempData["Poruka"] = "Zahtev je uspešno izmenjen.";
                return RedirectToAction("Detalji", new { id = model.IDZahteva });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Izmena nije uspela: " + ex.Message);
                PopuniDiscipline(model);
                OsigurajDokumentaciju(model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Odobri(int id)
        {
            try
            {
                RezultatPoslovnogPravila rezultat = odobravanje.ProveriIOdobri(
                    id,
                    DajKorisnickoIme());

                TempData[rezultat.Uspesno ? "Poruka" : "Greska"] = rezultat.Poruka;
            }
            catch (Exception ex)
            {
                TempData["Greska"] = "Poslovno pravilo nije moglo da se proveri: " + ex.Message;
            }

            return RedirectToAction("Detalji", new { id = id });
        }

        public ActionResult Obrisi(int id)
        {
            if (!DaLiJeAdministrator())
            {
                return new HttpStatusCodeResult(403);
            }

            ZahtevZaUclanjenje zahtev = zahtevRepozitorijum.DajPoId(id);
            return zahtev == null ? (ActionResult)HttpNotFound() : View(zahtev);
        }

        [HttpPost, ActionName("Obrisi")]
        [ValidateAntiForgeryToken]
        public ActionResult PotvrdiBrisanje(int id)
        {
            if (!DaLiJeAdministrator())
            {
                return new HttpStatusCodeResult(403);
            }

            zahtevRepozitorijum.Obrisi(id);
            TempData["Poruka"] = "Zahtev i svi njegovi detalji su obrisani.";
            return RedirectToAction("Spisak");
        }

        public ActionResult StampaSvih()
        {
            ViewBag.NaslovStampe = "Spisak svih zahteva za učlanjenje";
            ViewBag.Filter = null;
            return View("StampaSpiska", zahtevRepozitorijum.DajSve());
        }

        public ActionResult StampaFiltriranih(string pretraga)
        {
            ViewBag.NaslovStampe = "Filtrirani spisak zahteva za učlanjenje";
            ViewBag.Filter = pretraga;
            return View("StampaSpiska", zahtevRepozitorijum.DajSve(pretraga));
        }

        public ActionResult StampaZahteva(int id)
        {
            ZahtevZaUclanjenje zahtev = zahtevRepozitorijum.DajPoId(id);
            return zahtev == null ? (ActionResult)HttpNotFound() : View(zahtev);
        }

        private void Validiraj(
            ZahtevZaUclanjenjePrikazModel model,
            bool izmena)
        {
            if (!model.DatumRodjenja.HasValue ||
                model.DatumRodjenja.Value.Date < new DateTime(1900, 1, 1) ||
                model.DatumRodjenja.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("DatumRodjenja", "Datum rođenja nije ispravan.");
            }

            if (model.DatumPodnosenja == default(DateTime) ||
                model.DatumPodnosenja.Date > DateTime.Today)
            {
                ModelState.AddModelError("DatumPodnosenja", "Datum podnošenja nije ispravan.");
            }

            if (!model.DatumSportskogPregleda.HasValue ||
                model.DatumSportskogPregleda.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DatumSportskogPregleda",
                    "Datum sportskog pregleda nije ispravan.");
            }

            if (!DaLiJeSezonaIspravna(model.Sezona))
            {
                ModelState.AddModelError("Sezona", "Drugi deo sezone mora biti naredna godina.");
            }

            string[] rezultati = { "Položen", "Nije položen", "Nije realizovan" };
            if (!rezultati.Contains(model.RezultatTestaSposobnosti))
            {
                ModelState.AddModelError("RezultatTestaSposobnosti", "Rezultat testa nije dozvoljen.");
            }

            string[] statusiZaRucnuPromenu = { "U obradi", "Na proveri", "Odbijen" };
            if (izmena && !statusiZaRucnuPromenu.Contains(model.StatusZahteva))
            {
                ModelState.AddModelError(
                    "StatusZahteva",
                    "Status Odobren se postavlja isključivo akcijom poslovnog pravila.");
            }

            SportskaDisciplina disciplina =
                disciplinaRepozitorijum.DajPoId(model.IDSportskeDiscipline);

            if (disciplina == null || !disciplina.Aktivna)
            {
                ModelState.AddModelError(
                    "IDSportskeDiscipline",
                    "Izabrana sportska disciplina ne postoji ili nije aktivna.");
            }

            OsigurajDokumentaciju(model);
            if (model.Dokumentacija
                .GroupBy(d => d.NazivDokumenta, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("Dokumentacija", "Stavke dokumentacije moraju biti jedinstvene.");
            }

            foreach (string naziv in NaziviDokumentacije)
            {
                if (!model.Dokumentacija.Any(d => string.Equals(
                    d.NazivDokumenta,
                    naziv,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("Dokumentacija", "Nedostaje stavka: " + naziv + ".");
                }
            }

            if (!model.DatumRodjenja.HasValue)
            {
                return;
            }

            bool imaBiloKojiPodatakRoditelja =
                !string.IsNullOrWhiteSpace(model.RoditeljImePrezime) ||
                !string.IsNullOrWhiteSpace(model.RoditeljJMBG) ||
                !string.IsNullOrWhiteSpace(model.Srodstvo) ||
                !string.IsNullOrWhiteSpace(model.RoditeljTelefon) ||
                !string.IsNullOrWhiteSpace(model.RoditeljEmail);

            if (imaBiloKojiPodatakRoditelja &&
                (string.IsNullOrWhiteSpace(model.RoditeljImePrezime) ||
                 string.IsNullOrWhiteSpace(model.RoditeljJMBG) ||
                 string.IsNullOrWhiteSpace(model.Srodstvo) ||
                 string.IsNullOrWhiteSpace(model.RoditeljTelefon)))
            {
                ModelState.AddModelError(
                    "",
                    "Ako se unose podaci roditelja/staratelja, ime, JMBG, srodstvo i telefon su obavezni.");
            }

            try
            {
                if (poslovnaPravila.DaLiSuPotrebniPodaciRoditelja(model.DatumRodjenja.Value))
                {
                    bool saglasnost = model.Dokumentacija.Any(d =>
                        d.NazivDokumenta == "Saglasnost roditelja/staratelja" && d.Dostavljeno);

                    if (string.IsNullOrWhiteSpace(model.RoditeljImePrezime) ||
                        string.IsNullOrWhiteSpace(model.RoditeljJMBG) ||
                        string.IsNullOrWhiteSpace(model.Srodstvo) ||
                        string.IsNullOrWhiteSpace(model.RoditeljTelefon) ||
                        !saglasnost)
                    {
                        ModelState.AddModelError(
                            "",
                            "Za maloletnog kandidata obavezni su podaci roditelja/staratelja i dostavljena saglasnost.");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Nije moguće učitati starosnu granicu iz REST servisa: " + ex.Message);
            }
        }

        private void PopuniDiscipline(ZahtevZaUclanjenjePrikazModel model)
        {
            model.SportskeDiscipline = disciplinaRepozitorijum.DajAktivne()
                .Select(d => new SportskaDisciplinaOpcijaPrikazModel
                {
                    Vrednost = d.IDSportskeDiscipline,
                    Tekst = d.Sifra + " - " + d.Naziv
                })
                .ToList();
        }

        private static IList<DokumentacijaStavkaPrikazModel> KreirajPraznuDokumentaciju()
        {
            return NaziviDokumentacije
                .Select(n => new DokumentacijaStavkaPrikazModel
                {
                    NazivDokumenta = n,
                    Dostavljeno = false
                })
                .ToList();
        }

        private static void OsigurajDokumentaciju(ZahtevZaUclanjenjePrikazModel model)
        {
            if (model.Dokumentacija == null)
            {
                model.Dokumentacija = KreirajPraznuDokumentaciju();
            }
        }

        private static Kandidat UKandidata(ZahtevZaUclanjenjePrikazModel model)
        {
            return new Kandidat
            {
                JMBG = model.JMBG,
                Ime = model.Ime,
                Prezime = model.Prezime,
                DatumRodjenja = model.DatumRodjenja.Value,
                Pol = model.Pol,
                Drzavljanstvo = model.Drzavljanstvo,
                Adresa = model.Adresa,
                KontaktTelefon = model.KontaktTelefon,
                Email = model.Email
            };
        }

        private static ZahtevZaUclanjenje UZahtev(ZahtevZaUclanjenjePrikazModel model)
        {
            return new ZahtevZaUclanjenje
            {
                IDZahteva = model.IDZahteva,
                BrojZahteva = model.BrojZahteva,
                JMBGKandidata = model.JMBG,
                IDSportskeDiscipline = model.IDSportskeDiscipline,
                DatumPodnosenja = model.DatumPodnosenja,
                Sezona = model.Sezona,
                MestoKluba = model.MestoKluba,
                DatumSportskogPregleda = model.DatumSportskogPregleda.Value,
                RezultatTestaSposobnosti = model.RezultatTestaSposobnosti,
                StatusZahteva = model.StatusZahteva,
                Napomena = model.Napomena
            };
        }

        private static IList<Dokumentacija> UDokumentaciju(
            ZahtevZaUclanjenjePrikazModel model)
        {
            return model.Dokumentacija.Select(d => new Dokumentacija
            {
                IDDokumentacije = d.IDDokumentacije,
                IDZahteva = model.IDZahteva,
                NazivDokumenta = d.NazivDokumenta,
                Dostavljeno = d.Dostavljeno
            }).ToList();
        }

        private RoditeljStaratelj URoditelja(ZahtevZaUclanjenjePrikazModel model)
        {
            bool imaPodatke = !string.IsNullOrWhiteSpace(model.RoditeljImePrezime) ||
                !string.IsNullOrWhiteSpace(model.RoditeljJMBG) ||
                !string.IsNullOrWhiteSpace(model.Srodstvo) ||
                !string.IsNullOrWhiteSpace(model.RoditeljTelefon) ||
                !string.IsNullOrWhiteSpace(model.RoditeljEmail);

            if (!imaPodatke)
            {
                return null;
            }

            return new RoditeljStaratelj
            {
                IDZahteva = model.IDZahteva,
                ImePrezime = model.RoditeljImePrezime,
                JMBG = model.RoditeljJMBG,
                Srodstvo = model.Srodstvo,
                KontaktTelefon = model.RoditeljTelefon,
                Email = model.RoditeljEmail
            };
        }

        private static ZahtevZaUclanjenjePrikazModel UPrikazModel(ZahtevZaUclanjenje zahtev)
        {
            RoditeljStaratelj roditelj = zahtev.RoditeljiStaratelji.FirstOrDefault();

            return new ZahtevZaUclanjenjePrikazModel
            {
                IDZahteva = zahtev.IDZahteva,
                BrojZahteva = zahtev.BrojZahteva,
                JMBG = zahtev.Kandidat.JMBG,
                Ime = zahtev.Kandidat.Ime,
                Prezime = zahtev.Kandidat.Prezime,
                DatumRodjenja = zahtev.Kandidat.DatumRodjenja,
                Pol = zahtev.Kandidat.Pol,
                Drzavljanstvo = zahtev.Kandidat.Drzavljanstvo,
                Adresa = zahtev.Kandidat.Adresa,
                KontaktTelefon = zahtev.Kandidat.KontaktTelefon,
                Email = zahtev.Kandidat.Email,
                IDSportskeDiscipline = zahtev.IDSportskeDiscipline,
                DatumPodnosenja = zahtev.DatumPodnosenja,
                Sezona = zahtev.Sezona,
                MestoKluba = zahtev.MestoKluba,
                DatumSportskogPregleda = zahtev.DatumSportskogPregleda,
                RezultatTestaSposobnosti = zahtev.RezultatTestaSposobnosti,
                StatusZahteva = zahtev.StatusZahteva,
                Napomena = zahtev.Napomena,
                Dokumentacija = zahtev.Dokumentacija
                    .OrderBy(d => Array.IndexOf(NaziviDokumentacije, d.NazivDokumenta))
                    .Select(d => new DokumentacijaStavkaPrikazModel
                    {
                        IDDokumentacije = d.IDDokumentacije,
                        NazivDokumenta = d.NazivDokumenta,
                        Dostavljeno = d.Dostavljeno
                    }).ToList(),
                RoditeljImePrezime = roditelj == null ? null : roditelj.ImePrezime,
                RoditeljJMBG = roditelj == null ? null : roditelj.JMBG,
                Srodstvo = roditelj == null ? null : roditelj.Srodstvo,
                RoditeljTelefon = roditelj == null ? null : roditelj.KontaktTelefon,
                RoditeljEmail = roditelj == null ? null : roditelj.Email
            };
        }

        private string DajKorisnickoIme()
        {
            return Session["KorisnickoIme"] == null
                ? "SISTEM"
                : Session["KorisnickoIme"].ToString();
        }

        private bool DaLiJeAdministrator()
        {
            return Session["Uloga"] != null && Session["Uloga"].ToString() == "Administrator";
        }

        private static string DajTekucuSezonu()
        {
            int pocetnaGodina = DateTime.Today.Month >= 7 ? DateTime.Today.Year : DateTime.Today.Year - 1;
            return pocetnaGodina + "/" + ((pocetnaGodina + 1) % 100).ToString("00");
        }

        private static bool DaLiJeSezonaIspravna(string sezona)
        {
            if (string.IsNullOrWhiteSpace(sezona) || sezona.Length != 7)
            {
                return false;
            }

            int pocetna;
            int zavrsna;
            return int.TryParse(sezona.Substring(0, 4), out pocetna) &&
                int.TryParse(sezona.Substring(5, 2), out zavrsna) &&
                (pocetna + 1) % 100 == zavrsna;
        }
    }
}
