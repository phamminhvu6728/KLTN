using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;

namespace TuTuc.Tests;

// Real SQL Server, Kestrel, cookies and antiforgery. Never uses TuTucDemo.
public class SystemTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CompleteSUCWorkflowsAgainstIsolatedSqlDatabase()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var database = "TuTucTest_" + Guid.NewGuid().ToString("N");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(root);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True",
                ["Demo:Enabled"] = "true",
                ["Storage:UploadDirectory"] = Path.Combine(root,"TestResults",database,"UploadFiles")
            }));
        });
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        output.WriteLine("Isolated database: " + database);
        foreach (var script in new[] { "smoke-test.mjs", "journey-community-test.mjs", "marketplace-suc-test.mjs", "account-verification-suc-test.mjs", "security-search-test.mjs", "planning-request-deep-test.mjs", "design-alignment-test.mjs" })
        {
            var info = new ProcessStartInfo("node") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            info.ArgumentList.Add(script);
            info.Environment["DEMO_URL"] = client.BaseAddress!.ToString().TrimEnd('/');
            info.Environment["BASE_URL"] = info.Environment["DEMO_URL"];
            using var process = Process.Start(info)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMinutes(4)).Token);
            output.WriteLine(script + "\n" + await stdout + await stderr);
            Assert.True(process.ExitCode == 0, script + " failed. See test output.");
        }
        await PublicationChecks(factory,client.BaseAddress!);
        await GovernanceChecks(factory);
        await DetailedDesignDataChecks(factory);
        // Retain the isolated test DB for evidence/debugging; no automatic destructive cleanup.
    }

    private async Task DetailedDesignDataChecks(WebApplicationFactory<Program> factory)
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var offers=await db.AuditEntries.Where(x=>x.EventCode=="SK11").ToListAsync();
        Assert.NotEmpty(offers);
        Assert.All(offers,x=>{Assert.NotEmpty(x.Before);Assert.NotEmpty(x.After);});
        Assert.Contains(await db.Notifications.ToListAsync(),x=>x.Type=="YeuCauCoDeXuatKhac");
        Assert.Contains(await db.Notifications.ToListAsync(),x=>x.Type=="YeuCauDaXacNhan");
        var optional=await db.Trips.Include(x=>x.Activities).SingleAsync(x=>x.Name.StartsWith("Design nullable "));
        Assert.Null(optional.Budget);
        Assert.Contains(optional.Activities,x=>!x.Cost.HasValue);
        Assert.Equal(new[]{1,2},optional.Activities.OrderBy(x=>x.SortOrder).Select(x=>x.SortOrder));
        output.WriteLine("PASS detailed design SQL evidence: nullable amounts, per-day order, SK11 before/after, typed transactional notifications.");
    }

    private async Task PublicationChecks(WebApplicationFactory<Program> factory,Uri address)
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var provider=await db.Accounts.SingleAsync(x=>x.Email=="ncc@demo.vn");
        var user=await db.Accounts.SingleAsync(x=>x.Email=="khach@demo.vn");
        var draft=new TravelService{OwnerId=provider.Id,Name="PRIVATE UNAPPROVED CONTENT",ApprovalStatus="Chờ duyệt",IsPublished=true};
        db.Services.Add(draft);await db.SaveChangesAsync();
        var journey=scope.ServiceProvider.GetRequiredService<JourneyManager>();
        var trip=await journey.Save(user.Id,new(){Name="Publication regression",Destination="Đà Lạt",Start=MarketplaceManager.Today.AddDays(1),End=MarketplaceManager.Today.AddDays(2)});
        var detail=(await journey.Detail(user.Id,trip.Id))!;
        Assert.DoesNotContain(detail.Services,x=>x.Id==draft.Id);
        Assert.DoesNotContain(await scope.ServiceProvider.GetRequiredService<TravelSearchManager>().Search(new()),x=>x.SourceType=="Service"&&x.Id==draft.Id);
        var activity=await journey.Activity(user.Id,new(){TripId=trip.Id,Version=Convert.ToBase64String(detail.Trip.Version),SourceType="Service",SourceId=draft.Id,Start=detail.Trip.Start.AddHours(8),End=detail.Trip.Start.AddHours(9)});
        Assert.False(activity.Success);
        using var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=true}){BaseAddress=address};
        var login=await client.GetStringAsync("/Account/Login");
        var token=System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(login,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
        var response=await client.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","khach@demo.vn"},{"Password","Demo12345"},{"__RequestVerificationToken",token}}));
        Assert.Equal(System.Net.HttpStatusCode.Redirect,response.StatusCode);
        var profile=await client.GetStringAsync("/Account/Profile");
        token=System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(profile,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
        response=await client.PostAsync("/Marketplace/Preview",new FormUrlEncodedContent(new Dictionary<string,string>{{"Service",draft.Id.ToString()},{"Date",MarketplaceManager.Today.ToString("yyyy-MM-dd")},{"__RequestVerificationToken",token}}));
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity,response.StatusCode);
        Assert.Equal("0701-01",response.Headers.GetValues("X-Business-Error").Single());
        output.WriteLine("PASS publication regression: pending services excluded from journey, activity input, search and forged booking preview.");
    }

    private async Task GovernanceChecks(WebApplicationFactory<Program> factory)
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var manager=scope.ServiceProvider.GetRequiredService<GovernanceManager>();
        var admin=await db.Accounts.SingleAsync(x=>x.Email=="admin@demo.vn");
        var provider=await db.Accounts.SingleAsync(x=>x.Email=="ncc@demo.vn");
        var traveler=await db.Accounts.SingleAsync(x=>x.Email=="khach@demo.vn");
        var service=await db.Services.FirstAsync(x=>x.OwnerId==provider.Id&&x.IsPublished);
        var pending=new Booking{OwnerId=traveler.Id,TravelServiceId=service.Id,OperationId=Guid.NewGuid(),Date=MarketplaceManager.Today.AddDays(20),Status="Chờ phản hồi",SnapshotName="Governance pending"};
        var confirmed=new Booking{OwnerId=traveler.Id,TravelServiceId=service.Id,OperationId=Guid.NewGuid(),Date=MarketplaceManager.Today.AddDays(20),Status="Đã xác nhận",SnapshotName="Governance confirmed"};
        db.Bookings.AddRange(pending,confirmed);await db.SaveChangesAsync();
        GovernanceInput Input(string action)=>new(){ProviderId=provider.Id,Action=action,Reason="Test decision",Evidence="Test evidence",Version=Convert.ToBase64String(provider.Version)};
        Assert.Null((await manager.Preview(traveler.Id,Input("Suspend"))).Preview);
        var invalid=Input("Suspend");invalid.Reason="";Assert.Null((await manager.Preview(admin.Id,invalid)).Preview);
        invalid=Input("Suspend");invalid.Version="bad";Assert.Null((await manager.Preview(admin.Id,invalid)).Preview);
        invalid=Input("Suspend");invalid.Until=MarketplaceManager.Today.AddDays(-1);Assert.Null((await manager.Preview(admin.Id,invalid)).Preview);
        Assert.Null((await manager.Preview(admin.Id,Input("Restore"))).Preview);
        var preview=(await manager.Preview(admin.Id,Input("Suspend"))).Preview!;
        Assert.NotNull(preview);Assert.True(preview.PendingCount>0);Assert.True(preview.ConfirmedCount>0);
        Assert.Equal("Đang hoạt động",provider.ProviderStatus);
        var expired=(await manager.Preview(admin.Id,Input("Suspend"))).Preview!;expired.ExpiresAt=DateTime.UtcNow.AddMinutes(-1);
        Assert.Contains("hết hạn",await manager.Confirm(admin.Id,expired));
        Assert.Contains("Đã ghi",await manager.Confirm(admin.Id,preview));
        Assert.Equal("Tạm đình chỉ",provider.ProviderStatus);Assert.False(service.IsAccepting);Assert.True(service.IsPublished);
        Assert.Equal("Không tiếp tục",pending.Status);Assert.Equal("Đã xác nhận",confirmed.Status);
        Assert.Contains("trước đó",await manager.Confirm(admin.Id,preview));
        Assert.True(ProviderRules.CanFulfillExisting(provider));Assert.False(ProviderRules.CanPublish(provider));
        Assert.Contains("Đã gửi",await manager.Explain(provider.Id,"Đề nghị đối chiếu và khôi phục",null));
        Assert.Contains("đang chờ",await manager.Explain(provider.Id,"Gửi lặp",null));
        var explanation=await db.ProviderExplanations.SingleAsync(x=>x.OwnerId==provider.Id);
        var restore=Input("Restore");restore.ExplanationId=explanation.Id;
        preview=(await manager.Preview(admin.Id,restore)).Preview!;Assert.Contains("Đã ghi",await manager.Confirm(admin.Id,preview));
        Assert.Equal("Đang hoạt động",provider.ProviderStatus);Assert.True(service.IsAccepting);
        Assert.Equal("Đã xem xét",explanation.Status);Assert.Equal("Không tiếp tục",pending.Status);
        preview=(await manager.Preview(admin.Id,Input("Revoke"))).Preview!;
        Assert.Contains("Đã ghi",await manager.Confirm(admin.Id,preview));
        Assert.Equal("Đã thu hồi",provider.VerificationStatus);Assert.False(service.IsPublished);
        Assert.Equal("Đã xác nhận",confirmed.Status);Assert.True(ProviderRules.CanFulfillExisting(provider));
        Assert.Contains((await scope.ServiceProvider.GetRequiredService<AccountManager>().BuildDeactivation(provider)).Obligations,x=>x.Url=="/Provider/Requests"&&x.Text.Contains(confirmed.Id.ToString()));
        output.WriteLine("PASS SUC13: permission, invalid version/reason, preview, expiry, suspend, preserve agreements, explanation, restore, revoke, obligation checks.");
    }
}
