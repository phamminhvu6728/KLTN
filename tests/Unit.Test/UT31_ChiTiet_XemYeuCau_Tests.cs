using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Sử dụng lớp UT293031 trong UT29.
public sealed class UT31_ChiTiet_XemYeuCau_Tests
{
    [Fact(DisplayName = "UT-31a | ChiTiet KT1 | KH02 xem booking của KH01")]
    public Task UT31a_Test() => UT293031.Check("UT-31a", "ChiTiet KT1", "KH02 xem booking của KH01", "KH02 request BookingId=29001 thuộc KH01", "Lỗi 0701-04; không tiết lộ booking", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var vm=await manager.Requests(UT293031.KhachKhac,false,requestId:29001);
        observe($"ReturnedCount={vm.Bookings.Count};ErrorCode=(không có)");
        Assert.Empty(vm.Bookings);
        Assert.Fail("Theo Bảng 4.222 cần trả mã 0701-04; Requests chỉ trả danh sách rỗng, chưa trả mã lỗi.");
    });
    [Fact(DisplayName = "UT-31b | ChiTiet XL2 | Thao tác khi Chờ phản hồi")]
    public Task UT31b_Test() => UT293031.Check("UT-31b", "ChiTiet XL2", "Thao tác khi Chờ phản hồi", "Booking.Status=Chờ phản hồi", "Chỉ {SuaYeuCau,RutYeuCau}", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var vm=await manager.Requests(UT293031.Khach,false,requestId:29001);
        var b=Assert.Single(vm.Bookings);
        observe($"Status={b.Status};AvailableActions=(không có thuộc tính trong RequestListVm)");
        Assert.Equal("Chờ phản hồi",b.Status);
        Assert.Fail("RequestListVm không trả các thao tác SuaYeuCau/RutYeuCau; cần kiểm chứng phân nhánh View hoặc bổ sung dữ liệu ra từ Service.");
    });
    [Fact(DisplayName = "UT-31c | ChiTiet XL2 | Thao tác khi Đề xuất khác")]
    public Task UT31c_Test() => UT293031.Check("UT-31c", "ChiTiet XL2", "Thao tác khi Đề xuất khác", "Booking.Status=Đề xuất khác", "Chỉ {PhanHoiDeXuat,RutYeuCau}", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db,"Đề xuất khác");
        var vm=await manager.Requests(UT293031.Khach,false,requestId:29001);
        var b=Assert.Single(vm.Bookings);
        observe($"Status={b.Status};AvailableActions=(không có thuộc tính trong RequestListVm)");
        Assert.Equal("Đề xuất khác",b.Status);
        Assert.Fail("Chưa xác minh được danh sách thao tác PhanHoiDeXuat/RutYeuCau vì ViewModel không cung cấp trạng thái quyền thao tác.");
    });
    [Fact(DisplayName = "UT-31d | ChiTiet XL2 | Thao tác khi Đã xác nhận")]
    public Task UT31d_Test() => UT293031.Check("UT-31d", "ChiTiet XL2", "Thao tác khi Đã xác nhận", "Booking.Status=Đã xác nhận", "Chỉ {DeNghiDieuChinh}", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db,"Đã xác nhận");
        var vm=await manager.Requests(UT293031.Khach,false,requestId:29001);
        var b=Assert.Single(vm.Bookings);
        observe($"Status={b.Status};AvailableActions=(không có thuộc tính trong RequestListVm)");
        Assert.Equal("Đã xác nhận",b.Status);
        Assert.Fail("Chưa có dữ liệu thao tác DeNghiDieuChinh trong RequestListVm; cần xác minh ở View hoặc logic quyền thao tác.");
    });
    [Fact(DisplayName = "UT-31e | XemYeuCau KT1 | Nhà cung cấp khác mở booking")]
    public Task UT31e_Test() => UT293031.Check("UT-31e", "XemYeuCau KT1", "Nhà cung cấp khác mở booking", "NCC04 xem booking của DV02 (NCC01)", "Lỗi 0701-04", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var vm=await manager.Requests(UT293031.NhaCungCapKhac,true,requestId:29001);
        observe($"ReturnedCount={vm.Bookings.Count};ErrorCode=(không có)");
        Assert.Empty(vm.Bookings);
        Assert.Fail("Theo Bảng 4.224 cần trả 0701-04; Requests hiện không trả BusinessFault khi NCC không có quyền.");
    });
    [Fact(DisplayName = "UT-31f | XemYeuCau XL2 | Không còn tiếp nhận")]
    public Task UT31f_Test() => UT293031.Check("UT-31f", "XemYeuCau XL2", "Không còn tiếp nhận", "NCC01 xem Booking Chờ phản hồi; IsAccepting=false", "Chỉ còn thao tác TuChoi", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var s=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);
        s.IsAccepting=false;await db.SaveChangesAsync();
        var vm=await manager.Requests(UT293031.NhaCungCap,true,requestId:29001);
        var b=Assert.Single(vm.Bookings);
        observe($"Status={b.Status};IsAccepting={b.TravelService?.IsAccepting};Actions=(không có trong VM)");
        Assert.False(b.TravelService!.IsAccepting);
        Assert.Fail("Chưa kiểm chứng được chỉ có quyền TuChoi: RequestListVm không chứa tập thao tác hiển thị.");
    });
    [Fact(DisplayName = "UT-31g | XemYeuCau XL1 | Ẩn hiện liên hệ khách")]
    public Task UT31g_Test() => UT293031.Check("UT-31g", "XemYeuCau XL1", "Ẩn hiện liên hệ khách", "Booking Chờ phản hồi -> Đã xác nhận", "Liên hệ khách chỉ có ở Đã xác nhận", async observe =>
    {
        await using var db = UT232425.Db();
        await UT293031.Seed(db);
        var manager = UT293031.Manager(db);
        await UT293031.AddBooking(db);
        var account=await db.Accounts.SingleAsync(x=>x.Id==UT293031.Khach);
        account.Phone="0912345678";await db.SaveChangesAsync();
        var before=await manager.Requests(UT293031.NhaCungCap,true,requestId:29001);
        var hidden=before.CustomerContacts.ContainsKey(UT293031.Khach);
        var b=await db.Bookings.SingleAsync(x=>x.Id==29001);b.Status="Đã xác nhận";await db.SaveChangesAsync();
        var after=await manager.Requests(UT293031.NhaCungCap,true,requestId:29001);
        var visible=after.CustomerContacts.TryGetValue(UT293031.Khach,out var contact);
        observe($"BeforeVisible={hidden};AfterVisible={visible};Contact={contact}");
        Assert.False(hidden);Assert.True(visible);Assert.Contains("0912345678",contact);
    });
}
