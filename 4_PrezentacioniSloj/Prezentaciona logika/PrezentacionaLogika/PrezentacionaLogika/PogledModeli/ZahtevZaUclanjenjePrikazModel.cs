using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PrezentacionaLogika.PogledModeli
{
    public class ZahtevZaUclanjenjePrikazModel
    {
        public ZahtevZaUclanjenjePrikazModel()
        {
            Dokumentacija = new List<DokumentacijaStavkaPrikazModel>();
            SportskeDiscipline = new List<SportskaDisciplinaOpcijaPrikazModel>();
        }

        public int IDZahteva { get; set; }

        [Display(Name = "Broj zahteva")]
        public string BrojZahteva { get; set; }

        [Required(ErrorMessage = "JMBG je obavezan.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG mora imati tačno 13 cifara.")]
        public string JMBG { get; set; }

        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50)]
        public string Ime { get; set; }

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50)]
        public string Prezime { get; set; }

        [Required(ErrorMessage = "Datum rođenja je obavezan.")]
        [Display(Name = "Datum rođenja")]
        [DataType(DataType.Date)]
        public DateTime? DatumRodjenja { get; set; }

        [Required(ErrorMessage = "Pol je obavezan.")]
        [RegularExpression("^[MŽ]$", ErrorMessage = "Pol mora biti M ili Ž.")]
        public string Pol { get; set; }

        [Required(ErrorMessage = "Državljanstvo je obavezno.")]
        [StringLength(50)]
        public string Drzavljanstvo { get; set; }

        [Required(ErrorMessage = "Adresa je obavezna.")]
        [StringLength(120)]
        public string Adresa { get; set; }

        [Required(ErrorMessage = "Kontakt telefon je obavezan.")]
        [RegularExpression(@"^\+?[0-9][0-9 /\-]{6,19}$", ErrorMessage = "Telefon nije u ispravnom formatu.")]
        [Display(Name = "Kontakt telefon")]
        public string KontaktTelefon { get; set; }

        [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sportska disciplina je obavezna.")]
        [Display(Name = "Sportska disciplina")]
        public int IDSportskeDiscipline { get; set; }

        [Display(Name = "Datum podnošenja")]
        [DataType(DataType.Date)]
        public DateTime DatumPodnosenja { get; set; }

        [Required(ErrorMessage = "Sezona je obavezna.")]
        [RegularExpression(@"^\d{4}/\d{2}$", ErrorMessage = "Sezona mora biti u formatu GGGG/GG.")]
        public string Sezona { get; set; }

        [Required(ErrorMessage = "Mesto kluba je obavezno.")]
        [StringLength(60)]
        [Display(Name = "Mesto kluba")]
        public string MestoKluba { get; set; }

        [Required(ErrorMessage = "Datum sportskog pregleda je obavezan.")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum sportskog pregleda")]
        public DateTime? DatumSportskogPregleda { get; set; }

        [Required(ErrorMessage = "Rezultat testa sposobnosti je obavezan.")]
        [Display(Name = "Rezultat testa sposobnosti")]
        public string RezultatTestaSposobnosti { get; set; }

        [Display(Name = "Status zahteva")]
        public string StatusZahteva { get; set; }

        [StringLength(500)]
        public string Napomena { get; set; }

        public IList<DokumentacijaStavkaPrikazModel> Dokumentacija { get; set; }

        [Display(Name = "Ime i prezime roditelja/staratelja")]
        [StringLength(100)]
        public string RoditeljImePrezime { get; set; }

        [Display(Name = "JMBG roditelja/staratelja")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG roditelja/staratelja mora imati 13 cifara.")]
        public string RoditeljJMBG { get; set; }

        [StringLength(40)]
        public string Srodstvo { get; set; }

        [Display(Name = "Telefon roditelja/staratelja")]
        [RegularExpression(@"^\+?[0-9][0-9 /\-]{6,19}$", ErrorMessage = "Telefon roditelja nije ispravan.")]
        public string RoditeljTelefon { get; set; }

        [Display(Name = "Email roditelja/staratelja")]
        [EmailAddress(ErrorMessage = "Email roditelja nije ispravan.")]
        public string RoditeljEmail { get; set; }

        public IList<SportskaDisciplinaOpcijaPrikazModel> SportskeDiscipline { get; set; }
    }

    public class DokumentacijaStavkaPrikazModel
    {
        public int IDDokumentacije { get; set; }

        [Required]
        public string NazivDokumenta { get; set; }

        public bool Dostavljeno { get; set; }
    }

    public class SportskaDisciplinaOpcijaPrikazModel
    {
        public int Vrednost { get; set; }

        public string Tekst { get; set; }
    }
}
