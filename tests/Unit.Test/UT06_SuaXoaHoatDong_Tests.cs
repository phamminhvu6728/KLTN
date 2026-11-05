using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-06: Bảng 4.182-4.183, sửa/xóa hoạt động. Reuses UT567 in UT05 file.</summary>
public sealed class UT06_SuaXoaHoatDong_Tests
{
    [Fact(DisplayName="UT-06a | Sửa KT1 | hoạt động thuộc KH02")]
    public Task UT06a_OtherOwner() => UT567.Check("UT-06a","Sửa KT1","Từ chối sửa khác chủ", "KHP01 thuộc KH02; KH01 sửa hoạt động", "Lỗi 0201-02; hoạt động không đổi",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,UT567.KH02);db.Activities.Add(UT567.Item(601,0));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(id:601,title:"Đã sửa"));
        var activity=await db.Activities.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; title={activity.Title}");
        Assert.False(result.Success);Assert.Equal("0201-02",result.Code);Assert.Equal("Hoạt động 601",activity.Title);
    });

    [Fact(DisplayName="UT-06b | Sửa KT2 | đổi ngày sang 10/12")]
    public Task UT06b_InvalidDate() => UT567.Check("UT-06b","Sửa KT2","Ngày sửa vượt kế hoạch", "Sửa hoạt động HĐ1 sang 10/12; kế hoạch 05-08/12", "Lỗi 0201-03; ngày cũ giữ nguyên",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);db.Activities.Add(UT567.Item(601,0));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(id:601,dayOffset:5));
        var activity=await db.Activities.AsNoTracking().SingleAsync();record($"success={result.Success}; code={result.Code}; start={activity.Start:yyyy-MM-dd}");
        Assert.False(result.Success);Assert.Equal("0201-03",result.Code);Assert.Equal(UT567.Day,activity.Start.Date);
    });

    [Fact(DisplayName="UT-06c | Sửa XL1 XL2 | cập nhật thứ tự chi phí và chuyển Nháp")]
    public Task UT06c_UpdateActivity() => UT567.Check("UT-06c","Sửa XL1, XL2","Sửa thứ tự chi phí", "KHP01 Đã chấp nhận; sửa hoạt động HĐ1 ThuTu=1 Cost=200000", "Hai trường được cập nhật; TripId không đổi; kế hoạch về Nháp",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,status:"Đã chấp nhận");db.Activities.Add(UT567.Item(601,0,2,500000));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Activity(UT567.KH01,UT567.Input(id:601,order:1,cost:200000,title:"Hoạt động 601"));
        var activity=await db.Activities.AsNoTracking().SingleAsync();var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"success={result.Success}; thuTu={activity.SortOrder}; chiPhi={activity.Cost}; tripId={activity.TripId}; status={trip.Status}");
        Assert.True(result.Success);Assert.Equal(1,activity.SortOrder);Assert.Equal(200000m,activity.Cost);Assert.Equal(UT567.KHP01,activity.TripId);Assert.Equal("Nháp",trip.Status);
    });

    [Fact(DisplayName="UT-06d | Xóa KT2 | hoạt động đã gắn tình huống")]
    public Task UT06d_ReferencedActivity() => UT567.Check("UT-06d","Xóa KT2","Không xóa hoạt động đang gắn tình huống", "HĐ1 có dòng TinhHuongHoatDong", "Lỗi 0201-06; HĐ1 vẫn tồn tại",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);db.Activities.Add(UT567.Item(601,0));
        db.Incidents.Add(new Incident{Id=701,OwnerId=UT567.KH01,TripId=UT567.KHP01,Category="Thời tiết",Description="Mưa",Status="Chưa giải quyết"});
        db.IncidentActivities.Add(new IncidentActivity{IncidentId=701,ActivityId=601});await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).RemoveActivity(UT567.KH01,UT567.KHP01,601,Convert.ToBase64String(new byte[8]));
        bool exists=await db.Activities.AsNoTracking().AnyAsync(x=>x.Id==601);record($"success={result.Success}; code={result.Code}; activityStillExists={exists}");
        Assert.False(result.Success);Assert.Equal("0201-06",result.Code);Assert.True(exists);
    });

    [Fact(DisplayName="UT-06e | Xóa KT3 | rowVersion cũ")]
    public Task UT06e_StaleVersion() => UT567.Check("UT-06e","Xóa KT3","Phiên bản cũ", "HĐ2 không gắn tình huống; gửi rowVersion không khớp", "Lỗi 0201-05; HĐ2 chưa bị xóa",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);db.Activities.Add(UT567.Item(602,0));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).RemoveActivity(UT567.KH01,UT567.KHP01,602,Convert.ToBase64String(new byte[]{1,1,1,1,1,1,1,1}));
        bool exists=await db.Activities.AsNoTracking().AnyAsync(x=>x.Id==602);record($"success={result.Success}; code={result.Code}; exists={exists}");
        Assert.False(result.Success);Assert.Equal("0201-05",result.Code);Assert.True(exists);
    });

    [Fact(DisplayName="UT-06f | Xóa XL1 XL2 | xóa và cập nhật thời gian kế hoạch")]
    public Task UT06f_Remove() => UT567.Check("UT-06f","Xóa XL1, XL2","Xóa hoạt động hợp lệ", "HĐ2 không có tình huống; phiên bản đúng", "HĐ2 bị xóa; thời điểm cập nhật kế hoạch=BayGioUtc",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);db.Activities.Add(UT567.Item(602,0));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).RemoveActivity(UT567.KH01,UT567.KHP01,602,Convert.ToBase64String(new byte[8]));
        bool exists=await db.Activities.AsNoTracking().AnyAsync(x=>x.Id==602);var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"success={result.Success}; exists={exists}; updatedAt={trip.UpdatedAt:o}; status={trip.Status}");
        Assert.True(result.Success);Assert.False(exists);Assert.Equal(UT567.FixedNow,trip.UpdatedAt);
    });
}
