using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("Kandidat")]
    public class Kandidat
    {
        public Kandidat()
        {
            ZahteviZaUclanjenje = new HashSet<ZahtevZaUclanjenje>();
        }

        [Key]
        [StringLength(13, MinimumLength = 13)]
        [Column(TypeName = "char")]
        public string JMBG { get; set; }

        [Required]
        [StringLength(50)]
        public string Ime { get; set; }

        [Required]
        [StringLength(50)]
        public string Prezime { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DatumRodjenja { get; set; }

        [Required]
        [StringLength(1)]
        [Column(TypeName = "nchar")]
        public string Pol { get; set; }

        [Required]
        [StringLength(50)]
        public string Drzavljanstvo { get; set; }

        [Required]
        [StringLength(120)]
        public string Adresa { get; set; }

        [Required]
        [StringLength(20)]
        public string KontaktTelefon { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        public virtual ICollection<ZahtevZaUclanjenje> ZahteviZaUclanjenje { get; set; }
    }
}
