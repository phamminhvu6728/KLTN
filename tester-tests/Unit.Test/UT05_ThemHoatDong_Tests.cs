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

// Shared test setup for UT-05, UT-06, UT-07. This class lives in UT05_ThemHoatDong_Tests.cs.
internal static class UT567
{
    internal const int KH01 = 101, KH02 = 102, KHP01 = 401;
    internal static readonly DateTime Day = new(2026, 12, 5);
    internal static readonly DateTime FixedNow = new(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);
    internal sealed class Clock : IDongHo { public DateTime UtcNow => FixedNow; public DateTime Today => new(2026, 11, 3); }
    internal sealed class ConcurrencyInjector : SaveChangesInterceptor
    {
        internal bool Enabled;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken=default)
        {
            if(Enabled) throw new DbUpdateConcurrencyException("UT-05e: forced database concurrency conflict");
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }
    internal static AppDbContext Db(ConcurrencyInjector? injector=null)
    {
        var opts=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT567_"+Guid.NewGuid().ToString("N"));
        if(injector is not null) opts.AddInterceptors(injector);
        return new AppDbContext(opts.Options,new HttpContextAccessor());
    }
    internal static async Task Seed(AppDbContext db, int owner=KH01, string status="Nháp", DateTime? start=null, DateTime? end=null)
    {
        db.Accounts.AddRange(
            new Account { Id=KH01, Name="KH01", Email="ut567-kh01@example.test", Role="Traveler", IsActive=true },
            new Account { Id=KH02, Name="KH02", Email="ut567-kh02@example.test", Role="Traveler", IsActive=true });
        db.Trips.Add(new Trip { Id=KHP01, OwnerId=owner, Name="Kế hoạch Đà Lạt", Destination="Đà Lạt",
            Start=start??Day, End=end??Day.AddDays(3), Budget=3000000, Status=status,
            UpdatedAt=FixedNow.AddDays(-1), Version=new byte[8] });
        await db.SaveChangesAsync();
    }
    internal static string Version(Trip t) => Convert.ToBase64String(t.Version);
    internal static TripActivity Item(int id, int dayOffset, int order=1, decimal? cost=null) =>
        new() { Id=id, TripId=KHP01, Title="Hoạt động " + id, Start=Day.AddDays(dayOffset).AddHours(8),
                End=Day.AddDays(dayOffset).AddHours(9), SortOrder=order, Cost=cost, HasTime=true };
    internal static ActivityInput Input(int id=0, int dayOffset=0, string source="Manual", int? sourceId=null,
        string? title="Hoạt động mới", int order=1, decimal? cost=null, string? version=null) =>
        new() { Id=id, TripId=KHP01, Start=Day.AddDays(dayOffset).AddHours(8),
                End=Day.AddDays(dayOffset).AddHours(9), HasTime=true, SourceType=source, SourceId=sourceId,
                Title=title, SortOrder=order, Cost=cost, Version=version??Convert.ToBase64String(new byte[8]) };
    internal static TripInput TripChange(DateTime start,DateTime end,string? version=null) =>
        new() { Id=KHP01, Name="Kế hoạch Đà Lạt cập nhật", Destination="Đà Lạt", Start=start,
                End=end, Budget=3000000, Version=version??Convert.ToBase64String(new byte[8]) };
    internal static async Task Check(string id,string step,string title,string input,string expected,Func<Action<string>,Task> action)
    {
        string actual="Chưa ghi nhận đầu ra"; string problem="";
        try { await action(v=>actual=v); }
        catch(Exception ex) { problem=ex.Message; throw; }
        finally
        {
            var folder=Path.Combine(AppContext.BaseDirectory,"TestResults");
            Directory.CreateDirectory(folder);
            var path=Path.Combine(folder,"UnitTest_Details.jsonl");
            var line=JsonSerializer.Serialize(new {id,step,title,input,expected,actual,problem});
            bool written=false;
            for(int retry=0;retry<100;retry++)
            {
                try
                {
                    using var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(stream);
                    await writer.WriteLineAsync(line);
                    written=true;
                    break;
                }
                catch(IOException) when(retry<99) { await Task.Delay(20); }
            }
            if(!written) throw new IOException("Không ghi được biên bản JSONL");
        }
    }
}

