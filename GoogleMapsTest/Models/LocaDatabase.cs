namespace GoogleMapsTest.Models
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class LocaDatabase : DbContext
    {
        public virtual DbSet<FlightPoint> FlightPoints { get; set; }
        public virtual DbSet<Mission> Missions { get; set; }
        // Your context has been configured to use a 'LocaDatabase' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'GoogleMapsTest.Models.LocaDatabase' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'LocaDatabase' 
        // connection string in the application configuration file.
        public LocaDatabase()
            : base("name=LocaDatabase")
        {
            Database.SetInitializer<LocaDatabase>(new DataDbInitializer());
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
    }

    public class DataDbInitializer : CreateDatabaseIfNotExists<LocaDatabase>
    {
        protected override void Seed(LocaDatabase context)
        {
            base.Seed(context);
        }
    }
}