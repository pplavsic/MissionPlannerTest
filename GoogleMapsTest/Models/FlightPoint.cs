using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GoogleMapsTest.Models
{
    public partial class FlightPoint
    {

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FlightPointPK { get; set; }
        [Required]
        [ForeignKey("Mission")]
        [Index(IsUnique = false)]
        public long MissionFK { get; set; }

        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        [Required]
        public double Height { get; set; }

        [Required]
        public Action Action { get; set; }


        public virtual Mission Mission { get; set; }
    }

    public enum Action {[Description("Take Picture")]TakePicture, [Description("Do Nothing")]NoAction }
}