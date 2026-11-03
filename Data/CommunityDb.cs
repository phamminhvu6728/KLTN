using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext {public DbSet<CompanionAgreement> CompanionAgreements=>Set<CompanionAgreement>();}
