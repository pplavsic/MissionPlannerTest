namespace GoogleMapsTest.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PolygonPointsAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PolygonPoints",
                c => new
                    {
                        PolygonPointPK = c.Long(nullable: false, identity: true),
                        MissionFK = c.Long(nullable: false),
                        Latitude = c.Double(nullable: false),
                        Longitude = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.PolygonPointPK)
                .ForeignKey("dbo.Missions", t => t.MissionFK, cascadeDelete: true)
                .Index(t => t.MissionFK);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PolygonPoints", "MissionFK", "dbo.Missions");
            DropIndex("dbo.PolygonPoints", new[] { "MissionFK" });
            DropTable("dbo.PolygonPoints");
        }
    }
}
