using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GoogleMapsTest.Models
{
    public partial class PolygonPoint
    {

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PolygonPointPK { get; set; }
        [Required]
        [ForeignKey("Mission")]
        [Index(IsUnique = false)]
        public long MissionFK { get; set; }

        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        
        public virtual Mission Mission { get; set; }
    }
}