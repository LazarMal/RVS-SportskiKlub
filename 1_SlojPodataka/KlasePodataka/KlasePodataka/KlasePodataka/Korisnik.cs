using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("Korisnik")]
    public class Korisnik
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDKorisnika { get; set; }

        [Required]
        [StringLength(50)]
        [Index("UQ_Korisnik_KorisnickoIme", IsUnique = true)]
        public string KorisnickoIme { get; set; }

        [Required]
        [StringLength(100)]
        public string Sifra { get; set; }

        [Required]
        [StringLength(50)]
        public string Ime { get; set; }

        [Required]
        [StringLength(50)]
        public string Prezime { get; set; }

        [Required]
        [StringLength(30)]
        public string Uloga { get; set; }

        public bool Aktivan { get; set; }
    }
}
