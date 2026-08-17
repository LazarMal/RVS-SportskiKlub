using System.Collections.Generic;
using System.Linq;
using KlasePodataka;

namespace KlaseMapiranja
{
    public class MapiranjeSportskogKlubaKlasa
    {
        public ZahtevZaUclanjenjeDto UDto(ZahtevZaUclanjenje izvor)
        {
            if (izvor == null)
            {
                return null;
            }

            Kandidat kandidat = izvor.Kandidat ?? new Kandidat();

            return new ZahtevZaUclanjenjeDto
            {
                IDZahteva = izvor.IDZahteva,
                BrojZahteva = izvor.BrojZahteva,
                JMBG = kandidat.JMBG ?? izvor.JMBGKandidata,
                Ime = kandidat.Ime,
                Prezime = kandidat.Prezime,
                DatumRodjenja = kandidat.DatumRodjenja,
                Pol = kandidat.Pol,
                Drzavljanstvo = kandidat.Drzavljanstvo,
                Adresa = kandidat.Adresa,
                KontaktTelefon = kandidat.KontaktTelefon,
                Email = kandidat.Email,
                IDSportskeDiscipline = izvor.IDSportskeDiscipline,
                NazivSportskeDiscipline = izvor.SportskaDisciplina == null
                    ? null
                    : izvor.SportskaDisciplina.Naziv,
                DatumPodnosenja = izvor.DatumPodnosenja,
                Sezona = izvor.Sezona,
                MestoKluba = izvor.MestoKluba,
                DatumSportskogPregleda = izvor.DatumSportskogPregleda,
                RezultatTestaSposobnosti = izvor.RezultatTestaSposobnosti,
                StatusZahteva = izvor.StatusZahteva,
                Napomena = izvor.Napomena,
                Dokumentacija = (izvor.Dokumentacija ?? new List<Dokumentacija>())
                    .OrderBy(d => d.IDDokumentacije)
                    .Select(d => new DokumentacijaDto
                    {
                        IDDokumentacije = d.IDDokumentacije,
                        NazivDokumenta = d.NazivDokumenta,
                        Dostavljeno = d.Dostavljeno
                    })
                    .ToList(),
                RoditeljStaratelj = MapirajRoditelja(izvor.RoditeljiStaratelji),
                IstorijaStatusa = (izvor.IstorijaStatusa ?? new List<IstorijaStatusaZahteva>())
                    .OrderByDescending(i => i.DatumPromene)
                    .Select(i => new IstorijaStatusaDto
                    {
                        StariStatus = i.StariStatus,
                        NoviStatus = i.NoviStatus,
                        DatumPromene = i.DatumPromene,
                        KorisnickoIme = i.KorisnickoIme,
                        Napomena = i.Napomena
                    })
                    .ToList()
            };
        }

        public Kandidat UKandidata(ZahtevZaUclanjenjeDto izvor)
        {
            return new Kandidat
            {
                JMBG = izvor.JMBG,
                Ime = izvor.Ime,
                Prezime = izvor.Prezime,
                DatumRodjenja = izvor.DatumRodjenja,
                Pol = izvor.Pol,
                Drzavljanstvo = izvor.Drzavljanstvo,
                Adresa = izvor.Adresa,
                KontaktTelefon = izvor.KontaktTelefon,
                Email = izvor.Email
            };
        }

        public ZahtevZaUclanjenje UZahtev(ZahtevZaUclanjenjeDto izvor)
        {
            return new ZahtevZaUclanjenje
            {
                IDZahteva = izvor.IDZahteva,
                BrojZahteva = izvor.BrojZahteva,
                JMBGKandidata = izvor.JMBG,
                IDSportskeDiscipline = izvor.IDSportskeDiscipline,
                DatumPodnosenja = izvor.DatumPodnosenja,
                Sezona = izvor.Sezona,
                MestoKluba = izvor.MestoKluba,
                DatumSportskogPregleda = izvor.DatumSportskogPregleda,
                RezultatTestaSposobnosti = izvor.RezultatTestaSposobnosti,
                StatusZahteva = izvor.StatusZahteva,
                Napomena = izvor.Napomena
            };
        }

        public IList<Dokumentacija> UDokumentaciju(ZahtevZaUclanjenjeDto izvor)
        {
            return (izvor.Dokumentacija ?? new List<DokumentacijaDto>())
                .Select(d => new Dokumentacija
                {
                    IDDokumentacije = d.IDDokumentacije,
                    IDZahteva = izvor.IDZahteva,
                    NazivDokumenta = d.NazivDokumenta,
                    Dostavljeno = d.Dostavljeno
                })
                .ToList();
        }

        public RoditeljStaratelj URoditelja(ZahtevZaUclanjenjeDto izvor)
        {
            if (izvor.RoditeljStaratelj == null)
            {
                return null;
            }

            return new RoditeljStaratelj
            {
                IDRoditeljaStaratelja = izvor.RoditeljStaratelj.IDRoditeljaStaratelja,
                IDZahteva = izvor.IDZahteva,
                ImePrezime = izvor.RoditeljStaratelj.ImePrezime,
                JMBG = izvor.RoditeljStaratelj.JMBG,
                Srodstvo = izvor.RoditeljStaratelj.Srodstvo,
                KontaktTelefon = izvor.RoditeljStaratelj.KontaktTelefon,
                Email = izvor.RoditeljStaratelj.Email
            };
        }

        private static RoditeljStarateljDto MapirajRoditelja(
            IEnumerable<RoditeljStaratelj> roditelji)
        {
            RoditeljStaratelj roditelj = (roditelji ?? Enumerable.Empty<RoditeljStaratelj>())
                .FirstOrDefault();

            if (roditelj == null)
            {
                return null;
            }

            return new RoditeljStarateljDto
            {
                IDRoditeljaStaratelja = roditelj.IDRoditeljaStaratelja,
                ImePrezime = roditelj.ImePrezime,
                JMBG = roditelj.JMBG,
                Srodstvo = roditelj.Srodstvo,
                KontaktTelefon = roditelj.KontaktTelefon,
                Email = roditelj.Email
            };
        }
    }
}
