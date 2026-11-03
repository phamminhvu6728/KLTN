using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Services;
public class GovernanceReminderService(IServiceScopeFactory scopes,ILogger<GovernanceReminderService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                var vietnam=DateTime.UtcNow.AddHours(7);if(vietnam.Hour<7)continue;
                using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var due=await db.GovernanceDecisions.Where(d=>d.Until.HasValue&&d.Until.Value.Date<=vietnam.Date&&db.Accounts.Any(a=>a.Id==d.OwnerId&&a.IsActive&&a.ProviderStatus=="Tạm đình chỉ"&&a.CurrentGovernanceDecisionId==d.Id)&&!db.GovernanceReminders.Any(r=>r.DecisionId==d.Id)).ToListAsync(stoppingToken);
                var admins=await db.Accounts.Where(a=>a.Role=="Admin"&&a.IsActive).Select(a=>a.Id).ToListAsync(stoppingToken);
                foreach(var d in due){db.GovernanceReminders.Add(new(){DecisionId=d.Id});foreach(var id in admins)db.Notifications.Add(new(){OwnerId=id,Title="Đến hạn xem xét tạm đình chỉ",Description="Quyết định #"+d.Id+" đã đến hạn. Hệ thống không tự khôi phục quyền hoạt động.",Url="/Governance/Details/"+d.OwnerId});}
                await db.SaveChangesAsync(stoppingToken);
            }
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception error){logger.LogError(error,"Chưa thể tạo nhắc xem xét quyết định đình chỉ. Sẽ thử lại ở chu kỳ tiếp theo.");}
        }while(await timer.WaitForNextTickAsync(stoppingToken));
    }
}
