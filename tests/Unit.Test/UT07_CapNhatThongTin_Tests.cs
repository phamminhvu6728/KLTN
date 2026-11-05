using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-07: Bảng 4.184. Update branch uses JourneyManager.Save with TripInput.Id>0.</summary>
public sealed class UT07_CapNhatThongTin_Tests
{
    [Fact(DisplayName="UT-07a | KT2 | thu hẹp khoảng nhưng còn hoạt động ngoài")]
    public Task UT07a_ShrinkRange() => UT567.Check("UT-07a","KT2","Khoảng mới không chứa hoạt động", "Ngày mới 05-07/12; hoạt động đang ở 08/12", "0201-07; khoảng cũ giữ nguyên",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);db.Activities.Add(UT567.Item(701,3));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Save(UT567.KH01,UT567.TripChange(UT567.Day,UT567.Day.AddDays(2)));
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; end={trip.End:yyyy-MM-dd}");
        Assert.False(result.Success);Assert.Equal("0201-07",result.Code);Assert.Equal(UT567.Day.AddDays(3),trip.End.Date);
    });

    [Fact(DisplayName="UT-07b | Đầu vào | giữ nguyên ngày bắt đầu cũ trong quá khứ")]
    public Task UT07b_KeepPastStart() => UT567.Check("UT-07b","Đầu vào","Giữ ngày bắt đầu cũ", "Kế hoạch bắt đầu 01/11/2026; chỉ đổi tên", "Hợp lệ, không buộc ngày bắt đầu cũ >= 03/11",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,start:new DateTime(2026,11,1),end:new DateTime(2026,11,8));
        var input=UT567.TripChange(new DateTime(2026,11,1),new DateTime(2026,11,8));
        var result=await new JourneyManager(db,new UT567.Clock()).Save(UT567.KH01,input);
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; name={trip.Name}; start={trip.Start:yyyy-MM-dd}");
        Assert.True(result.Success);Assert.Equal("Kế hoạch Đà Lạt cập nhật",trip.Name);Assert.Equal(new DateTime(2026,11,1),trip.Start.Date);
    });

    [Fact(DisplayName="UT-07c | Đầu vào | đổi ngày bắt đầu sang quá khứ")]
    public Task UT07c_ChangeStartToPast() => UT567.Check("UT-07c","Đầu vào","Từ chối ngày bắt đầu mới quá khứ", "Ngày cũ 01/11; đổi thành 02/11; hôm nay 03/11", "Lỗi tại trường ngayBD; không cập nhật ngày",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,start:new DateTime(2026,11,1),end:new DateTime(2026,11,8));
        var result=await new JourneyManager(db,new UT567.Clock()).Save(UT567.KH01,UT567.TripChange(new DateTime(2026,11,2),new DateTime(2026,11,8)));
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; message={result.Message}; start={trip.Start:yyyy-MM-dd}");
        Assert.False(result.Success);Assert.Equal(new DateTime(2026,11,1),trip.Start.Date);
        Assert.Contains("ngayBD",result.Message,StringComparison.OrdinalIgnoreCase);
    });

    [Fact(DisplayName="UT-07d | KT3 | rowVersion cũ")]
    public Task UT07d_StaleVersion() => UT567.Check("UT-07d","KT3","Phiên bản kế hoạch không hợp lệ", "rowVersion cũ khác rowVersion hiện tại", "0201-05; kế hoạch không đổi",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        var input=UT567.TripChange(UT567.Day,UT567.Day.AddDays(3),Convert.ToBase64String(new byte[]{2,2,2,2,2,2,2,2}));
        var result=await new JourneyManager(db,new UT567.Clock()).Save(UT567.KH01,input);
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; name={trip.Name}");
        Assert.False(result.Success);Assert.Equal("0201-05",result.Code);Assert.Equal("Kế hoạch Đà Lạt",trip.Name);
    });

    [Fact(DisplayName="UT-07e | XL1 | cập nhật thông tin và chuyển Nháp")]
    public Task UT07e_UpdateAndDraft() => UT567.Check("UT-07e","XL1","Cập nhật và chuyển về Nháp", "KHP01 Đã chấp nhận; sửa tên, điểm đến, ngân sách", "Các trường được cập nhật; trạng thái thành Nháp",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,status:"Đã chấp nhận");
        var input=UT567.TripChange(UT567.Day,UT567.Day.AddDays(3));input.Name="Kế hoạch Sa Pa";input.Destination="Sa Pa";input.Budget=2500000;
        var result=await new JourneyManager(db,new UT567.Clock()).Save(UT567.KH01,input);
        var trip=await db.Trips.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; name={trip.Name}; destination={trip.Destination}; budget={trip.Budget}; status={trip.Status}; updatedAt={trip.UpdatedAt:o}");
        Assert.True(result.Success);Assert.Equal("Kế hoạch Sa Pa",trip.Name);Assert.Equal("Sa Pa",trip.Destination);Assert.Equal(2500000m,trip.Budget);Assert.Equal("Nháp",trip.Status);
    });
}
