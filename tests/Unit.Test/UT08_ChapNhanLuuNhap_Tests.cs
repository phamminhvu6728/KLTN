using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-08, Bảng 4.185-4.186. Phụ thuộc UT567 trong UT05_ThemHoatDong_Tests.cs.</summary>
public sealed class UT08_ChapNhanLuuNhap_Tests
{
    [Fact(DisplayName="UT-08a | KT1 | Khach khac chap nhan ke hoach => 0201-02")]
    public Task UT08a_NotOwner() => UT567.Check("UT-08a","KT1","Không phải chủ kế hoạch",
        "KH02 chấp nhận KHP01 của KH01","Lỗi 0201-02; trạng thái giữ Nháp",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        var result=await new JourneyManager(db,new UT567.Clock()).Confirm(UT567.KH02,UT567.KHP01,Convert.ToBase64String(new byte[8]));
        var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"success={result.Success}; code={result.Code}; status={trip.Status}; message={result.Message}");
        Assert.False(result.Success);Assert.Equal("0201-02",result.Code);Assert.Equal("Nháp",trip.Status);
    });

    [Fact(DisplayName="UT-08b | KT2 | RowVersion cu => 0201-05")]
    public Task UT08b_StaleVersion() => UT567.Check("UT-08b","KT2","Phiên bản cũ",
        "KHP01, rowVersion gửi khác rowVersion thực tế","Lỗi 0201-05; trạng thái không đổi",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        var result=await new JourneyManager(db,new UT567.Clock()).Confirm(UT567.KH01,UT567.KHP01,Convert.ToBase64String(new byte[]{1,1,1,1,1,1,1,1}));
        var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"success={result.Success}; code={result.Code}; status={trip.Status}; message={result.Message}");
        Assert.False(result.Success);Assert.Equal("0201-05",result.Code);Assert.Equal("Nháp",trip.Status);
    });

    [Fact(DisplayName="UT-08c | KT3 | Hoat dong ngoai ngay => 0201-03")]
    public Task UT08c_BadLoadedActivity() => UT567.Check("UT-08c","KT3","Hoạt động nạp sẵn ngoài khoảng",
        "KHP01 05-08/12; hoạt động ngày 10/12","Lỗi 0201-03; không chấp nhận kế hoạch",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        db.Activities.Add(UT567.Item(801,5));await db.SaveChangesAsync();
        var result=await new JourneyManager(db,new UT567.Clock()).Confirm(UT567.KH01,UT567.KHP01,Convert.ToBase64String(new byte[8]));
        var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"success={result.Success}; code={result.Code}; status={trip.Status}; message={result.Message}");
        Assert.False(result.Success);Assert.Equal("0201-03",result.Code);Assert.Equal("Nháp",trip.Status);
    });

    [Fact(DisplayName="UT-08d | XL1 | Canh bao khong ngan ChapNhan")]
    public Task UT08d_WarningsNotBlocking() => UT567.Check("UT-08d","XL1","Cảnh báo không chặn chấp nhận",
        "Hai hoạt động trùng giờ; tổng chi phí 3.100.000 > 3.000.000", "Kế hoạch thành Đã chấp nhận; cảnh báo không chặn lưu",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db);
        db.Activities.AddRange(UT567.Item(811,0,1,1500000),UT567.Item(812,0,2,1600000));
        await db.SaveChangesAsync();
        var mgr=new JourneyManager(db,new UT567.Clock());
        var before=await mgr.Detail(UT567.KH01,UT567.KHP01);
        var result=await mgr.Confirm(UT567.KH01,UT567.KHP01,Convert.ToBase64String(new byte[8]));
        var trip=await db.Trips.AsNoTracking().SingleAsync();
        record($"warningCount={before?.Warnings.Count}; warnings=[{string.Join(";",before?.Warnings??new())}]; success={result.Success}; code={result.Code}; status={trip.Status}");
        Assert.NotNull(before);Assert.Contains(before!.Warnings,w=>w.Contains("Trùng giờ"));
        Assert.Contains(before.Warnings,w=>w.Contains("vượt ngân sách"));
        Assert.True(result.Success);Assert.Equal("Đã chấp nhận",trip.Status);
    });

    [Fact(DisplayName="UT-08e | LuuNhap XL1 | Chuyen huong /ke-hoach va thong diep")]
    public Task UT08e_SaveDraft() => UT567.Check("UT-08e","LuuNhap XL1","Lưu Nháp từ kế hoạch đã chấp nhận",
        "KHP01 trạng thái Đã chấp nhận; KH01 LuuNhap", "Trạng thái Nháp; thông điệp Đã lưu nháp; chuyển hướng /ke-hoach",async record=>{
        await using var db=UT567.Db();await UT567.Seed(db,status:"Đã chấp nhận");
        var result=await new JourneyManager(db,new UT567.Clock()).Draft(UT567.KH01,UT567.KHP01,Convert.ToBase64String(new byte[8]));
        var trip=await db.Trips.AsNoTracking().SingleAsync();
        // JourneyManager.Draft chỉ trả kết quả nghiệp vụ, không có trường redirect URL.
        string? redirect=null;
        record($"success={result.Success}; code={result.Code}; status={trip.Status}; message={result.Message}; redirect={(redirect??"KHONG CO TRONG KET QUA")}");
        Assert.True(result.Success);Assert.Equal("Nháp",trip.Status);
        Assert.Contains("Đã lưu nháp",result.Message,StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/ke-hoach",redirect);
    });
}
