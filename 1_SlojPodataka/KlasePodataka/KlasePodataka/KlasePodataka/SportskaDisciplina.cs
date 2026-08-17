using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("SportskaDisciplina")]
    public class SportskaDisciplina
    {
        public SportskaDisciplina()
        {
            ZahteviZaUclanjenje = new HashSet<ZahtevZaUclanjenje>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDSportskeDiscipline { get; set; }

        [Required]
        [StringLength(10)]
        [Index("UQ_SportskaDisciplina_Sifra", IsUnique = true)]
        public string Sifra { get; set; }

        [Required]
        [StringLength(60)]
        [Index("UQ_SportskaDisciplina_Naziv", IsUnique = true)]
        public string Naziv { get; set; }

        public bool Aktivna { get; set; }

        public virtual ICollection<ZahtevZaUclanjenje> ZahteviZaUclanjenje { get; set; }
    }
}

