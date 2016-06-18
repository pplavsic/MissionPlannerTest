namespace GoogleMapsTest.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FlightPoints",
                c => new
                    {
                        FlightPointPK = c.Long(nullable: false, identity: true),
                        MissionFK = c.Long(nullable: false),
                        Latitude = c.Double(nullable: false),
                        Longitude = c.Double(nullable: false),
                        Altitude = c.Single(nullable: false),
                        Damping_distance = c.Single(nullable: false),
                        Target_yaw = c.Short(nullable: false),
                        Target_gimbal_pitch = c.Short(nullable: false),
                        Turn_mode = c.Int(nullable: false),
                        Has_action = c.Int(nullable: false),
                        Action_time_limit = c.Short(nullable: false),
                        ACTION_action_repeat = c.Long(nullable: false),
                        _ACTION_COMMAND_LIST = c.String(),
                        _ACTION_COMMAND_PARAMS = c.String(),
                        PIAction = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.FlightPointPK)
                .ForeignKey("dbo.Missions", t => t.MissionFK, cascadeDelete: true)
                .Index(t => t.MissionFK);
            
            CreateTable(
                "dbo.Missions",
                c => new
                    {
                        MissionPK = c.Long(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 250),
                        MissionStatus = c.Int(nullable: false),
                        velocity_range = c.Single(nullable: false),
                        idle_velocity = c.Single(nullable: false),
                        action_on_finish = c.Int(nullable: false),
                        mission_exec_times = c.Int(nullable: false),
                        yaw_mode = c.Int(nullable: false),
                        trace_mode = c.Int(nullable: false),
                        action_on_rc_lost = c.Int(nullable: false),
                        gimbal_pitch_mode = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MissionPK);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FlightPoints", "MissionFK", "dbo.Missions");
            DropIndex("dbo.FlightPoints", new[] { "MissionFK" });
            DropTable("dbo.Missions");
            DropTable("dbo.FlightPoints");
        }
    }
}