/// <summary>UT-05: Bảng 4.181, GD-01. Tests call the current implementation JourneyManager.Activity.</summary>
public sealed class UT05_ThemHoatDong_Tests
{
    [Fact(DisplayName="UT-05a | KT1 | chủ kế hoạch không hợp lệ")]
    public Task UT05a_Owner() => UT567.Check("UT-05a","KT1","Từ chối thêm hoạt động khác chủ", "KH01 thêm vào kế hoạch KHP01 thuộc KH02", "0201-02; không có hoạt động mới",async record=>{
        await using var db=UT567.Db(); await UT567.Seed(db,UT567.KH02);
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input());
        var count=await db.Activities.CountAsync(); record($"success={result.Success}; code={result.Code}; soHoatDong={count}; message={result.Message}");
        Assert.False(result.Success); Assert.Equal("0201-02",result.Code); Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-05b | KT2 | ngày 09/12 ngoài kế hoạch")]
    public Task UT05b_OutOfRange() => UT567.Check("UT-05b","KT2","Ngày ngoài phạm vi","Hoạt động ngày 09/12; KHP01 05-08/12", "0201-03; không tạo hoạt động",async record=>{
        await using var db=UT567.Db(); await UT567.Seed(db);
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(dayOffset:4));
        int count=await db.Activities.CountAsync(); record($"success={result.Success}; code={result.Code}; soHoatDong={count}");
        Assert.False(result.Success); Assert.Equal("0201-03",result.Code); Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-05c | KT3 | địa điểm DangHienThi=0")]
    public Task UT05c_HiddenPlace() => UT567.Check("UT-05c","KT3","Địa điểm bị ẩn", "Địa điểm DD01 tồn tại, DangHienThi=0", "0201-04; không tạo hoạt động",async record=>{
        await using var db=UT567.Db(); await UT567.Seed(db);
        db.Places.Add(new Place{Id=501,Name="Hồ Xuân Hương",Region="Đà Lạt",Category="Tham quan"});
        await db.SaveChangesAsync();
        // Không tự giả lập một trường trạng thái mà mã nguồn không có.
        var visibility=db.Model.FindEntityType(typeof(Place))?.FindProperty("DangHienThi");
        if(visibility is null) {record("Place không có thuộc tính DangHienThi trong model -> không thể cấu hình DD01 là ẩn"); Assert.Fail("Chưa có trường DangHienThi để thực thi đúng dữ liệu đầu vào của UT-05c.");}
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(source:"Place",sourceId:501,title:""));
        int count=await db.Activities.CountAsync();record($"success={result.Success}; code={result.Code}; count={count}");
        Assert.False(result.Success);Assert.Equal("0201-04",result.Code);Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-05d | KT3 | dịch vụ ChoDuyet")]
    public Task UT05d_UnapprovedService() => UT567.Check("UT-05d","KT3","Dịch vụ chưa được duyệt", "Dịch vụ DV01 có ApprovalStatus=Chờ duyệt", "0201-04; không tạo hoạt động",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        db.Services.Add(new TravelService{Id=601,OwnerId=UT567.KH02,Name="Dịch vụ đang duyệt",ApprovalStatus="Chờ duyệt"});await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(source:"Service",sourceId:601,title:""));
        int count=await db.Activities.CountAsync();record($"success={result.Success}; code={result.Code}; soHoatDong={count}");
        Assert.False(result.Success);Assert.Equal("0201-04",result.Code);Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-05e | KT4 | DbUpdateConcurrencyException không lưu hoạt động")]
    public Task UT05e_Concurrency() => UT567.Check("UT-05e","KT4","Lỗi đồng thời khi lưu","SaveChanges phát sinh DbUpdateConcurrencyException", "0201-05; không phát sinh hoạt động được lưu",async record=>{
        var injector=new UT567.ConcurrencyInjector();
        await using var db=UT567.Db(injector);await UT567.Seed(db);
        injector.Enabled=true;
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input());
        injector.Enabled=false;
        var count=await db.Activities.AsNoTracking().CountAsync();
        record($"success={result.Success}; code={result.Code}; countPersisted={count}; message={result.Message}; injected=DbUpdateConcurrencyException");
        Assert.False(result.Success);Assert.Equal("0201-05",result.Code);Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-05f | XL1 XL2 XL3 | DD01 và ThuTu=3")]
    public Task UT05f_AddPlace() => UT567.Check("UT-05f","XL1, XL2, XL3","Thêm địa điểm theo thứ tự", "05/12 có hai hoạt động; DD01; Title rỗng", "ThuTu=3; NoiDung=Hồ Xuân Hương; MaDiaDiem=DD01; MaDichVu=NULL",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        db.Activities.AddRange(UT567.Item(511,0,1),UT567.Item(512,0,2));
        db.Places.Add(new Place{Id=501,Name="Hồ Xuân Hương",Region="Đà Lạt",Category="Tham quan"});await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(source:"Place",sourceId:501,title:""));
        var created=await db.Activities.AsNoTracking().Where(x=>x.Id!=511&&x.Id!=512).SingleOrDefaultAsync();
        record($"success={result.Success}; code={result.Code}; thuTu={created?.SortOrder}; noiDung={created?.Title}; maDiaDiem={created?.PlaceId}; maDichVu={created?.TravelServiceId}");
        Assert.True(result.Success);Assert.NotNull(created);Assert.Equal(3,created!.SortOrder);Assert.Equal("Hồ Xuân Hương",created.Title);Assert.Equal(501,created.PlaceId);Assert.Null(created.TravelServiceId);
    });

    [Fact(DisplayName="UT-05g | XL1 | ngày mới ThuTu=1")]
    public Task UT05g_NewDayOrder() => UT567.Check("UT-05g","XL1","Thứ tự ngày chưa có hoạt động", "Ngày 06/12 chưa có hoạt động", "ThuTu=1",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(dayOffset:1));
        var created=await db.Activities.AsNoTracking().SingleOrDefaultAsync();record($"success={result.Success}; thuTu={created?.SortOrder}");
        Assert.True(result.Success);Assert.NotNull(created);Assert.Equal(1,created!.SortOrder);
    });

