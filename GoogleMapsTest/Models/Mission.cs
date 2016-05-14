using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GoogleMapsTest.Models
{
    public partial class Mission
    {

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MissionPK { get; set; }
        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [InverseProperty("Mission")]
        public List<FlightPoint> FlightPoints { get; set; }
    }
}