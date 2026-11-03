using System.Data;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Services;
public class GovernanceManager(AppDbContext db, PrivateDocumentStore documents)
{
    static readonly string[] PendingBookings = ["Chờ phản hồi", "Đề xuất khác", "Chờ NCC xác nhận cuối", "Chờ xác nhận"];
    public async Task<GovernanceList> List(string? filter)
    {
        var providers=await db.Accounts.AsNoTracking().Where(x=>x.Role=="Provider"&&(x.VerificationStatus=="Đã xác minh"||x.VerificationStatus=="Đã thu hồi")).OrderBy(x=>x.Name).ToListAsync();
        var explanations=await db.ProviderExplanations.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync();
        var decisions=await db.GovernanceDecisions.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync();
        if(filter=="review") providers=providers.Where(p=>explanations.Any(e=>e.OwnerId==p.Id&&e.Status=="Chờ xem xét")||decisions.Any(d=>d.Id==p.CurrentGovernanceDecisionId&&d.Until.HasValue&&d.Until.Value.Date<=DateTime.UtcNow.AddHours(7).Date&&p.ProviderStatus=="Tạm đình chỉ")).ToList();
        else if(!string.IsNullOrEmpty(filter))providers=providers.Where(x=>x.ProviderStatus==filter).ToList();
        return new(){Providers=providers,Explanations=explanations,Decisions=decisions,Filter=filter??""};
    }
    public async Task<GovernanceDetail?> Detail(int owner,bool admin)
    {
        var provider=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==owner&&x.Role=="Provider");
        if(provider==null)return null;
        var explanations=await db.ProviderExplanations.AsNoTracking().Where(x=>x.OwnerId==owner).OrderByDescending(x=>x.Id).ToListAsync();
        var explanationIds=explanations.Select(x=>x.Id).ToList();
        return new(){Provider=provider,IsAdmin=admin,Decisions=await db.GovernanceDecisions.AsNoTracking().Where(x=>x.OwnerId==owner).OrderByDescending(x=>x.Id).ToListAsync(),
            Explanations=explanations,Services=await db.Services.AsNoTracking().Where(x=>x.OwnerId==owner).ToListAsync(),
            Bookings=await db.Bookings.AsNoTracking().Include(x=>x.TravelService).Where(x=>x.TravelService!.OwnerId==owner).OrderByDescending(x=>x.Id).ToListAsync(),
            Documents=await db.PrivateDocuments.AsNoTracking().Where(x=>x.OwnerId==owner&&x.Scope=="Explanation"&&explanationIds.Contains(x.ObjectId)).ToListAsync(),
            Input=new(){ProviderId=owner,Version=Convert.ToBase64String(provider.Version)}};
    }
    public async Task<(GovernancePreview? Preview,string Error)> Preview(int admin,GovernanceInput input)
    {
        if(!await IsAdmin(admin))return(null,"Bạn không có quyền quản lý hoạt động nhà cung cấp.");
        var p=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.ProviderId&&x.Role=="Provider");
        var error=await Validate(p,input);if(error.Length>0)return(null,error);
        var serviceIds=db.Services.Where(x=>x.OwnerId==input.ProviderId).Select(x=>x.Id);
        return(new(){OperationId=Guid.NewGuid(),Input=input,ProviderName=p!.Name,Before=p.ProviderStatus!,After=After(input.Action,p.ProviderStatus!),
            ServiceCount=await serviceIds.CountAsync(),PendingCount=await db.Bookings.CountAsync(x=>serviceIds.Contains(x.TravelServiceId)&&PendingBookings.Contains(x.Status)),
            ConfirmedCount=await db.Bookings.CountAsync(x=>serviceIds.Contains(x.TravelServiceId)&&(x.Status=="Đã xác nhận"||x.Status=="Chờ kiểm tra"))},"");
    }
    async Task<string> Validate(Account? p,GovernanceInput input)
    {
        if(p==null||!p.IsActive)return "Nhà cung cấp không tồn tại hoặc tài khoản đã ngừng sử dụng.";
        if(string.IsNullOrWhiteSpace(input.Reason)||string.IsNullOrWhiteSpace(input.Evidence)||input.Reason.Length>2000||input.Evidence.Length>3000)return "Cần nhập lý do và căn cứ hợp lệ trước khi ghi quyết định.";
        byte[] version;try{version=Convert.FromBase64String(input.Version??"");}catch(FormatException){return "Phiên bản không hợp lệ.";}
        if(!version.SequenceEqual(p.Version))return "Trạng thái nhà cung cấp đã thay đổi. Hãy mở lại thông tin mới nhất.";
        if(p.VerificationStatus!="Đã xác minh"||p.ProviderStatus is not ("Đang hoạt động" or "Tạm đình chỉ"))return "Chỉ quản lý quyền của nhà cung cấp đã xác minh; hồ sơ chưa xác minh hoặc đã thu hồi cần xử lý tại Xác minh nhà cung cấp.";
        if(input.Action=="Suspend"&&p.ProviderStatus!="Đang hoạt động"||input.Action is "Restore" or "Keep"&&p.ProviderStatus!="Tạm đình chỉ")return "Thao tác không phù hợp với quyền hoạt động hiện hành.";
        if(input.Action is not ("Suspend" or "Restore" or "Revoke" or "Keep"))return "Lựa chọn không hợp lệ.";
        if(input.Action=="Suspend"&&input.Until.HasValue&&input.Until.Value.Date<DateTime.UtcNow.AddHours(7).Date)return "Thời hạn đình chỉ không được ở trước ngày hiện tại.";
        if(input.ExplanationId.HasValue&&!await db.ProviderExplanations.AnyAsync(x=>x.Id==input.ExplanationId&&x.OwnerId==p.Id&&x.Status=="Chờ xem xét"))return "Giải trình không còn chờ xem xét hoặc không thuộc nhà cung cấp này.";
        return "";
    }
    public async Task<string> Confirm(int admin,GovernancePreview preview)
    {
        if(!await IsAdmin(admin)||preview.ExpiresAt<DateTime.UtcNow)return "Phiên xác nhận đã hết hạn hoặc bạn không có quyền. Hãy xem lại hệ quả.";
        if(await db.GovernanceDecisions.AnyAsync(x=>x.OperationId==preview.OperationId))return "Quyết định này đã được ghi nhận trước đó.";
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var input=preview.Input;
        var p=await db.Accounts.SingleOrDefaultAsync(x=>x.Id==input.ProviderId&&x.Role=="Provider");
        var error=await Validate(p,input);if(error.Length>0)return error;
        db.Entry(p!).Property(x=>x.Version).OriginalValue=Convert.FromBase64String(input.Version);
        var previousDecision=p!.CurrentGovernanceDecisionId;
        var decision=new GovernanceDecision{OperationId=preview.OperationId,OwnerId=p.Id,AdminId=admin,Action=input.Action,Before=p.ProviderStatus!,After=After(input.Action,p.ProviderStatus!),Reason=input.Reason.Trim(),Evidence=input.Evidence.Trim(),Until=input.Action=="Suspend"?input.Until:null};
        db.GovernanceDecisions.Add(decision);
        try
        {
            await db.SaveChangesAsync();
            var services=await db.Services.Where(x=>x.OwnerId==p.Id).ToListAsync();
            var ids=services.Select(x=>x.Id).ToList();
            if(input.Action is "Suspend" or "Revoke")
            {
                foreach(var s in services)
                {
                    if(s.IsAccepting){s.IsAccepting=false;s.BlockedByDecisionId=decision.Id;}
                    if(input.Action=="Revoke")s.IsPublished=false;
                }
                var pending=await db.Bookings.Where(x=>ids.Contains(x.TravelServiceId)&&PendingBookings.Contains(x.Status)).ToListAsync();
                foreach(var b in pending){b.Status="Không tiếp tục";b.Note+="\nYêu cầu kết thúc do quyết định quyền hoạt động #"+decision.Id+". Thỏa thuận mới chưa được xác nhận.";}
                if(input.Action=="Revoke")
                {
                    p.VerificationStatus="Đã thu hồi";p.ProviderVerified=false;
                    var revisions=await db.ServiceRevisions.Where(x=>ids.Contains(x.TravelServiceId)&&x.Status=="Chờ duyệt").ToListAsync();
                    foreach(var revision in revisions){revision.Status="Từ chối";revision.ReviewNote="Quyền hoạt động nhà cung cấp đã bị thu hồi. "+decision.Reason;revision.ReviewedById=admin;revision.ReviewedAt=DateTime.UtcNow;}
                }
            }
            if(input.Action=="Restore")
                foreach(var s in services.Where(x=>x.BlockedByDecisionId==previousDecision)){s.IsAccepting=true;s.BlockedByDecisionId=null;}
            p.ProviderStatus=decision.After;
            if(input.Action!="Keep")p.CurrentGovernanceDecisionId=decision.Id;
            if(input.ExplanationId.HasValue)
            {
                var explanation=await db.ProviderExplanations.SingleAsync(x=>x.Id==input.ExplanationId);
                explanation.Status="Đã xem xét";explanation.ReviewerId=admin;explanation.ReviewedAt=DateTime.UtcNow;explanation.ReviewReason=decision.Reason;explanation.ReviewEvidence=decision.Evidence;
            }
            db.Notifications.Add(new(){OwnerId=p.Id,Title="Quyết định quyền hoạt động: "+decision.After,Description=decision.Reason+" · Căn cứ: "+decision.Evidence,Url="/Governance/Mine"});
            await db.SaveChangesAsync();await tx.CommitAsync();return "Đã ghi quyết định và cập nhật đồng bộ. Các thỏa thuận đã xác nhận được giữ nguyên.";
        }
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync();return "Trạng thái đã thay đổi ở phiên khác. Không có quyết định nào được áp dụng; hãy tải lại.";}
        catch(DbUpdateException){await tx.RollbackAsync();return "Chưa thể cập nhật đồng bộ. Tất cả thay đổi của quyết định đã được hoàn tác.";}
    }
    public async Task<string> Explain(int owner,string? content,IEnumerable<IFormFile>? files)
    {
        if(string.IsNullOrWhiteSpace(content)||content.Length>5000)return "Nhập nội dung giải trình từ 1 đến 5000 ký tự.";
        var p=await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==owner&&x.IsActive&&x.Role=="Provider");
        if(p?.ProviderStatus!="Tạm đình chỉ"||!p.CurrentGovernanceDecisionId.HasValue)return "Chỉ nhà cung cấp đang bị tạm đình chỉ mới gửi giải trình cho quyết định hiện hành.";
        var staged=await documents.StageAsync(owner,"Explanation",files,false);if(staged.Error.Length>0)return staged.Error;
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            p=await db.Accounts.AsNoTracking().SingleAsync(x=>x.Id==owner);
            if(!p.IsActive||p.ProviderStatus!="Tạm đình chỉ"||!p.CurrentGovernanceDecisionId.HasValue){documents.CleanupFiles(staged.Documents);return "Quyền hoạt động vừa thay đổi. Hãy mở lại trang.";}
            if(await db.ProviderExplanations.AnyAsync(x=>x.OwnerId==owner&&x.DecisionId==p.CurrentGovernanceDecisionId&&x.Status=="Chờ xem xét")){documents.CleanupFiles(staged.Documents);return "Bạn đã có giải trình đang chờ Admin xem xét cho quyết định này.";}
            var explanation=new ProviderExplanation{OwnerId=owner,DecisionId=p.CurrentGovernanceDecisionId.Value,Content=content.Trim()};db.ProviderExplanations.Add(explanation);await db.SaveChangesAsync();
            foreach(var d in staged.Documents){d.ObjectId=explanation.Id;db.PrivateDocuments.Add(d);}
            foreach(var admin in await db.Accounts.Where(x=>x.Role=="Admin"&&x.IsActive).Select(x=>x.Id).ToListAsync())db.Notifications.Add(new(){OwnerId=admin,Title="Có giải trình mới của "+p.Name,Description="Quyền hoạt động giữ nguyên cho đến khi Admin có quyết định.",Url="/Governance/Details/"+owner});
            await db.SaveChangesAsync();await tx.CommitAsync();return "Đã gửi giải trình. Quyền hoạt động chưa thay đổi; chờ Admin xem xét.";
        }
        catch(Exception e)when(e is DbUpdateException or IOException){await tx.RollbackAsync();documents.CleanupFiles(staged.Documents);return "Chưa thể lưu giải trình và tài liệu. Vui lòng thử lại.";}
    }
    Task<bool> IsAdmin(int id)=>db.Accounts.AnyAsync(x=>x.Id==id&&x.Role=="Admin"&&x.IsActive);
    static string After(string action,string current)=>action switch{"Suspend"=>"Tạm đình chỉ","Restore"=>"Đang hoạt động","Revoke"=>"Đã thu hồi",_=>current};
}

