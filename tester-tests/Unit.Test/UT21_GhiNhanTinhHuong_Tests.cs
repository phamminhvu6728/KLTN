using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Dung chung UT-21 va UT-22. Du lieu rieng cho tung test.
internal static class UT2122
{
    internal const int KH01=101,KH02=102,KHP01=401,KHP02=402,HD1=501,HD2=502,HD3=503,TH1=601;
    internal static readonly byte[] RowVersion={1,2,3,4,5,6,7,8};
    internal static string Version=>Convert.ToBase64String(RowVersion);
    internal static readonly DateTime Day=new(2026,12,5);
    internal sealed class SaveObserver : SaveChangesInterceptor
    {
        internal readonly System.Collections.Generic.List<string> Snapshots=new();
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result,CancellationToken cancellationToken=default)
        {
            var context=eventData.Context;
            if(context!=null)
            {
                var entries=context.ChangeTracker.Entries<IncidentPlan>().ToList();
                var pendingAdded=entries.Count(e=>e.State==EntityState.Added);
                var oldModified=entries.Count(e=>e.State==EntityState.Modified && !e.Entity.IsCurrent);
                Snapshots.Add($"Added={pendingAdded};OldSetFalse={oldModified}");
            }
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }
    internal static AppDbContext Db(SaveObserver? observer=null)
    {
        var builder=new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("UT2122_"+Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        if(observer!=null)builder.AddInterceptors(observer);
        var options=builder.Options;
        return new AppDbContext(options,new HttpContextAccessor());
    }
    internal static async Task Seed(AppDbContext db,bool includeActivities=true)
    {
        db.Accounts.AddRange(new Account{Id=KH01,Name="KH01",Email="ut2122-01@example.test",Role="Traveler",IsActive=true},
                             new Account{Id=KH02,Name="KH02",Email="ut2122-02@example.test",Role="Traveler",IsActive=true});
        db.Trips.AddRange(new Trip{Id=KHP01,OwnerId=KH01,Name="KHP01",Destination="Đà Lạt",Start=Day,End=Day.AddDays(3),Version=RowVersion.ToArray()},
                          new Trip{Id=KHP02,OwnerId=KH02,Name="KHP02",Destination="Huế",Start=Day,End=Day.AddDays(3),Version=RowVersion.ToArray()});
        if(includeActivities)db.Activities.AddRange(new TripActivity{Id=HD1,TripId=KHP01,Title="HD1",Start=Day,End=Day.AddHours(1)},
                                                     new TripActivity{Id=HD2,TripId=KHP01,Title="HD2",Start=Day.AddDays(1),End=Day.AddDays(1).AddHours(1)},
                                                     new TripActivity{Id=HD3,TripId=KHP02,Title="HD3",Start=Day,End=Day.AddHours(1)});
        await db.SaveChangesAsync();
    }
    internal static IncidentInput NewIncident(int trip=KHP01,string category="Thời tiết",params int[] activities)
        =>new(){TripId=trip,Category=category,Description="Mưa lớn ảnh hưởng kế hoạch",ActivityIds=activities.ToList()};
    internal static async Task SeedIncident(AppDbContext db,string status="Chưa giải quyết",bool withCurrentPlan=false)
    {
        db.Incidents.Add(new Incident{Id=TH1,TripId=KHP01,OwnerId=KH01,Category="Thời tiết",Description="Mưa lớn",Status=status,Version=RowVersion.ToArray()});
        if(withCurrentPlan)db.IncidentPlans.Add(new IncidentPlan{Id=701,IncidentId=TH1,Content="PA1",IsCurrent=true});
        await db.SaveChangesAsync();
    }
    internal static IncidentPlanInput PlanInput(string? version=null)=>new(){Id=TH1,Version=version??Version,Content="PA2 thay thế"};
    internal static async Task Check(string id,string step,string title,string input,string expected,Func<Action<string>,Task> body)
    {
        string actual="Chưa có dữ liệu thực tế",problem="";
        try{await body(value=>actual=value);}
        catch(Exception ex){problem=ex.Message;throw;}
        finally
        {
            var directory=Path.Combine(AppContext.BaseDirectory,"TestResults");Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"UnitTest_Details.jsonl");
            var line=JsonSerializer.Serialize(new{id,step,title,input,expected,actual,problem});
            for(var attempt=0;attempt<100;attempt++)
            {
                try
                {
                    using var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(stream);await writer.WriteLineAsync(line);break;
                }
                catch(IOException) when(attempt<99){await Task.Delay(20);}
            }
        }
    }
}
public sealed class UT21_GhiNhanTinhHuong_Tests
{
    [Fact(DisplayName="UT-21a | KT1 | Loai tinh huong trong")]
    public Task UT21a_EmptyCategory()=>UT2122.Check("UT-21a","KT1","Loại tình huống trống","Category rỗng","Lỗi 0501-02; không tạo tình huống",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);
        var result=await new JourneyManager(db).AddIncident(UT2122.KH01,UT2122.NewIncident(category:""));
        var count=await db.Incidents.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-02",result.Code);
    });
    [Fact(DisplayName="UT-21b | KT2 | Ke hoach khong thuoc nguoi goi")]
    public Task UT21b_NotOwner()=>UT2122.Check("UT-21b","KT2","KH01 ghi tình huống vào kế hoạch KH02","TripId=KHP02; user=KH01","Lỗi 0501-01; không tạo tình huống",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);
        var result=await new JourneyManager(db).AddIncident(UT2122.KH01,UT2122.NewIncident(UT2122.KHP02));
        var count=await db.Incidents.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-01",result.Code);
    });
    [Fact(DisplayName="UT-21c | KT3 | Hoat dong thuoc ke hoach khac")]
    public Task UT21c_WrongActivity()=>UT2122.Check("UT-21c","KT3","Hoạt động được chọn không thuộc kế hoạch","TripId=KHP01; ActivityIds=[HD3] thuộc KHP02","Lỗi 0501-03; không tạo tình huống",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);
        var result=await new JourneyManager(db).AddIncident(UT2122.KH01,UT2122.NewIncident(UT2122.KHP01,"Thời tiết",UT2122.HD3));
        var count=await db.Incidents.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-03",result.Code);
    });
    [Fact(DisplayName="UT-21d | XL1-XL4 | Loai trung hoat dong lien ket")]
    public Task UT21d_DeduplicateActivities()=>UT2122.Check("UT-21d","XL1-XL4","Tạo tình huống, chỉ liên kết 2 hoạt động khác nhau","[HD1,HD1,HD2]","Một tình huống ChuaGiaiQuyet; đúng 2 liên kết HD1,HD2",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);
        var result=await new JourneyManager(db).AddIncident(UT2122.KH01,UT2122.NewIncident(UT2122.KHP01,"Thời tiết",UT2122.HD1,UT2122.HD1,UT2122.HD2));
        var incidents=await db.Incidents.AsNoTracking().ToListAsync();var links=await db.IncidentActivities.AsNoTracking().OrderBy(x=>x.ActivityId).ToListAsync();
        observe($"Success={result.Success}; NewId={result.Id}; IncidentCount={incidents.Count}; Status={incidents.FirstOrDefault()?.Status}; Links={string.Join(",",links.Select(x=>x.ActivityId))}");
        Assert.True(result.Success);var item=Assert.Single(incidents);Assert.Equal(item.Id,result.Id);Assert.Equal("Chưa giải quyết",item.Status);
        Assert.Equal(new[]{UT2122.HD1,UT2122.HD2},links.Select(x=>x.ActivityId).ToArray());
    });
    [Fact(DisplayName="UT-21e | XL1 | Khong chon hoat dong")]
    public Task UT21e_NoActivities()=>UT2122.Check("UT-21e","XL1","Tình huống không có hoạt động liên kết","ActivityIds rỗng","Tạo tình huống hợp lệ; không có dòng IncidentActivity",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);
        var result=await new JourneyManager(db).AddIncident(UT2122.KH01,UT2122.NewIncident());
        var count=await db.Incidents.CountAsync();var links=await db.IncidentActivities.CountAsync();
        observe($"Success={result.Success}; IncidentCount={count}; Links={links}; NewId={result.Id}");
        Assert.True(result.Success);Assert.Equal(1,count);Assert.Equal(0,links);Assert.True(result.Id>0);
    });
}
