using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;
namespace TuTuc.Tests.Unit;
public sealed class UT33_PhanHoiDeXuat_Tests
{
    [Fact(DisplayName="UT-33a | KT2 | Phản hồi khi chưa có đề xuất")]
    public Task UT33a_Test()=>UT323334.Check("UT-33a","KT2","Phản hồi khi chưa có đề xuất","Status=Chờ phản hồi","0701-05",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Chờ phản hồi",1);
        await using (db)
        {
            var code=await UT323334.ReplyCode(manager,UT293031.Khach,UT323334.Id,true);observe($"Code={code}");Assert.Equal("0701-05",code);
        }
    });
    [Fact(DisplayName="UT-33b | KT2 | Phản hồi phiên bản cũ")]
    public Task UT33b_Test()=>UT323334.Check("UT-33b","KT2","Phản hồi phiên bản cũ","Status=Đề xuất khác; ConditionsVersion=2; gửi version=1","0701-05",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đề xuất khác",2);
        await using (db)
        {
            var code=await UT323334.ReplyCode(manager,UT293031.Khach,UT323334.Id,true,1);observe($"Code={code}");Assert.Equal("0701-05",code);
        }
    });
    [Fact(DisplayName="UT-33c | XL1 | Khách đồng ý đề xuất")]
    public Task UT33c_Test()=>UT323334.Check("UT-33c","XL1","Khách đồng ý đề xuất","Đề xuất khác; IsAccepting=true","Đã xác nhận; thông báo cho NCC",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đề xuất khác",1);
        await using (db)
        {
            var code=await UT323334.ReplyCode(manager,UT293031.Khach,UT323334.Id,true);var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={code};Status={item.Status};NCCNotices={notices.Count}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Đã xác nhận",item.Status);Assert.Contains(notices,n=>n.Title.Contains("YeuCauDaXacNhan",StringComparison.OrdinalIgnoreCase));
        }
    });
    [Fact(DisplayName="UT-33d | XL2 | Khách đồng ý khi NCC ngừng nhận")]
    public Task UT33d_Test()=>UT323334.Check("UT-33d","XL2","Khách đồng ý khi NCC ngừng nhận","Đề xuất khác; IsAccepting=false","Không tiếp tục; đúng lý do; mã 0701-02 trong kết quả",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đề xuất khác",1);
        await using (db)
        {
            var service=await db.Services.SingleAsync(x=>x.Id==UT293031.DichVu);service.IsAccepting=false;await db.SaveChangesAsync();var code=await UT323334.ReplyCode(manager,UT293031.Khach,UT323334.Id,true);var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);observe($"Return={code};Status={item.Status};Reason={item.ResponseReason}");Assert.Equal("Không tiếp tục",item.Status);Assert.Equal("Nhà cung cấp không còn tiếp nhận yêu cầu mới.",item.ResponseReason);Assert.Contains("0701-02",code);
        }
    });
    [Fact(DisplayName="UT-33e | XL3 | Khách không đồng ý đề xuất")]
    public Task UT33e_Test()=>UT323334.Check("UT-33e","XL3","Khách không đồng ý đề xuất","Đề xuất khác; accept=false; lý do=Không đổi được ngày","Không tiếp tục; lưu lý do; thông báo NCC",async observe=>
    {
        var (db,manager)=await UT323334.Setup("Đề xuất khác",1);
        await using (db)
        {
            var code=await UT323334.ReplyCode(manager,UT293031.Khach,UT323334.Id,false);var item=await db.Bookings.AsNoTracking().SingleAsync(x=>x.Id==UT323334.Id);var notices=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==UT293031.NhaCungCap).ToListAsync();observe($"Return={code};Status={item.Status};Reason={item.ResponseReason};NCCNotices={notices.Count}");Assert.StartsWith("NO_ERROR:",code);Assert.Equal("Không tiếp tục",item.Status);Assert.Equal("Không đổi được ngày",item.ResponseReason);Assert.Contains(notices,n=>n.Title.Contains("YeuCauKhongTiepTuc",StringComparison.OrdinalIgnoreCase));
        }
    });
}
