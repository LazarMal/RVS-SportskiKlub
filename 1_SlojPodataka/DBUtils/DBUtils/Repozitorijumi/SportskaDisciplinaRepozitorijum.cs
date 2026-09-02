using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using KlasePodataka;

namespace DBUtils.Repozitorijumi
{
    public class SportskaDisciplinaRepozitorijum : TabelaKlasa, ISportskaDisciplinaRepozitorijum
    {
        public SportskaDisciplinaRepozitorijum()
        {
            NazivTabele = "SportskaDisciplina";
            StatusObrade = "Učitavanje šifarnika sportskih disciplina SQL upitom";
        }

        public List<SportskaDisciplina> DajAktivne()
        {
            const string upit = @"
                SELECT IDSportskeDiscipline, Sifra, Naziv, Aktivna
                FROM dbo.SportskaDisciplina
                WHERE Aktivna = @Aktivna
                ORDER BY Naziv;";

            var parametri = new[]
            {
                new SqlParameter("@Aktivna", SqlDbType.Bit) { Value = true }
            };

            return Mapiraj(IzvrsiUpitSelect(upit, parametri));
        }

        public SportskaDisciplina DajPoId(int idSportskeDiscipline)
        {
            const string upit = @"
                SELECT IDSportskeDiscipline, Sifra, Naziv, Aktivna
                FROM dbo.SportskaDisciplina
                WHERE IDSportskeDiscipline = @IDSportskeDiscipline;";

            var parametri = new[]
            {
                new SqlParameter("@IDSportskeDiscipline", SqlDbType.Int)
                {
                    Value = idSportskeDiscipline
                }
            };

            List<SportskaDisciplina> discipline = Mapiraj(IzvrsiUpitSelect(upit, parametri));
            return discipline.Count == 0 ? null : discipline[0];
        }

        private static List<SportskaDisciplina> Mapiraj(DataTable tabela)
        {
            var rezultat = new List<SportskaDisciplina>();

            foreach (DataRow red in tabela.Rows)
            {
                rezultat.Add(new SportskaDisciplina
                {
                    IDSportskeDiscipline = (int)red["IDSportskeDiscipline"],
                    Sifra = (string)red["Sifra"],
                    Naziv = (string)red["Naziv"],
                    Aktivna = (bool)red["Aktivna"]
                });
            }

            return rezultat;
        }

        public override string DajOpisObrade()
        {
            return "Šifarnik sportskih disciplina učitava se nasleđivanjem TabelaKlasa i parametrizovanim SQL upitom.";
        }
    }
}

