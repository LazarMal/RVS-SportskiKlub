using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KlasePodataka
{
    [Table("ZahtevZaUclanjenje")]
    public class ZahtevZaUclanjenje
    {
        public ZahtevZaUclanjenje()
        {
            Dokumentacija = new HashSet<Dokumentacija>();
            RoditeljiStaratelji = new HashSet<RoditeljStaratelj>();
            IstorijaStatusa = new HashSet<IstorijaStatusaZahteva>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IDZahteva { get; set; }

        [Required]
        [StringLength(30)]
        [Index("UQ_ZahtevZaUclanjenje_Broj", IsUnique = true)]
        public string BrojZahteva { get; set; }

        [Required]
        [StringLength(13)]
        [Column(TypeName = "char")]
        public string JMBGKandidata { get; set; }

        public int IDSportskeDiscipline { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DatumPodnosenja { get; set; }

        [Required]
        [StringLength(7, MinimumLength = 7)]
        [Column(TypeName = "char")]
        public string Sezona { get; set; }

        [Required]
        [StringLength(60)]
        public string MestoKluba { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DatumSportskogPregleda { get; set; }

        [Required]
        [StringLength(20)]
        public string RezultatTestaSposobnosti { get; set; }

        [Required]
        [StringLength(20)]
        public string StatusZahteva { get; set; }

        [StringLength(500)]
        public string Napomena { get; set; }

        [ForeignKey("JMBGKandidata")]
        public virtual Kandidat Kandidat { get; set; }

        [ForeignKey("IDSportskeDiscipline")]
        public virtual SportskaDisciplina SportskaDisciplina { get; set; }

        public virtual ICollection<Dokumentacija> Dokumentacija { get; set; }

        public virtual ICollection<RoditeljStaratelj> RoditeljiStaratelji { get; set; }

        public virtual ICollection<IstorijaStatusaZahteva> IstorijaStatusa { get; set; }
    }
}
