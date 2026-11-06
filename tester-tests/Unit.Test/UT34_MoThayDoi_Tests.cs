using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;
namespace TuTuc.Tests.Unit;
public sealed class UT34_MoThayDoi_Tests
{
    [Fact(DisplayName="UT-34a | KT1 | KH02 mở yêu cầu của KH01")]
    public Task UT34a_Test()=>UT323334.Check("UT-34a","KT1","KH02 mở yêu cầu của KH01","KH02 xem booking của KH01","0801-01",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi");
        await using (db)
        {
            var vm=await manager.Requests(UT293031.KhachKhac,false,requestId:UT323334.Id);observe($"ReturnedCount={vm.Bookings.Count};ErrorCode=(none)");Assert.Empty(vm.Bookings);Assert.Fail("Theo Bảng 4.227 phải trả lỗi 0801-01, nhưng Requests không có mã lỗi.");
        }
    });
    [Fact(DisplayName="UT-34b | KT2 | Có đề nghị đang chờ phản hồi")]
    public Task UT34b_Test()=>UT323334.Check("UT-34b","KT2","Có đề nghị đang chờ phản hồi","Booking Đã xác nhận; Change Chờ phản hồi","Trả đề nghị đang chờ thay vì form",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đã xác nhận");
        await using (db)
        {
            db.BookingChanges.Add(new BookingChange{BookingId=UT323334.Id,RequesterId=UT293031.Khach,Kind="Điều chỉnh",Status="Chờ phản hồi",Date=new DateTime(2026,12,21),People=2,Guests=2});await db.SaveChangesAsync();var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);var found=vm.Changes.Where(x=>x.BookingId==UT323334.Id&&x.Status=="Chờ phản hồi").ToList();observe($"PendingChanges={found.Count};FormOrChangeDecision=(not exposed)");Assert.Single(found);Assert.Fail("Requests trả danh sách Changes nhưng không trả quyết định thay biểu mẫu bằng đề nghị đang chờ như Bảng 4.227.");
        }
    });
    [Fact(DisplayName="UT-34c | XL1 | Yêu cầu chờ phản hồi")]
    public Task UT34c_Test()=>UT323334.Check("UT-34c","XL1","Yêu cầu chờ phản hồi","Status=Chờ phản hồi; ngày 10/12","Chỉ {SuaYeuCau,RutYeuCau}",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi");
        await using (db)
        {
            var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);var b=Assert.Single(vm.Bookings);observe($"Status={b.Status};Actions=(not exposed)");Assert.Fail("Không có tập hành động SuaYeuCau/RutYeuCau trong RequestListVm để xác minh đúng XL1.");
        }
    });
    [Fact(DisplayName="UT-34d | XL1 | Đề xuất khác")]
    public Task UT34d_Test()=>UT323334.Check("UT-34d","XL1","Đề xuất khác","Status=Đề xuất khác; ngày 10/12","Chỉ {RutYeuCau}",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đề xuất khác");
        await using (db)
        {
            var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);var b=Assert.Single(vm.Bookings);observe($"Status={b.Status};Actions=(not exposed)");Assert.Fail("Không có tập hành động RutYeuCau trong RequestListVm để xác minh đúng XL1.");
        }
    });
    [Fact(DisplayName="UT-34e | XL1 biên | Ngày bắt đầu 04/11; đã xác nhận")]
    public Task UT34e_Test()=>UT323334.Check("UT-34e","XL1 biên","Ngày bắt đầu 04/11; đã xác nhận","Ngày bắt đầu=04/11/2026","{ThayDoi,Huy} khi chưa đến ngày phục vụ",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đã xác nhận");
        await using (db)
        {
            var b=await db.Bookings.SingleAsync(x=>x.Id==UT323334.Id);b.Date=new DateTime(2026,11,4);await db.SaveChangesAsync();var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);observe($"Date={b.Date:dd/MM/yyyy};Today=03/11/2026;Actions=(not exposed)");Assert.Single(vm.Bookings);Assert.Fail("Không có thông tin quyền ThayDoi/Huy để xác minh XL1 ở ngày biên.");
        }
    });
    [Fact(DisplayName="UT-34f | KT3 biên | Ngày bắt đầu 03/11 (hôm nay)")]
    public Task UT34f_Test()=>UT323334.Check("UT-34f","KT3 biên","Ngày bắt đầu 03/11 (hôm nay)","Ngày bắt đầu=03/11/2026","0801-02",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đã xác nhận");
        await using (db)
        {
            var b=await db.Bookings.SingleAsync(x=>x.Id==UT323334.Id);b.Date=new DateTime(2026,11,3);await db.SaveChangesAsync();var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);observe($"Date={b.Date:dd/MM/yyyy};ErrorCode=(none)");Assert.Single(vm.Bookings);Assert.Fail("Chức năng Requests không trả mã 0801-02 khi ngày bắt đầu là hôm nay; cần kiểm tra hàm mở thay đổi tương ứng.");
        }
    });
    [Fact(DisplayName="UT-34g | KT3 | Đã có kết quả thực hiện")]
    public Task UT34g_Test()=>UT323334.Check("UT-34g","KT3","Đã có kết quả thực hiện","Đã xác nhận; có kết quả","0801-02",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đã xác nhận");
        await using (db)
        {
            db.BookingItemResults.Add(new BookingItemResult{BookingId=UT323334.Id,ItemKey="main",ItemTitle="Hạng mục",Outcome="Hoàn thành"});await db.SaveChangesAsync();var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);observe($"ResultRows={vm.Results.Count};ErrorCode=(none)");Assert.NotEmpty(vm.Results);Assert.Fail("Chức năng Requests không trả mã 0801-02 cho trường hợp đã có kết quả.");
        }
    });
    [Fact(DisplayName="UT-34h | KT3 | Trạng thái không được đổi/hủy")]
    public Task UT34h_Test()=>UT323334.Check("UT-34h","KT3","Trạng thái không được đổi/hủy","TuChoi,KhongTiepTuc,DaHuy,HoanThanh","0801-02 cho cả bốn trạng thái",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Từ chối");
        await using (db)
        {
            var values=new[]{"Từ chối","Không tiếp tục","Đã hủy","Hoàn thành"};var outputs=new System.Collections.Generic.List<string>();foreach(var state in values){var b=await db.Bookings.SingleAsync(x=>x.Id==UT323334.Id);b.Status=state;await db.SaveChangesAsync();var vm=await manager.Requests(UT293031.Khach,false,requestId:UT323334.Id);outputs.Add($"{state}:rows={vm.Bookings.Count};code=none");}observe(string.Join(";",outputs));Assert.Fail("Chức năng xem yêu cầu chưa trả 0801-02 hoặc tập thao tác đổi/hủy để đối chiếu cả bốn trạng thái.");
        }
    });
}
