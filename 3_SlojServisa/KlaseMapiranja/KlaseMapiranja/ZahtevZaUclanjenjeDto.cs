using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KlaseMapiranja
{
    public class ZahtevZaUclanjenjeDto
    {
        public ZahtevZaUclanjenjeDto()
        {
            Dokumentacija = new List<DokumentacijaDto>();
            IstorijaStatusa = new List<IstorijaStatusaDto>();
        }

        public int IDZahteva { get; set; }

        public string BrojZahteva { get; set; }

        [Required]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG mora imati tačno 13 cifara.")]
        public string JMBG { get; set; }

        [Required, StringLength(50)]
        public string Ime { get; set; }

        [Required, StringLength(50)]
        public string Prezime { get; set; }

        [Required]
        public DateTime DatumRodjenja { get; set; }

        [Required, RegularExpression("^[MŽ]$")]
        public string Pol { get; set; }

        [Required, StringLength(50)]
        public string Drzavljanstvo { get; set; }

        [Required, StringLength(120)]
        public string Adresa { get; set; }

        [Required, StringLength(20)]
        [RegularExpression(@"^\+?[0-9][0-9 /\-]{6,19}$", ErrorMessage = "Telefon nije u ispravnom formatu.")]
        public string KontaktTelefon { get; set; }

        [EmailAddress, StringLength(100)]
        public string Email { get; set; }

        [Range(1, int.MaxValue)]
        public int IDSportskeDiscipline { get; set; }

        public string NazivSportskeDiscipline { get; set; }

        [Required]
        public DateTime DatumPodnosenja { get; set; }

        [Required, RegularExpression(@"^\d{4}/\d{2}$", ErrorMessage = "Sezona mora biti u formatu GGGG/GG.")]
        public string Sezona { get; set; }

        [Required, StringLength(60)]
        public string MestoKluba { get; set; }

        [Required]
        public DateTime DatumSportskogPregleda { get; set; }

        [Required]
        public string RezultatTestaSposobnosti { get; set; }

        public string StatusZahteva { get; set; }

        [StringLength(500)]
        public string Napomena { get; set; }

        public IList<DokumentacijaDto> Dokumentacija { get; set; }

        public RoditeljStarateljDto RoditeljStaratelj { get; set; }

        public IList<IstorijaStatusaDto> IstorijaStatusa { get; set; }
    }

    public class DokumentacijaDto
    {
        public int IDDokumentacije { get; set; }

        [Required, StringLength(100)]
        public string NazivDokumenta { get; set; }

        public bool Dostavljeno { get; set; }
    }

    public class RoditeljStarateljDto
    {
        public int IDRoditeljaStaratelja { get; set; }

        [Required, StringLength(100)]
        public string ImePrezime { get; set; }

        [Required, RegularExpression(@"^\d{13}$")]
        public string JMBG { get; set; }

        [Required, StringLength(40)]
        public string Srodstvo { get; set; }

        [Required, StringLength(20)]
        [RegularExpression(@"^\+?[0-9][0-9 /\-]{6,19}$", ErrorMessage = "Telefon roditelja/staratelja nije u ispravnom formatu.")]
        public string KontaktTelefon { get; set; }

        [EmailAddress, StringLength(100)]
        public string Email { get; set; }
    }

    public class IstorijaStatusaDto
    {
        public string StariStatus { get; set; }

        public string NoviStatus { get; set; }

        public DateTime DatumPromene { get; set; }

        public string KorisnickoIme { get; set; }

        public string Napomena { get; set; }
    }

    public class ParametriPoslovnihPravilaDto
    {
        [Range(1, 24)]
        public int MaksimalnaStarostSportskogPregledaMeseci { get; set; }

        [Range(1, 21)]
        public int StarosnaGranicaZaSaglasnost { get; set; }
    }
}
