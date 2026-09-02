using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public class KandidatRepozitorijum : OsnovnaTehnoloskaKlasa, IKandidatRepozitorijum
    {
        public Kandidat DajPoJMBG(string jmbg)
        {
            using (var db = new SportskiKlubKontekst())
            {
                return db.Kandidati.Find(jmbg);
            }
        }

        public void Sacuvaj(Kandidat kandidat)
        {
            using (var db = new SportskiKlubKontekst())
            {
                Kandidat postojeci = db.Kandidati.Find(kandidat.JMBG);

                if (postojeci == null)
                {
                    db.Kandidati.Add(kandidat);
                }
                else
                {
                    db.Entry(postojeci).CurrentValues.SetValues(kandidat);
                }

                db.SaveChanges();
            }
        }

        public override string DajOpisObrade()
        {
            return "Repozitorijum kandidata koristi Entity Framework klase.";
        }
    }
}

