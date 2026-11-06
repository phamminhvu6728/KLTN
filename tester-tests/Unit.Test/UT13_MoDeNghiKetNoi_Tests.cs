using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-13: MoDeNghiAsync. Bang 4.194. Code hien tai co Detail/Connect, chua co MoDeNghiAsync.</summary>
public sealed class UT13_MoDeNghiKetNoi_Tests
{
    [Fact(DisplayName="UT-13a | KT1 | Nhu cau DungCongBo => 0401-01")]
    public Task UT13a_Unpublished()=>UT111213.Check("UT-13a","KT1","Nhu cau dung cong bo",
        "NC01.IsPublished=false; KH01 mo de nghi","Loi 0401-01",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        db.Companions.Add(UT111213.Demand(published:false));await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT111213.KH01,UT111213.NC01);
        observe($"Detail={(card==null?"NULL":"CO")}; errorCode=KHONG CO");
        Assert.Null(card);
        Assert.Fail("UT-13a: Detail tra null, khong co loi 0401-01 theo buoc KT1 cua MoDeNghiAsync.");
    });

    [Fact(DisplayName="UT-13b | KT1 | Chu nhu cau DaNgung => 0401-01")]
    public Task UT13b_OwnerInactive()=>UT111213.Check("UT-13b","KT1","Chu nhu cau da ngung tai khoan",
        "KH02.IsActive=false; NC01 DangCongBo","Loi 0401-01",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db,kh02Active:false);
        db.Companions.Add(UT111213.Demand());await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT111213.KH01,UT111213.NC01);
        observe($"Detail={(card==null?"NULL":"CO")}; errorCode=KHONG CO");
        Assert.Null(card);
        Assert.Fail("UT-13b: Khong tim thay nhu cau la dung, nhung chua xac nhan ma loi 0401-01.");
    });

    [Fact(DisplayName="UT-13c | KT2 | Khong duoc de nghi cho chinh minh => 0401-02")]
    public Task UT13c_SelfRequest()=>UT111213.Check("UT-13c","KT2","Khong duoc mo de nghi cho chinh minh",
        "KH02 la chu NC01 va cung la nguoi xem","Loi 0401-02",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        db.Companions.Add(UT111213.Demand());await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT111213.KH02,UT111213.NC01);
        observe($"Detail={(card==null?"NULL":"CO")}; CanConnect={card?.CanConnect}; errorCode=KHONG CO");
        Assert.NotNull(card);Assert.False(card.CanConnect);
        Assert.Fail("UT-13c: Moi kiem tra CanConnect=false, chua co phan hoi ma loi 0401-02 tu MoDeNghiAsync.");
    });

    [Fact(DisplayName="UT-13d | KT3 | Da co ChoPhanHoi => 0401-03")]
    public Task UT13d_Pending()=>UT111213.Check("UT-13d","KT3","Da co de nghi ChoPhanHoi",
        "KH01 da gui de nghi ChoPhanHoi den NC01","Loi 0401-03; khong cho mo bieu mau moi",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        db.Companions.Add(UT111213.Demand());
        db.Connections.Add(new Connection{Id=1304,CompanionId=UT111213.NC01,SenderId=UT111213.KH01,Status="Chờ phản hồi"});
        await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT111213.KH01,UT111213.NC01);
        observe($"CanConnect={card?.CanConnect}; errorCode=KHONG CO");
        Assert.NotNull(card);Assert.False(card.CanConnect);
        Assert.Fail("UT-13d: Chua co ma loi 0401-03 cho MoDeNghiAsync; CanConnect=false chua du.");
    });

    [Fact(DisplayName="UT-13e | KT3 | Da co DaChapNhan => 0401-04")]
    public Task UT13e_Accepted()=>UT111213.Check("UT-13e","KT3","Da co de nghi DaChapNhan",
        "KH01 da co ket noi DaChapNhan toi NC01","Loi 0401-04; khong cho mo bieu mau moi",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        db.Companions.Add(UT111213.Demand());
        db.Connections.Add(new Connection{Id=1305,CompanionId=UT111213.NC01,SenderId=UT111213.KH01,Status="Đã chấp nhận"});
        await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT111213.KH01,UT111213.NC01);
        observe($"CanConnect={card?.CanConnect}; Connected={card?.Connected}; errorCode=KHONG CO");
        Assert.NotNull(card);Assert.False(card.CanConnect);Assert.True(card.Connected);
        Assert.Fail("UT-13e: Chua co ma loi 0401-04 cho MoDeNghiAsync.");
    });

    [Fact(DisplayName="UT-13f | XL1 | TuChoi hoac DaRut cho phep tao de nghi moi")]
    public Task UT13f_RejectedOrWithdrawn()=>UT111213.Check("UT-13f","XL1","TuChoi hoac DaRut cho phep mo de nghi moi",
        "KH01 chi co de nghi TuChoi/DaRut toi NC01","Tra bieu mau gui de nghi",async observe=>
    {
        await using var db=UT111213.Db();await UT111213.Seed(db);
        db.Companions.AddRange(UT111213.Demand(id:1310),UT111213.Demand(id:1311));
        db.Connections.AddRange(
            new Connection{Id=1312,CompanionId=1310,SenderId=UT111213.KH01,Status="Từ chối"},
            new Connection{Id=1313,CompanionId=1311,SenderId=UT111213.KH01,Status="Đã rút"});
        await db.SaveChangesAsync();
        var manager=new CommunityManager(db);
        var rejected=await manager.Detail(UT111213.KH01,1310);
        var withdrawn=await manager.Detail(UT111213.KH01,1311);
        observe($"Rejected CanConnect={rejected?.CanConnect}; Withdrawn CanConnect={withdrawn?.CanConnect}; Form=KHONG CO MO_DE_NGHI");
        Assert.NotNull(rejected);Assert.NotNull(withdrawn);
        Assert.True(rejected.CanConnect);Assert.True(withdrawn.CanConnect);
        Assert.Fail("UT-13f: Chi xac minh quyen CanConnect; chua xac minh bieu mau gui de nghi theo XL1.");
    });
}
