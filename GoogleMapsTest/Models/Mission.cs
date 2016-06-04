using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [Required]
        public MissionStatus MissionStatus { get; set; }
        [Required]
        public float velocity_range { get; set; }
        [Required]
        public float idle_velocity { get; set; }
        [Required]
        public ActionOnFinish action_on_finish { get; set; }
        [Required]
        public MissionExecNum mission_exec_times { get; set; }
        [Required]
        public YawMode yaw_mode { get; set; }
        [Required]
        public TraceMode trace_mode { get; set; }
        [Required]
        public ActionOnRCLost action_on_rc_lost { get; set; }
        [Required]
        public GimbalPitchMode gimbal_pitch_mode { get; set; }


        [InverseProperty("Mission")]
        public List<FlightPoint> FlightPoints { get; set; }
        //ROS
        /*
        		waypoint_task.velocity_range = 10;
				waypoint_task.idle_velocity = 3;
				waypoint_task.action_on_finish = 0;
				waypoint_task.mission_exec_times = 1;
				waypoint_task.yaw_mode = 4;
				waypoint_task.trace_mode = 0;
				waypoint_task.action_on_rc_lost = 0;
				waypoint_task.gimbal_pitch_mode = 0;

    */
        //Ground station
        /*
        typedef struct {

    uint8_t length;//count of waypoints
    float vel_cmd_range;//Maximum speed joystick input(2~15m)
    float idle_vel;//Cruising Speed (without joystick input, no more than vel_cmd_range)
    uint8_t action_on_finish;//Action on finish
                            //0: no action
                            //1: return to home
                            //2: auto landing
                            //3: return to point 0
                            //4: infinite mode， no exit
    uint8_t mission_exec_num;//Function execution times
                            //1: once
                            //2: twice
    uint8_t yaw_mode;//Yaw mode
                    //0: auto mode(point to next waypoint)
                    //1: Lock as an initial value
                    //2: controlled by RC
                    //3: use waypoint's yaw(tgt_yaw)
    uint8_t trace_mode;//Trace mode
                    //0: point to point, after reaching the target waypoint hover, complete waypoints action (if any), then fly to the next waypoint
                    //1: Coordinated turn mode, smooth transition between waypoints, no waypoints task
    uint8_t action_on_rc_lost;//Action on rc lost
                            //0: exit waypoint and failsafe
                            //1: continue the waypoint
    uint8_t gimbal_pitch_mode;//Gimbal pitch mode
                            //0: Free mode, no control on gimbal
                            //1: Auto mode, Smooth transition between waypoints
    double hp_lati;//Focus latitude (radian)
    double hp_longti;//Focus longitude (radian)
    float hp_alti;//Focus altitude (relative takeoff point height)
    uint8_t resv[16];//reserved, must be set as 0

}waypoint_mission_info_comm_t;
    */
    }
    /*
    0 - //
    1 - UploadMission - Graficka
    2 - UploadedMissionSuccess - PI
    3 - UploadedMissionFailed - PI
    4 - StartMisson Graficka
    5 - CancelMission Graficka
    6 - MissionSuccess PI

    */

    public enum MissionStatus
    {
        [Description("NotInUse")]
        NotInUse,
        [Description("UploadMission")]
        UploadMission,
        [Description("UploadedMissionSuccess")]
        UploadedMissionSuccess,
        [Description("UploadedMissionFailed")]
        UploadedMissionFailed,
        [Description("StartMisson")]
        StartMisson,
        [Description("CancelMission")]
        CancelMission,
        [Description("MissionSuccess")]
        MissionSuccess
    }


    public enum ActionOnFinish
    {
        [Description("No action")]
        No_action,
        [Description("Return to home")]
        Return_to_home,
        [Description("Auto landing")]
        Auto_landing,
        [Description("Return to point 0")]
        Return_to_point_0,
        [Description("Infinite mode")]
        Infinite_mode
    }
    public enum MissionExecNum
    {
        [Description("N/A")]
        NA,
        [Description("Once")]
        Once,
        [Description("Twice")]
        Twice
    }
    public enum YawMode
    {
        [Description("Auto mode(point to next waypoint)")]
        Auto_mode,
        [Description("Lock as an initial value")]
        Lock_as_an_initial_value,
        [Description("Controlled by RC")]
        Controlled_by_RC,
        [Description("Use waypoint's yaw(tgt_yaw)")]
        Use_waypoints
    }
    public enum TraceMode
    {
        [Description("Point to point, after reaching the target waypoint hover, complete waypoints action (if any), then fly to the next waypoint")]
        Point_to_point,
        [Description("Coordinated turn mode, smooth transition between waypoints, no waypoints task")]
        Coordinated
    }
    public enum ActionOnRCLost
    {
        [Description("Exit waypoint and failsafe")]
        Exit_waypoint_and_failsafe,
        [Description("Continue the waypoint")]
        Continue_the_waypoint
    }
    public enum GimbalPitchMode
    {
        [Description("Free mode, no control on gimbal")]
        Free,
        [Description("Auto mode, Smooth transition between waypoints")]
        Smooth
    }
}