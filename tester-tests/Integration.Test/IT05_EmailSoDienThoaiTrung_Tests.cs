using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;
public class IT05_EmailSoDienThoaiTrung_Tests
{
    [Fact]
    public async Task IT_05_EmailVaSoDienThoaiTrung_KhongTaoTaiKhoan()
    {
        const string id="IT-05", title="Trung email / so dien thoai khong phan biet hoa thuong";
        const string pre="KH01 da ton tai (dung tai khoan demo tuong ung de kiem thu CSDL that)";
        const string input="(a) Email viet hoa; (b) SDT trung; (c) SQL chen email trung khac chu hoa";
        const string expected="(a,b) Tu choi dang ky; (c) SQL 2601/2627; khong co tai khoan moi";
        var facts=new List<string>();var errors=new List<string>();
        try
        {
            using var factory=new IntegrationTestFactory();using var client=IT_Common.Client(factory);
            var form=await client.GetStringAsync("/Account/Register");var token=IT_Common.Token(form);
            async Task<HttpResponseMessage> Register(string email,string phone) => await client.PostAsync("/Account/Register",IT_Common.Form(new()
            {
                ["Name"]="Khach kiem thu IT05",["Email"]=email,["Phone"]=phone,["Password"]="Test@12345",
                ["ConfirmPassword"]="Test@12345",["Role"]="Traveler",["__RequestVerificationToken"]=token
            }));
            string existingEmail,existingPhone;
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var user=await db.Accounts.SingleAsync(x=>x.Email=="khach@demo.vn");
                if(string.IsNullOrEmpty(user.Phone)) {user.Phone="0911111111";await db.SaveChangesAsync();}
                existingEmail=user.Email;existingPhone=user.Phone;
            }
            var ra=await Register(existingEmail.ToUpperInvariant(),"0988776655");
            var ha=await ra.Content.ReadAsStringAsync();
            IT_Common.Check(facts,errors,ra.StatusCode==HttpStatusCode.OK&&ha.Contains("Email hoặc số điện thoại đã được sử dụng"),"(a) Email trung",$"HTTP {(int)ra.StatusCode}; Loi trung={ha.Contains("Email hoặc số điện thoại đã được sử dụng")}");
            var rb=await Register("khach8-it05@test.vn",existingPhone);
            var hb=await rb.Content.ReadAsStringAsync();
            IT_Common.Check(facts,errors,rb.StatusCode==HttpStatusCode.OK&&hb.Contains("Email hoặc số điện thoại đã được sử dụng"),"(b) So dien thoai trung",$"HTTP {(int)rb.StatusCode}; Loi trung={hb.Contains("Email hoặc số điện thoại đã được sử dụng")}");
            bool sqlRejected=false;int sqlCode=0;
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var old=await db.Accounts.AsNoTracking().SingleAsync(x=>x.Email==existingEmail);
                // Chen truc tiep qua EF Core vao SQL Server, bo qua luong chuan hoa cua AccountManager.
                var duplicate=new Account{Name="IT05 SQL",Email=existingEmail.ToUpperInvariant(),Phone="0988000555",Role="Traveler",PasswordHash=old.PasswordHash};
                db.Accounts.Add(duplicate);
                try {await db.SaveChangesAsync();}
                catch(DbUpdateException ex) when(ex.InnerException is SqlException sql)
                {sqlCode=sql.Number;sqlRejected=sqlCode is 2601 or 2627;}
                IT_Common.Check(facts,errors,sqlRejected,"(c) SQL UNIQUE",$"SQL error {sqlCode}");
            }
            using(var scope=factory.Services.CreateScope())
            {
                var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var count=await db.Accounts.CountAsync(x=>x.Email=="khach8-it05@test.vn"||x.Email=="khach@demo.vn"||x.Phone=="0988776655"||x.Phone=="0988000555");
                IT_Common.Check(facts,errors,count==1,"Khong phat sinh tai khoan",$"So dong lien quan={count}");
            }
        }
        catch(Exception ex){errors.Add("Ngoai le: "+ex.GetType().Name+" - "+ex.Message);}
        IT_Common.Finish(id,title,pre,input,expected,facts,errors);
    }
}
