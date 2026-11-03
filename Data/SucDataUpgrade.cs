using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public static class SucDataUpgrade
{
    public static void Apply(AppDbContext db,IWebHostEnvironment env,bool demo)
    {
        if(db.DemoSeedMarkers.Any(x=>x.Key=="suc-v04-data"))return;
        var accounts=db.Accounts.ToList();
        foreach(var a in accounts)
        {
            if(!db.UserProfiles.Any(x=>x.OwnerId==a.Id))db.UserProfiles.Add(new(){OwnerId=a.Id});
            if(a.Role=="Provider"&&a.VerificationStatus==""){a.VerificationStatus=a.ProviderVerified?"Đã xác minh":"Chờ xác minh";if(a.ProviderVerified)a.ProviderStatus="Đang hoạt động";}
        }
        if(demo)
        {
            var demoEmails=new[]{"khach@demo.vn","ncc@demo.vn","anh@demo.vn","admin@demo.vn"};
            for(var i=0;i<demoEmails.Length;i++){var a=accounts.FirstOrDefault(x=>x.Email==demoEmails[i]);if(a!=null&&a.Phone=="")a.Phone="090000000"+(i+1);}
            var provider=accounts.FirstOrDefault(x=>x.Email=="ncc@demo.vn");var admin=accounts.FirstOrDefault(x=>x.Email=="admin@demo.vn");
            if(provider!=null)
            {
                if(!db.ProviderProfiles.Any(x=>x.OwnerId==provider.Id))db.ProviderProfiles.Add(new(){OwnerId=provider.Id,CompanyName=provider.Name,CompanyType="Cơ sở du lịch minh họa",ContactName=provider.Name,Address="Đà Lạt — dữ liệu demo",ContactPhone=provider.Phone,ContactEmail=provider.Email});
                if(!db.VerificationDossiers.Any(x=>x.OwnerId==provider.Id))
                {
                    var dossier=new VerificationDossier{OwnerId=provider.Id,Content="HỒ SƠ DEMO: thông tin minh họa để kiểm tra luồng phần mềm, không phải chứng cứ xác minh ngoài thực tế.",Representative=provider.Name,Status="Đã xác minh"};db.VerificationDossiers.Add(dossier);db.SaveChanges();
                    db.DossierSubmissions.Add(new(){DossierId=dossier.Id,Submission=0,Content=dossier.Content,Representative=dossier.Representative});
                    if(admin!=null)db.VerificationDecisions.Add(new(){DossierId=dossier.Id,AdminId=admin.Id,Sequence=1,Outcome="Đã xác minh",Reason="Khởi tạo tài khoản demo đã xác minh",Comment="Dữ liệu giả lập để trình diễn; không phải thẩm định thực tế."});
                    var source=Path.Combine(env.WebRootPath,"images","dalat.jpg");if(File.Exists(source)){var directory=Path.Combine(env.ContentRootPath,"UploadFiles");Directory.CreateDirectory(directory);var key=Guid.NewGuid().ToString("N")+".jpg";File.Copy(source,Path.Combine(directory,key));db.PrivateDocuments.Add(new(){OwnerId=provider.Id,Scope="Verification",ObjectId=dossier.Id,OriginalName="ANH-MINH-HOA-DEMO.jpg",StorageKey=key,Mime="image/jpeg",Size=new FileInfo(source).Length});}
                }
            }
            var friend=accounts.FirstOrDefault(x=>x.Email=="anh@demo.vn");
            if(friend!=null&&provider!=null)foreach(var c in db.Companions.Where(x=>x.OwnerId==provider.Id)){c.OwnerId=friend.Id;c.Name=friend.Name;}
        }
        foreach(var demand in db.Companions){if(demand.EndDate<demand.Date)demand.EndDate=demand.Date;}
        foreach(var incident in db.Incidents.Where(x=>x.Plan!="").ToList()){if(!db.IncidentPlans.Any(x=>x.IncidentId==incident.Id))db.IncidentPlans.Add(new(){IncidentId=incident.Id,Content=incident.Plan,IsCurrent=true});}
        foreach(var activity in db.Activities.Where(x=>x.SortOrder<1))activity.SortOrder=1;
        foreach(var booking in db.Bookings.Where(x=>x.Result!="").ToList())
        {
            if(db.BookingItemResults.Any(x=>x.BookingId==booking.Id))continue;
            db.BookingItemResults.Add(new(){BookingId=booking.Id,ItemKey="main",ItemTitle=string.IsNullOrWhiteSpace(booking.SnapshotName)?"Dịch vụ đã thỏa thuận":booking.SnapshotName,Outcome="Chưa xác định",Note="Ghi nhận văn bản từ bản demo trước: "+booking.Result,Feedback=booking.Feedback,FeedbackAt=booking.Feedback==""?null:DateTime.UtcNow,VerificationStatus="Chờ kiểm tra"});
            booking.Status="Chờ kiểm tra";
        }
        db.DemoSeedMarkers.Add(new(){Key="suc-v04-data"});db.SaveChanges();
    }
}

