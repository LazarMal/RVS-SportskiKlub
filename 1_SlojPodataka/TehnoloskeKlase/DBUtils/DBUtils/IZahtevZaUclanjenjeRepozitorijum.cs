using System.Collections.Generic;
using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public interface IZahtevZaUclanjenjeRepozitorijum
    {
        List<ZahtevZaUclanjenje> DajSve(string filter = null);

        ZahtevZaUclanjenje DajPoId(int idZahteva);

        int DodajSaDetaljima(
            Kandidat kandidat,
            ZahtevZaUclanjenje zahtev,
            IList<Dokumentacija> dokumentacija,
            RoditeljStaratelj roditeljStaratelj,
            string korisnickoIme);

        void IzmeniSaDetaljima(
            Kandidat kandidat,
            ZahtevZaUclanjenje zahtev,
            IList<Dokumentacija> dokumentacija,
            RoditeljStaratelj roditeljStaratelj,
            string korisnickoIme);

        void PromeniStatusBezOdobravanja(
            int idZahteva,
            string noviStatus,
            string korisnickoIme,
            string napomena);

        void PotvrdiOdobrenjePoslePoslovneProvere(
            int idZahteva,
            string korisnickoIme,
            string napomena);

        void Obrisi(int idZahteva);
    }
}
