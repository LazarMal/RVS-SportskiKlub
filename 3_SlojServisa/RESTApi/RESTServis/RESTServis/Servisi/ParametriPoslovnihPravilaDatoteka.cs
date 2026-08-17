using System.IO;
using System.Web.Hosting;
using KlaseMapiranja;
using Newtonsoft.Json;

namespace RESTServis.Servisi
{
    internal static class ParametriPoslovnihPravilaDatoteka
    {
        public static ParametriPoslovnihPravilaDto Ucitaj()
        {
            string putanja = HostingEnvironment.MapPath(
                "~/App_Data/poslovna_pravila.json");

            if (string.IsNullOrWhiteSpace(putanja) || !File.Exists(putanja))
            {
                throw new FileNotFoundException(
                    "Nije pronađen fajl sa parametrima poslovnih pravila.",
                    putanja);
            }

            string json = File.ReadAllText(putanja);
            var parametri = JsonConvert.DeserializeObject<ParametriPoslovnihPravilaDto>(json);

            if (parametri == null ||
                parametri.MaksimalnaStarostSportskogPregledaMeseci < 1 ||
                parametri.MaksimalnaStarostSportskogPregledaMeseci > 24 ||
                parametri.StarosnaGranicaZaSaglasnost < 1 ||
                parametri.StarosnaGranicaZaSaglasnost > 21)
            {
                throw new InvalidDataException(
                    "Parametri poslovnih pravila nisu ispravni.");
            }

            return parametri;
        }
    }
}
