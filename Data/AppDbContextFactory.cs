using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace TuTuc.Data;
public class AppDbContextFactory:IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TuTucDemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True").Options;
        return new AppDbContext(options,new HttpContextAccessor());
    }
}
