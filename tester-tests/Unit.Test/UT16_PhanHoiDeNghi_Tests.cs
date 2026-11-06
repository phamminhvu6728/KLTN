using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT16_PhanHoiDeNghi_Tests
{
    [Fact(DisplayName="UT-16a | KT1 | Nguoi thu ba => 0401-05")]
    public Task UT16a_ThirdParty()=>UT141516.Check("UT-16a","KT1","Nguoi thu ba phan hoi de nghi",
        "KH03 goi Respond de nghi #998", "Loi 0401-05",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH03,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"accept");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal("Chờ phản hồi",c.Status);Assert.Contains("0401-05",r.Message);
    });

    [Fact(DisplayName="UT-16b | KT2 | Nguoi gui khong duoc ChapNhan => 0401-06")]
    public Task UT16b_SenderAccept()=>UT141516.Check("UT-16b","KT2","Nguoi gui tu ChapNhan",
        "KH01 gui decision=accept", "Loi 0401-06",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH01,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"accept");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal("Chờ phản hồi",c.Status);Assert.Contains("0401-06",r.Message);
    });

    [Fact(DisplayName="UT-16c | KT2 | Nguoi nhan khong duoc Rut => 0401-06")]
    public Task UT16c_ReceiverWithdraw()=>UT141516.Check("UT-16c","KT2","Nguoi nhan tu Rut",
        "KH02 gui decision=withdraw", "Loi 0401-06",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH02,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"withdraw");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal("Chờ phản hồi",c.Status);Assert.Contains("0401-06",r.Message);
    });

    [Fact(DisplayName="UT-16d | KT3 | DaRut khong duoc xu ly lai => 0401-07")]
    public Task UT16d_WithdrawnAlready()=>UT141516.Check("UT-16d","KT3","Khong phan hoi de nghi DaRut",
        "De nghi #998 Status=DaRut; KH02 accept", "Loi 0401-07",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection(status:"Đã rút"));await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH02,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"accept");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.Equal("Đã rút",c.Status);Assert.Contains("0401-07",r.Message);
    });

    [Fact(DisplayName="UT-16e | KT4 | ChapNhan khi nguoi gui DaNgung => 0401-08")]
    public Task UT16e_SenderInactiveAccept()=>UT141516.Check("UT-16e","KT4","ChapNhan khi nguoi gui da ngung",
        "KH01.IsActive=false; KH02 decision=accept", "Loi 0401-08; khong chap nhan",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db,senderActive:false);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH02,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"accept");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}; MaLoi=KHONG CO");
        Assert.False(r.Success);Assert.NotEqual("Đã chấp nhận",c.Status);Assert.Contains("0401-08",r.Message);
    });

    [Fact(DisplayName="UT-16f | KT4 | TuChoi duoc phep khi nguoi gui DaNgung")]
    public Task UT16f_SenderInactiveReject()=>UT141516.Check("UT-16f","KT4","TuChoi duoc phep khi nguoi gui da ngung",
        "KH01.IsActive=false; KH02 decision=reject", "Hop le; de nghi TuChoi",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db,senderActive:false);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH02,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"reject");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        record($"Success={r.Success}; StatusCode={r.StatusCode}; Message={r.Message}; ConnectionStatus={c.Status}");
        Assert.True(r.Success);Assert.Equal("Từ chối",c.Status);
    });

    [Fact(DisplayName="UT-16g | KT5 | Concurrency => 0401-07; khong thong bao")]
    public Task UT16g_Concurrency()=>UT141516.Check("UT-16g","KT5","SaveChanges nem DbUpdateConcurrencyException",
        "Giai lap SaveChanges xung dot dong thoi", "Loi 0401-07; khong ghi thong bao",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var before=await db.Notifications.CountAsync();
        record($"NotificationsBefore={before}; ConcurrencyException=CHUA INJECT; DbContext EF InMemory khong gia lap duoc loi khi SaveChanges; ErrorCode=KHONG CO");
        Assert.Fail("UT-16g CHUA KIEM DUNG DIEU KIEN: can inject DbUpdateConcurrencyException tai SaveChanges va xac nhan ma 0401-07, khong thong bao. Khong duoc bao PASS bang cach thay loi concurrency bang version sai.");
    });

    [Fact(DisplayName="UT-16h | XL1/XL2 | ChapNhan, thoi gian va thong bao")]
    public Task UT16h_AcceptValid()=>UT141516.Check("UT-16h","XL1, XL2","ChapNhan de nghi hop le",
        "KH02 ChapNhan de nghi ChoPhanHoi KH01", "DaChapNhan; ThoiDiemPhanHoi=BayGioUtc; thong bao DeNghiDuocChapNhan den KH01",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var before=DateTime.UtcNow;var initialNotifications=await db.Notifications.CountAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH02,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"accept");
        var after=DateTime.UtcNow;var c=await db.Connections.AsNoTracking().SingleAsync();
        var ns=await db.Notifications.AsNoTracking().Where(n=>n.OwnerId==UT141516.KH01).ToListAsync();
        record($"Success={r.Success}; Status={c.Status}; RespondedAt={c.RespondedAt:O}; NotifyCount={ns.Count}; NotifyTypes=[{string.Join(',',ns.Select(n=>n.Type))}]; InitialNotificationCount={initialNotifications}");
        Assert.True(r.Success);Assert.Equal("Đã chấp nhận",c.Status);Assert.NotNull(c.RespondedAt);
        Assert.InRange(c.RespondedAt.Value,before,after);
        Assert.Contains(ns,n=>n.Type=="DeNghiDuocChapNhan");
    });

    [Fact(DisplayName="UT-16i | XL1/XL2 | Rut, thong bao ben nhan")]
    public Task UT16i_WithdrawValid()=>UT141516.Check("UT-16i","XL1, XL2","Rut de nghi hop le",
        "KH01 gui decision=withdraw cho de nghi ChoPhanHoi", "TrangThai=DaRut; thong bao DeNghiDaRut cho KH02",async record=>
    {
        await using var db=UT141516.Db();await UT141516.Seed(db);
        db.Companions.Add(UT141516.Demand());db.Connections.Add(UT141516.Connection());await db.SaveChangesAsync();
        var r=await new CommunityManager(db).Respond(UT141516.KH01,998,Convert.ToBase64String(new byte[]{1,2,3,4,5,6,7,8}),"withdraw");
        var c=await db.Connections.AsNoTracking().SingleAsync();
        var ns=await db.Notifications.AsNoTracking().Where(n=>n.OwnerId==UT141516.KH02).ToListAsync();
        record($"Success={r.Success}; Status={c.Status}; NotifyCount={ns.Count}; NotifyTypes=[{string.Join(',',ns.Select(n=>n.Type))}]");
        Assert.True(r.Success);Assert.Equal("Đã rút",c.Status);
        Assert.Contains(ns,n=>n.Type=="DeNghiDaRut");
    });
}
