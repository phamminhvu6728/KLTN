using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-12: CongBoAsync. Bang 4.192, GD-02. Su dung UT111213 tu file UT-11.</summary>
public sealed class UT12_CongBoNhuCau_Tests
{
    [Fact(DisplayName="UT-12a | KT1 | Ngay bat dau qua khu => 0302-01")]
    public Task UT12a_PastStart()=>UT111213.Check("UT-12a","KT1","Ngay bat dau qua khu",
        "ngayBD=02/11/2026 (theo ke hoach, hom nay 03/11/2026)","Loi 0302-01; khong tao nhu cau",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var manager=new CommunityManager(db);var input=UT111213.Input(begin:new DateTime(2026,11,2));
        var r=await manager.Save(UT111213.KH01,input);
        var count=await db.Companions.CountAsync();
        observe($"Success={r.Success}; Message={r.Message}; StatusCode={r.StatusCode}; Id={r.Id}; SoNhuCau={count}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal(0,count);
        Assert.Contains("0302-01",r.Message);
    });

    [Fact(DisplayName="UT-12b | KT1 | Pham vi ngoai enum => 0302-01")]
    public Task UT12b_InvalidScope()=>UT111213.Check("UT-12b","KT1","Pham vi chia se ngoai danh muc",
        "SharingScope=MoiNguoi","Loi 0302-01; khong tao nhu cau",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var input=UT111213.Input("MoiNguoi");
        var r=await new CommunityManager(db).Save(UT111213.KH01,input);
        var count=await db.Companions.CountAsync();
        observe($"Success={r.Success}; Message={r.Message}; StatusCode={r.StatusCode}; Id={r.Id}; SoNhuCau={count}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal(0,count);
        Assert.Contains("0302-01",r.Message);
    });

    [Fact(DisplayName="UT-12c | KT2 | Tai khoan ngung hoat dong khong duoc cong bo")]
    public Task UT12c_InactiveAccount()=>UT111213.Check("UT-12c","KT2","Tai khoan KH01 ngung hoat dong",
        "KH01.IsActive=false; input hop le","Tu choi; khong tao nhu cau",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db,kh01Active:false);
        var r=await new CommunityManager(db).Save(UT111213.KH01,UT111213.Input());
        var count=await db.Companions.CountAsync();
        observe($"Success={r.Success}; Message={r.Message}; StatusCode={r.StatusCode}; SoNhuCau={count}");
        Assert.False(r.Success);Assert.Equal(0,count);
    });

    [Fact(DisplayName="UT-12d | XL1 | Cong bo nhu cau DangCongBo, MaTaiKhoan nguoi goi")]
    public Task UT12d_ValidPublish()=>UT111213.Check("UT-12d","XL1","Cong bo nhu cau hop le",
        "KH01 tao nhu cau moi; input hop le; pham vi Public",
        "Dung 1 nhu cau DangCongBo; OwnerId=KH01; ThoiDiemDang=BayGioUtc",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        var before=DateTime.UtcNow;
        var r=await new CommunityManager(db).Save(UT111213.KH01,UT111213.Input("Public"));
        var after=DateTime.UtcNow;
        var saved=await db.Companions.AsNoTracking().ToListAsync();
        var item=saved.SingleOrDefault();
        observe($"Success={r.Success}; Message={r.Message}; Id={r.Id}; Count={saved.Count}; OwnerId={item?.OwnerId}; IsPublished={item?.IsPublished}; CreatedAt={item?.CreatedAt:O}; Before={before:O}; After={after:O}");
        Assert.True(r.Success);Assert.Single(saved);Assert.NotNull(item);
        Assert.Equal(UT111213.KH01,item.OwnerId);Assert.True(item.IsPublished);Assert.Equal(item.Id,r.Id);
        Assert.InRange(item.CreatedAt,before,after);
    });
}
