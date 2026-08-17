using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace PoslovnaLogika
{
    public class RestParametriPoslovnihPravilaServis : IParametriPoslovnihPravilaServis
    {
        private readonly Uri osnovnaAdresa;

        public RestParametriPoslovnihPravilaServis(string osnovnaAdresa)
        {
            if (string.IsNullOrWhiteSpace(osnovnaAdresa))
            {
                throw new ArgumentException("Osnovna adresa REST servisa nije podešena.", "osnovnaAdresa");
            }

            this.osnovnaAdresa = new Uri(osnovnaAdresa.TrimEnd('/') + "/", UriKind.Absolute);
        }

        public ParametriPoslovnihPravila DajParametre()
        {
            using (var klijent = new HttpClient { BaseAddress = osnovnaAdresa })
            {
                HttpResponseMessage odgovor = klijent
                    .GetAsync("api/parametri/poslovna-pravila")
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                odgovor.EnsureSuccessStatusCode();

                string json = odgovor.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                var parametri = JsonConvert.DeserializeObject<ParametriPoslovnihPravila>(json);

                if (parametri == null ||
                    parametri.MaksimalnaStarostSportskogPregledaMeseci < 1 ||
                    parametri.MaksimalnaStarostSportskogPregledaMeseci > 24 ||
                    parametri.StarosnaGranicaZaSaglasnost < 1 ||
                    parametri.StarosnaGranicaZaSaglasnost > 21)
                {
                    throw new InvalidOperationException(
                        "REST servis je vratio neispravne parametre poslovnih pravila.");
                }

                return parametri;
            }
        }
    }
}
