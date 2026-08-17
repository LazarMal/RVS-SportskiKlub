using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using DBUtils.Repozitorijumi;
using KlaseMapiranja;
using KlasePodataka;
using RESTServis.Servisi;

namespace RESTServis.Controllers.Api
{
    [RoutePrefix("api/zahtevi")]
    public class ZahteviController : ApiController
    {
        private static readonly string[] DozvoljeniStatusi =
        {
            "U obradi", "Na proveri", "Odobren", "Odbijen"
        };

        private static readonly string[] DozvoljeniRezultati =
        {
            "Položen", "Nije položen", "Nije realizovan"
        };

        private static readonly string[] ObaveznaDokumenta =
        {
            "Potvrda o sportskom pregledu",
            "Evidencija o položenom testu sposobnosti"
        };

        private readonly IZahtevZaUclanjenjeRepozitorijum repozitorijum;
        private readonly ISportskaDisciplinaRepozitorijum disciplinaRepozitorijum;
        private readonly MapiranjeSportskogKlubaKlasa mapiranje;

        public ZahteviController()
            : this(
                new ZahtevZaUclanjenjeRepozitorijum(),
                new SportskaDisciplinaRepozitorijum(),
                new MapiranjeSportskogKlubaKlasa())
        {
        }

        public ZahteviController(
            IZahtevZaUclanjenjeRepozitorijum repozitorijum,
            ISportskaDisciplinaRepozitorijum disciplinaRepozitorijum,
            MapiranjeSportskogKlubaKlasa mapiranje)
        {
            this.repozitorijum = repozitorijum;
            this.disciplinaRepozitorijum = disciplinaRepozitorijum;
            this.mapiranje = mapiranje;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult DajSveZahteve(string filter = null)
        {
            var zahtevi = repozitorijum.DajSve(filter)
                .Select(mapiranje.UDto)
                .ToList();

            return Ok(zahtevi);
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult DajZahtevPoId(int id)
        {
            ZahtevZaUclanjenjeDto zahtev = mapiranje.UDto(repozitorijum.DajPoId(id));

            if (zahtev == null)
            {
                return NotFound();
            }

            return Ok(zahtev);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult DodajZahtev(ZahtevZaUclanjenjeDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Podaci nisu poslati.");
            }

            dto.StatusZahteva = "U obradi";

            try
            {
                ValidirajDomen(dto, false);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int id = repozitorijum.DodajSaDetaljima(
                    mapiranje.UKandidata(dto),
                    mapiranje.UZahtev(dto),
                    mapiranje.UDokumentaciju(dto),
                    mapiranje.URoditelja(dto),
                    DajKorisnickoIme());

                ZahtevZaUclanjenjeDto sacuvani = mapiranje.UDto(repozitorijum.DajPoId(id));
                return Ok(sacuvani);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult IzmeniZahtev(int id, ZahtevZaUclanjenjeDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Podaci nisu poslati.");
            }

            ZahtevZaUclanjenje postojeci = repozitorijum.DajPoId(id);

            if (postojeci == null)
            {
                return NotFound();
            }

            dto.IDZahteva = id;
            dto.BrojZahteva = postojeci.BrojZahteva;
            dto.DatumPodnosenja = postojeci.DatumPodnosenja;

            try
            {
                ValidirajDomen(dto, true);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                repozitorijum.IzmeniSaDetaljima(
                    mapiranje.UKandidata(dto),
                    mapiranje.UZahtev(dto),
                    mapiranje.UDokumentaciju(dto),
                    mapiranje.URoditelja(dto),
                    DajKorisnickoIme());

                return Ok(mapiranje.UDto(repozitorijum.DajPoId(id)));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult ObrisiZahtev(int id)
        {
            if (repozitorijum.DajPoId(id) == null)
            {
                return NotFound();
            }

            repozitorijum.Obrisi(id);
            return Ok(new { Poruka = "Zahtev je uspešno obrisan." });
        }

        private void ValidirajDomen(ZahtevZaUclanjenjeDto dto, bool izmena)
        {
            if (dto.DatumRodjenja == default(DateTime) || dto.DatumRodjenja.Date > DateTime.Today)
            {
                ModelState.AddModelError("DatumRodjenja", "Datum rođenja nije ispravan.");
            }

            if (dto.DatumPodnosenja == default(DateTime) || dto.DatumPodnosenja.Date > DateTime.Today)
            {
                ModelState.AddModelError("DatumPodnosenja", "Datum podnošenja nije ispravan.");
            }

            if (dto.DatumSportskogPregleda == default(DateTime) ||
                dto.DatumSportskogPregleda.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DatumSportskogPregleda",
                    "Datum sportskog pregleda nije ispravan.");
            }

            if (!DaLiJeSezonaIspravna(dto.Sezona))
            {
                ModelState.AddModelError(
                    "Sezona",
                    "Sezona mora biti u formatu GGGG/GG, a drugi deo mora biti naredna godina.");
            }

            SportskaDisciplina disciplina =
                disciplinaRepozitorijum.DajPoId(dto.IDSportskeDiscipline);

            if (disciplina == null || !disciplina.Aktivna)
            {
                ModelState.AddModelError(
                    "IDSportskeDiscipline",
                    "Izabrana sportska disciplina ne postoji ili nije aktivna.");
            }

            if (!DozvoljeniRezultati.Contains(dto.RezultatTestaSposobnosti))
            {
                ModelState.AddModelError(
                    "RezultatTestaSposobnosti",
                    "Rezultat testa mora biti: Položen, Nije položen ili Nije realizovan.");
            }

            if (izmena && !DozvoljeniStatusi.Contains(dto.StatusZahteva))
            {
                ModelState.AddModelError(
                    "StatusZahteva",
                    "Status mora biti: U obradi, Na proveri, Odobren ili Odbijen.");
            }

            if (izmena && string.Equals(dto.StatusZahteva, "Odobren", StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    "StatusZahteva",
                    "Status Odobren se postavlja isključivo servisom poslovnog pravila.");
            }

            IList<DokumentacijaDto> dokumentacija = (dto.Dokumentacija ??
                new List<DokumentacijaDto>())
                .Where(d => d != null)
                .ToList();

            foreach (string naziv in ObaveznaDokumenta)
            {
                if (!dokumentacija.Any(d => string.Equals(
                    d.NazivDokumenta,
                    naziv,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(
                        "Dokumentacija",
                        "Nedostaje stavka dokumentacije: " + naziv + ".");
                }
            }

            if (dokumentacija
                .Where(d => !string.IsNullOrWhiteSpace(d.NazivDokumenta))
                .GroupBy(d => d.NazivDokumenta, StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("Dokumentacija", "Nazivi dokumenata moraju biti jedinstveni.");
            }

            int starosnaGranica = ParametriPoslovnihPravilaDatoteka
                .Ucitaj()
                .StarosnaGranicaZaSaglasnost;

            if (dto.DatumRodjenja != default(DateTime) &&
                DaLiJeMaloletan(dto.DatumRodjenja, starosnaGranica))
            {
                bool saglasnostDostavljena = dokumentacija.Any(d =>
                    d.Dostavljeno &&
                    string.Equals(
                        d.NazivDokumenta,
                        "Saglasnost roditelja/staratelja",
                        StringComparison.OrdinalIgnoreCase));

                RoditeljStarateljDto roditelj = dto.RoditeljStaratelj;

                if (roditelj == null ||
                    string.IsNullOrWhiteSpace(roditelj.ImePrezime) ||
                    string.IsNullOrWhiteSpace(roditelj.JMBG) ||
                    string.IsNullOrWhiteSpace(roditelj.Srodstvo) ||
                    string.IsNullOrWhiteSpace(roditelj.KontaktTelefon) ||
                    !saglasnostDostavljena)
                {
                    ModelState.AddModelError(
                        "RoditeljStaratelj",
                        "Za maloletnog kandidata obavezni su potpuni podaci roditelja/staratelja i dostavljena saglasnost.");
                }
            }
        }

        private static bool DaLiJeMaloletan(DateTime datumRodjenja, int starosnaGranica)
        {
            DateTime danas = DateTime.Today;
            int godine = danas.Year - datumRodjenja.Year;

            if (datumRodjenja.Date > danas.AddYears(-godine))
            {
                godine--;
            }

            return godine < starosnaGranica;
        }

        private static bool DaLiJeSezonaIspravna(string sezona)
        {
            if (string.IsNullOrWhiteSpace(sezona) || sezona.Length != 7 || sezona[4] != '/')
            {
                return false;
            }

            int pocetna;
            int zavrsna;
            return int.TryParse(sezona.Substring(0, 4), out pocetna) &&
                int.TryParse(sezona.Substring(5, 2), out zavrsna) &&
                (pocetna + 1) % 100 == zavrsna;
        }

        private string DajKorisnickoIme()
        {
            return User == null || User.Identity == null || !User.Identity.IsAuthenticated
                ? "REST"
                : User.Identity.Name;
        }
    }
}
