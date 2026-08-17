using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public class KorisnikRepozitorijum : OsnovnaTehnoloskaKlasa, IKorisnikRepozitorijum
    {
        private readonly string stringKonekcije;

        public KorisnikRepozitorijum()
        {
            stringKonekcije = ConfigurationManager
                .ConnectionStrings["SportskiKlubKonekcija"]
                .ConnectionString;
        }

        public Korisnik Prijavi(string korisnickoIme, string sifra)
        {
            using (var konekcija = new SqlConnection(stringKonekcije))
            using (var komanda = new SqlCommand("dbo.PrijaviKorisnika", konekcija))
            {
                komanda.CommandType = CommandType.StoredProcedure;
                komanda.Parameters.Add("@KorisnickoIme", SqlDbType.NVarChar, 50).Value = korisnickoIme;
                komanda.Parameters.Add("@Sifra", SqlDbType.NVarChar, 100).Value = sifra;

                konekcija.Open();

                using (SqlDataReader citac = komanda.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!citac.Read())
                    {
                        return null;
                    }

                    return new Korisnik
                    {
                        IDKorisnika = citac.GetInt32(citac.GetOrdinal("IDKorisnika")),
                        KorisnickoIme = citac.GetString(citac.GetOrdinal("KorisnickoIme")),
                        Ime = citac.GetString(citac.GetOrdinal("Ime")),
                        Prezime = citac.GetString(citac.GetOrdinal("Prezime")),
                        Uloga = citac.GetString(citac.GetOrdinal("Uloga")),
                        Aktivan = true
                    };
                }
            }
        }

        public override string DajOpisObrade()
        {
            return "Prijava korisnika koristi standardne SQL Client klase i Stored Procedure.";
        }
    }
}

