using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;
public class IT07_KhongTrungDeNghiKetNoi_Tests
{
    [Fact]
    public async Task IT_07_GuiDeNghi_Trung_ChoPhepGuiLaiSauTuChoi()
    {
        const string id="IT-07",title="GD-03 | Khong trung de nghi ket noi dang hieu luc";
        const string pre="KH01 dang nhap; NC01 cong khai cua KH02 dang cong bo";
        const string input="KH01 gui lan 1, gui lan 2, KH02 tu choi, KH01 gui lan 3";
        const string expected="Lan 1: Cho phan hoi + thong bao; lan 2: HTTP 409; KH02 tu choi; lan 3: de nghi Cho phan hoi moi";
        var facts=new List<string>();var errors=new List<string>();
        try
        {
            using var factory=new IntegrationTestFactory();using var sender=IT_Common.Client(factory);using var recipient=IT_Common.Client(factory);
            int demandId,senderId,recipientId;
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                senderId=(await db.Accounts.SingleAsync(x=>x.Email=="khach@demo.vn")).Id;
                recipientId=(await db.Accounts.SingleAsync(x=>x.Email=="anh@demo.vn")).Id;
                var d=new Companion {OwnerId=recipientId,Name="NC01-IT07",Destination="Đà Lạt",Description="Du lich cung nhau",
                    Date=new DateTime(2026,12,10),EndDate=new DateTime(2026,12,14),IsPublished=true,SharingScope="Public",TravelStyle="Tự túc",People=2};
                db.Companions.Add(d);await db.SaveChangesAsync();demandId=d.Id;
            }
            var a=await IT_Common.Login(sender,"khach@demo.vn");
            var b=await IT_Common.Login(recipient,"anh@demo.vn");
            IT_Common.Check(facts,errors,a.StatusCode==HttpStatusCode.Redirect&&b.StatusCode==HttpStatusCode.Redirect,"Dang nhap 2 KH",$"KH01={(int)a.StatusCode}; KH02={(int)b.StatusCode}");
            var details=await sender.GetStringAsync($"/Community/Details/{demandId}");
            var senderToken=IT_Common.Token(details);
            async Task<HttpResponseMessage> Connect()=>await sender.PostAsync("/Community/Connect",IT_Common.Form(new()
            {
                ["id"]=demandId.ToString(),["message"]="Minh cung di Da Lat dip nay",["__RequestVerificationToken"]=senderToken
            }));
            var first=await Connect();
            IT_Common.Check(facts,errors,first.StatusCode==HttpStatusCode.Redirect,"Lan 1",$"HTTP {(int)first.StatusCode}");
            int firstId=0;byte[] version=Array.Empty<byte>();
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var items=await db.Connections.AsNoTracking().Where(x=>x.CompanionId==demandId&&x.SenderId==senderId).ToListAsync();
                IT_Common.Check(facts,errors,items.Count==1&&items[0].Status=="Chờ phản hồi","De nghi lan 1",$"So de nghi={items.Count}; Trang thai={items.FirstOrDefault()?.Status}");
                if(items.Count==1){firstId=items[0].Id;version=items[0].Version;}
                var count=await db.Notifications.CountAsync(x=>x.OwnerId==recipientId&&x.Type=="DeNghiKetNoiMoi");
                IT_Common.Check(facts,errors,count==1,"Thong bao KH02",count.ToString());
            }
            if(firstId==0)throw new InvalidOperationException("Khong co de nghi lan 1 de kiem tra tiep");
            var second=await Connect();
            IT_Common.Check(facts,errors,second.StatusCode==HttpStatusCode.Conflict,"Lan 2 bi chan",$"HTTP {(int)second.StatusCode}");
            // Chủ nhu cầu từ chối qua HTTP thật với RowVersion của đề nghị.
            var dashboard=await recipient.GetStringAsync("/Community/Dashboard");
            var reject=await recipient.PostAsync("/Community/Respond",IT_Common.Form(new()
            {
                ["id"]=firstId.ToString(),["version"]=Convert.ToBase64String(version),["decision"]="reject",
                ["__RequestVerificationToken"]=IT_Common.Token(dashboard)
            }));
            IT_Common.Check(facts,errors,reject.StatusCode==HttpStatusCode.Redirect,"KH02 tu choi",$"HTTP {(int)reject.StatusCode}");
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var item=await db.Connections.AsNoTracking().SingleAsync(x=>x.Id==firstId);
                IT_Common.Check(facts,errors,item.Status=="Từ chối"&&item.RespondedAt!=null,"Luu tu choi",$"{item.Status}; RespondedAt={item.RespondedAt:O}");
            }
            var third=await Connect();
            IT_Common.Check(facts,errors,third.StatusCode==HttpStatusCode.Redirect,"Lan 3 duoc gui",$"HTTP {(int)third.StatusCode}");
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var connections=await db.Connections.AsNoTracking().Where(x=>x.CompanionId==demandId&&x.SenderId==senderId).ToListAsync();
                IT_Common.Check(facts,errors,connections.Count==2&&connections.Count(x=>x.Status=="Chờ phản hồi")==1&&connections.Count(x=>x.Status=="Từ chối")==1,
                    "Luu ket qua 3 lan",$"So de nghi={connections.Count}; Cho={connections.Count(x=>x.Status=="Chờ phản hồi")}; TuChoi={connections.Count(x=>x.Status=="Từ chối")}");
            }
        }
        catch(Exception ex){errors.Add("Ngoai le: "+ex.GetType().Name+" - "+ex.Message);}
        IT_Common.Finish(id,title,pre,input,expected,facts,errors);
    }
}
