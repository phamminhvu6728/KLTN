using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext {public DbSet<IncidentActivity> IncidentActivities=>Set<IncidentActivity>();public DbSet<IncidentPlan> IncidentPlans=>Set<IncidentPlan>();}
