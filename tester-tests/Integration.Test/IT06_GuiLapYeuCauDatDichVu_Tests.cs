using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;
public class IT06_GuiLapYeuCauDatDichVu_Tests
{
    [Fact]
    public async Task IT_06_GuiDongThoiVaGuiLai_MotYeuCau()
    {
        const string id="IT-06",title="GD-08 | Gui lap yeu cau dat dich vu";
        const string pre="KH01 dang nhap; DV01 duoc duyet va dang tiep nhan";
        const string input="2 POST dong thoi va 1 POST lap, cung Operation GUID; 05-07/12/2026; 1 don vi, 2 khach; 1.600.000";
        const string expected="Ca 3 redirect cung yeu cau; 1 Booking Cho phan hoi, phien ban 1; 1 thong bao YeuCauDatMoi";
        var facts=new List<string>();var errors=new List<string>();
        try
        {
            using var factory=new IntegrationTestFactory();using var client=IT_Common.Client(factory);
            int serviceId,travelerId,providerId;
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                travelerId=(await db.Accounts.SingleAsync(x=>x.Email=="khach@demo.vn")).Id;
                var provider=await db.Accounts.SingleAsync(x=>x.Email=="ncc@demo.vn");
                providerId=provider.Id;provider.VerificationStatus="Đã xác minh";provider.ProviderStatus="Đang hoạt động";provider.ProviderVerified=true;
                var service=new TravelService{OwnerId=providerId,Name="IT06 - Phong doi",Category="Lưu trú",Price=1600000,ApprovalStatus="Đã duyệt",IsAccepting=true,IsPublished=true,Location="Đà Lạt"};
                db.Services.Add(service);await db.SaveChangesAsync();serviceId=service.Id;
            }
            var login=await IT_Common.Login(client,"khach@demo.vn");
            IT_Common.Check(facts,errors,login.StatusCode==HttpStatusCode.Redirect,"Dang nhap",$"HTTP {(int)login.StatusCode}");
            var detail=await client.GetStringAsync($"/Marketplace/Details/{serviceId}");
            var preview=await client.PostAsync("/Marketplace/Preview",IT_Common.Form(new()
            {
                ["Service"]=serviceId.ToString(),["Date"]="2026-12-05",["EndDate"]="2026-12-07",["People"]="1",["Guests"]="2",
                ["__RequestVerificationToken"]=IT_Common.Token(detail)
            }));
            var previewHtml=await preview.Content.ReadAsStringAsync();
            IT_Common.Check(facts,errors,preview.StatusCode==HttpStatusCode.OK,"Preview",$"HTTP {(int)preview.StatusCode}");
            if(preview.StatusCode!=HttpStatusCode.OK)throw new InvalidOperationException("Khong tao duoc ban xem truoc: "+previewHtml[..Math.Min(previewHtml.Length,250)]);
            string Hidden(string key)
            {
                var match=Regex.Match(previewHtml,"<input\\b[^>]*name=\""+Regex.Escape(key)+"\"[^>]*>",RegexOptions.IgnoreCase);
                if(!match.Success)throw new InvalidOperationException("Thieu truong form: "+key);
                var val=Regex.Match(match.Value,"value=\"([^\"]*)\"");
                return val.Success?WebUtility.HtmlDecode(val.Groups[1].Value):"";
            }
            var fields=new Dictionary<string,string>();
            foreach(var key in new[]{"PreviewToken","Service","Operation","Version","Date","EndDate","People","Guests","Total","Options","ArrivalDeparture","Note"})fields[key]=Hidden(key);
            fields["__RequestVerificationToken"]=IT_Common.Token(previewHtml);
            IT_Common.Check(facts,errors,fields["Total"]=="1600000","Tong du kien",fields["Total"]);
            // Gửi HTTP thật đồng thời, không dùng cùng một HttpContent cho hai yêu cầu.
            async Task<HttpResponseMessage> Send()=>await client.PostAsync("/Marketplace/Book",IT_Common.Form(new(fields)));
            var pending1=Send();var pending2=Send();
            var both=await Task.WhenAll(pending1,pending2);
            var third=await Send();
            var all=both.Append(third).ToArray();
            var statuses=string.Join(",",all.Select(x=>(int)x.StatusCode));
            IT_Common.Check(facts,errors,all.All(x=>x.StatusCode==HttpStatusCode.Redirect),"3 HTTP 302",statuses);
            var locations=all.Select(x=>x.Headers.Location?.ToString()??"").ToArray();
            IT_Common.Check(facts,errors,locations.All(x=>x.Length>0)&&locations.Distinct().Count()==1,"Cung URL yeu cau",string.Join(" | ",locations));
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var operation=Guid.Parse(fields["Operation"]);
                var bookings=await db.Bookings.AsNoTracking().Where(x=>x.OperationId==operation&&x.OwnerId==travelerId).ToListAsync();
                IT_Common.Check(facts,errors,bookings.Count==1,"So Booking",bookings.Count.ToString());
                if(bookings.Count==1)
                {
                    var b=bookings[0];
                    IT_Common.Check(facts,errors,b.Status=="Chờ phản hồi"&&b.ConditionsVersion==1&&b.Total==1600000,"Trang thai/phien ban/chi phi",$"{b.Status}; version={b.ConditionsVersion}; total={b.Total}");
                    var count=await db.Notifications.CountAsync(x=>x.OwnerId==providerId&&x.Type=="YeuCauDatMoi"&&x.Url.Contains(b.Id.ToString()));
                    IT_Common.Check(facts,errors,count==1,"Thong bao NCC",count.ToString());
                }
            }
        }
        catch(Exception ex){errors.Add("Ngoai le: "+ex.GetType().Name+" - "+ex.Message);}
        IT_Common.Finish(id,title,pre,input,expected,facts,errors);
    }
}
