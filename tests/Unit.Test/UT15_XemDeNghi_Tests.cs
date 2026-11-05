using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT15_XemDeNghi_Tests
{
    [Fact(DisplayName="UT-15a | KT1 | Nguoi thu ba mo de nghi => 0401-05")]
    public Task UT15a_ThirdParty()=>UT141516.Check("UT-15a","KT1","Nguoi thu ba truy cap de nghi",
        "KH03 xem de nghi #998 giua KH01 va KH02","Loi 0401-05; khong lo thong tin de nghi",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var dash=await new CommunityManager(db).Dashboard(UT141516.KH03);
        record($"Dashboard.Connections={dash.Connections.Count}; ErrorCode=KHONG CO; endpoint xem de nghi rieng=KHONG CO");
        Assert.Empty(dash.Connections);
        Assert.Fail("UT-15a: Dashboard an du lieu nguoi thu ba nhung khong phat sinh ma loi 0401-05 cua XemDeNghiAsync.");
    });

    [Fact(DisplayName="UT-15b | XL2 | Nguoi nhan: ChapNhan/TuChoi, an lien he")]
    public Task UT15b_ReceiverPending()=>UT141516.Check("UT-15b","XL2","Quyen thao tac nguoi nhan va an thong tin lien he",
        "KH02 xem de nghi ChoPhanHoi tu KH01", "Thao tac {ChapNhan,TuChoi}; khong Email/SDT KH01",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var dash=await new CommunityManager(db).Dashboard(UT141516.KH02);
        var row=Assert.Single(dash.Connections);
        record($"Viewer=KH02; Status={row.Connection.Status}; OtherEmail={row.OtherEmail??"NULL"}; OtherPhone={row.OtherPhone??"NULL"}; AllowedActions=KHONG CO TRONG DTO");
        Assert.Null(row.OtherEmail);Assert.Null(row.OtherPhone);
        Assert.Fail("UT-15b: Da kiem tra an lien he, nhung Dashboard DTO khong co danh sach thao tac {ChapNhan,TuChoi} de xac nhan XL2.");
    });

    [Fact(DisplayName="UT-15c | XL2 | Nguoi gui: chi co Rut")]
    public Task UT15c_SenderPending()=>UT141516.Check("UT-15c","XL2","Quyen thao tac nguoi gui",
        "KH01 xem de nghi ChoPhanHoi ma minh da gui", "Chi co thao tac {Rut}",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var dash=await new CommunityManager(db).Dashboard(UT141516.KH01);
        var row=Assert.Single(dash.Connections);
        record($"Viewer=KH01; SenderId={row.Connection.SenderId}; Status={row.Connection.Status}; AllowedActions=KHONG CO TRONG DTO");
        Assert.Equal(UT141516.KH01,row.Connection.SenderId);
        Assert.Fail("UT-15c: Dashboard DTO khong truyen danh sach thao tac {Rut}; chua the xac nhan dung XL2.");
    });

    [Fact(DisplayName="UT-15d | XL1/XL2 | DaChapNhan: TaoThoaThuan, co lien he hai ben")]
    public Task UT15d_Accepted()=>UT141516.Check("UT-15d","XL1, XL2","Quyen tao thoa thuan va hien lien he",
        "KH01 va KH02 co de nghi DaChapNhan, profile cho phep chia se", "Thao tac {TaoThoaThuan}; hien lien he hai ben",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection(status:"Đã chấp nhận"));await db.SaveChangesAsync();
        var dash=await new CommunityManager(db).Dashboard(UT141516.KH01);
        var row=Assert.Single(dash.Connections);
        record($"Status={row.Connection.Status}; OtherEmail={row.OtherEmail??"NULL"}; OtherPhone={row.OtherPhone??"NULL"}; ActionTaoThoaThuan=KHONG CO TRONG DTO; ShareContact=false mac dinh");
        Assert.Equal("Đã chấp nhận",row.Connection.Status);
        Assert.Fail("UT-15d: Chua co quyen TaoThoaThuan va lien he hai ben dung thiet ke tu du lieu gia lap; can kiem chung ca view va chinh sach chia se.");
    });

    [Fact(DisplayName="UT-15e | XL3 | Chi thong bao cung URL chuyen DaDoc")]
    public Task UT15e_ReadNotification()=>UT141516.Check("UT-15e","XL3","Danh dau da doc thong bao dung duong dan",
        "Co 2 thong bao chua doc, chi 1 cai cung URL de nghi", "Thong bao dung URL IsRead=true, thong bao khac giu false",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());
        db.Notifications.AddRange(
            new UserNotification{Id=901,OwnerId=UT141516.KH02,Url="/dong-hanh/de-nghi/xem-de-nghi/998",IsRead=false},
            new UserNotification{Id=902,OwnerId=UT141516.KH02,Url="/khac",IsRead=false});
        await db.SaveChangesAsync();
        _=await new CommunityManager(db).Dashboard(UT141516.KH02);
        var a=await db.Notifications.AsNoTracking().SingleAsync(n=>n.Id==901);
        var b=await db.Notifications.AsNoTracking().SingleAsync(n=>n.Id==902);
        record($"ReadURL={a.Url}; Read.IsRead={a.IsRead}; OtherURL={b.Url}; Other.IsRead={b.IsRead}");
        Assert.True(a.IsRead);Assert.False(b.IsRead);
    });
}