    [Fact(DisplayName="UT-05h | XL4 | Đã chấp nhận chuyển về Nháp")]
    public Task UT05h_ApprovedToDraft() => UT567.Check("UT-05h","XL4","Thêm hoạt động vào kế hoạch đã chấp nhận", "Trạng thái trước=Đã chấp nhận", "Trạng thái sau=Nháp; thời điểm cập nhật=BayGioUtc; thông điệp Kế hoạch đã chuyển về Nháp",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,status:"Đã chấp nhận");
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input());
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; status={trip.Status}; updatedAt={trip.UpdatedAt:o}; message={result.Message}");
        Assert.True(result.Success);Assert.Equal("Nháp",trip.Status);Assert.Equal(UT567.FixedNow,trip.UpdatedAt);
        Assert.Contains("Kế hoạch đã chuyển về Nháp",result.Message,StringComparison.OrdinalIgnoreCase);
    });

    [Fact(DisplayName="UT-05i | XL4 | Nháp không thông báo chuyển về Nháp")]
    public Task UT05i_DraftStaysDraft() => UT567.Check("UT-05i","XL4","Thêm hoạt động vào kế hoạch Nháp", "Trạng thái trước=Nháp", "Giữ Nháp; không có thông điệp chuyển về Nháp",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,status:"Nháp");
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input());
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; status={trip.Status}; message={result.Message}");
        Assert.True(result.Success);Assert.Equal("Nháp",trip.Status);
        Assert.DoesNotContain("chuyển về Nháp",result.Message,StringComparison.OrdinalIgnoreCase);
    });
}
