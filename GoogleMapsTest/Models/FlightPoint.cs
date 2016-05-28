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
        public float Altitude { get; set; }
        public float Damping_distance { get; set; }
        public Int16 Target_yaw { get; set; }
        public Int16 Target_gimbal_pitch { get; set; }
        public TurnMode Turn_mode { get; set; }
        public HasAction Has_action { get; set; }
        public Int16 Action_time_limit { get; set; }
        public Int64 ACTION_action_repeat { get; set; }
        public string _ACTION_COMMAND_LIST { get; set; }
        public string _ACTION_COMMAND_PARAMS { get; set; }


        public List<Int32> ACTION_Command_list { get { return _ACTION_COMMAND_LIST.Split(';').Select(s=> Int32.Parse(s)).ToList(); } set { _ACTION_COMMAND_LIST = String.Join(";", value); } }
        public List<Int32> ACTION_Param_list { get { return _ACTION_COMMAND_PARAMS.Split(';').Select(s => Int32.Parse(s)).ToList(); } set { _ACTION_COMMAND_PARAMS = String.Join(";", value); } }

        [Required]
        public PIAction PIAction { get; set; }
        public virtual Mission Mission { get; set; }
    }
    //ROS
    /*
                waypoint.latitude = 22.540091;
				waypoint.longitude = 113.946593;
				waypoint.altitude = 100;
				waypoint.damping_distance = 0;
				waypoint.target_yaw = 0;
				waypoint.target_gimbal_pitch = 0;
				waypoint.turn_mode = 0;
				waypoint.has_action = 0;
				waypoint.action_time_limit = 10;
				
                waypoint.waypoint_action.action_repeat = 1;
				waypoint.waypoint_action.command_list[0] = 1;
				waypoint.waypoint_action.command_parameter[0] = 1;
    */
    //Ground station
    /*
    typedef struct {
    double latitude;//waypoint latitude (radian)
    double longitude;//waypoint longitude (radian)
    float altitude;//waypoint altitude (relative altitude from takeoff point)
    float damping_dis;//bend length (effective coordinated turn mode only)
    int16_t tgt_yaw;//waypoint yaw (degree)
    int16_t tgt_gimbal_pitch;//waypoint gimbal pitch
    uint8_t turn_mode;//turn mode
                    //0: clockwise
                    //1: counter-clockwise
    uint8_t resv[8];//reserved

    uint8_t has_action;//waypoint action flag
                    //0: no action
                    //1: has action
    uint16_t action_time_limit;//waypoint action time limit unit:s
    waypoint_action_comm_t action;//waypoint action

    } waypoint_comm_t

    typedef struct {
    uint8_t action_num :4;//total number of actions
    uint8_t action_rpt :4;//total running times

    uint8_t command_list[15];//command list, 15 at most
    int16_t command_param[15];//command param, 15 at most

    }waypoint_action_comm_t

*/

    public enum PIAction {[Description("Take Picture")]TakePicture, [Description("Do Nothing")]NoAction }
    public enum TurnMode {[Description("Clockwise")]Clockwise, [Description("Counter-clockwise")]Counter_clockwise }
    public enum HasAction {[Description("No action")]No_Action, [Description("Has Action")]Has_Action }
}