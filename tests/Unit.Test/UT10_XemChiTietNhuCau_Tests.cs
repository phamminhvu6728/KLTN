using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>UT-10, Bảng 4.189. Phụ thuộc UT910 trong UT09_TimNhuCau_Tests.cs.</summary>
public sealed class UT10_XemChiTietNhuCau_Tests
{
    [Fact(DisplayName="UT-10a | KT1 | Nhu cau khong duoc phep xem => 0301-02")]
    public Task UT10a_NotVisible()=>UT910.Report("UT-10a","KT1","Nhu cầu ngoài phạm vi nhìn thấy",
        "Người xem chưa đăng nhập; NC01 SharingScope=Connected",
        "Lỗi 0301-02",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Detail(0,UT910.NC01);
        string? errorCode=null;
        record($"result={(result==null?"NULL":"DemandCard")}; errorCode={errorCode??"KHONG CO"}");
        Assert.Null(result);Assert.Equal("0301-02",errorCode);
    });

    [Fact(DisplayName="UT-10b | XL2 | Da gui ChoPhanHoi khong cho gui tiep")]
    public Task UT10b_PendingProposal()=>UT910.Report("UT-10b","XL2","Đã gửi đề nghị Chờ phản hồi",
        "KH01 đã có đề nghị ChoPhanHoi cho NC01","coTheGuiDeNghi=false",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());
        db.Connections.Add(new Connection{Id=1001,CompanionId=UT910.NC01,SenderId=UT910.KH01,Status="Chờ phản hồi"});await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT910.KH01,UT910.NC01);
        record($"card={(card==null?"NULL":"CO")}; canConnect={card?.CanConnect}");
        Assert.NotNull(card);Assert.False(card!.CanConnect);
    });

    [Fact(DisplayName="UT-10c | XL2 | De nghi TuChoi cho phep gui lai")]
    public Task UT10c_RejectedProposal()=>UT910.Report("UT-10c","XL2","Đề nghị bị từ chối được gửi lại",
        "KH01 chỉ có đề nghị TuChoi tới NC01","coTheGuiDeNghi=true",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());
        db.Connections.Add(new Connection{Id=1002,CompanionId=UT910.NC01,SenderId=UT910.KH01,Status="Từ chối"});await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(UT910.KH01,UT910.NC01);
        record($"card={(card==null?"NULL":"CO")}; canConnect={card?.CanConnect}");
        Assert.NotNull(card);Assert.True(card!.CanConnect);
    });

    [Fact(DisplayName="UT-10d | Ket qua | Khach chua dang nhap co ReturnUrl")]
    public Task UT10d_LoginReturnUrl()=>UT910.Report("UT-10d","Kết quả","Nút đăng nhập với ReturnUrl",
        "Khách chưa đăng nhập truy cập nhu cầu công khai NC01","Có nút đăng nhập mang ReturnUrl của trang chi tiết",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand(scope:"Public"));await db.SaveChangesAsync();
        var card=await new CommunityManager(db).Detail(0,UT910.NC01);
        // CommunityManager.Detail chỉ trả DemandCard và không có LoginUrl/ReturnUrl.
        string? loginUrl=null;
        record($"card={(card==null?"NULL":"CO")}; canConnect={card?.CanConnect}; loginUrl={loginUrl??"KHONG CO"}");
        Assert.NotNull(card);Assert.False(card!.CanConnect);
        Assert.Fail("UT-10d: ReturnUrl nằm trong Views/Community/Details.cshtml (asp-route-returnUrl), không nằm trong DemandCard. Cần kiểm thử kết quả render Razor; không đánh FAIL là lỗi nghiệp vụ Dev.");
    });
}
