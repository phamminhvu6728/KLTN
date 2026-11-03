using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext
{
    public DbSet<GovernanceDecision> GovernanceDecisions=>Set<GovernanceDecision>();
    public DbSet<ProviderExplanation> ProviderExplanations=>Set<ProviderExplanation>();
    public DbSet<GovernanceReminder> GovernanceReminders=>Set<GovernanceReminder>();
}
