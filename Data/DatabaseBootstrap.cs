using Microsoft.EntityFrameworkCore;
namespace TuTuc.Data;
public static class DatabaseBootstrap
{
    public static void Initialize(AppDbContext db)
    {
        var migrations=db.Database.GetMigrations().ToList();
        if(migrations.Count==0){DemoSchemaUpgrade.Apply(db);return;}
        var legacy=false;
        if(db.Database.CanConnect())
        {
            db.Database.OpenConnection();
            try{using var command=db.Database.GetDbConnection().CreateCommand();command.CommandText="SELECT CASE WHEN OBJECT_ID(N'[Accounts]',N'U') IS NOT NULL AND OBJECT_ID(N'[__EFMigrationsHistory]',N'U') IS NULL THEN 1 ELSE 0 END";legacy=Convert.ToInt32(command.ExecuteScalar())==1;}
            finally{db.Database.CloseConnection();}
        }
        if(legacy)
        {
            DemoSchemaUpgrade.Apply(db);
            db.Database.ExecuteSqlRaw("CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);");
            var baseline=migrations.First();var productVersion="10.0.0";
            db.Database.ExecuteSqlInterpolated($"INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES ({baseline},{productVersion});");
        }
        db.Database.Migrate();
    }
}
