using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;
using Microsoft.AspNetCore.Mvc;
using TuTuc.Controllers;

public sealed class UT27_XemNoiDung_Tests
{
    [Fact(DisplayName="UT-27a | KT1 | Bản cập nhật không tồn tại")]
    public Task UT27a_Test()=>UT262728.Check("UT-27a","KT1","Bản cập nhật không tồn tại","maBanCapNhat=999999","Lỗi 0602-01",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        var id=999999;var revision=await db.ServiceRevisions.SingleOrDefaultAsync(x=>x.Id==id);
        observe($"Revision={revision?.Id.ToString()??"NULL"}; No dedicated XemNoiDungAsync/business error 0602-01 exists");
        Assert.Null(revision);
        Assert.Fail("Chưa có đầu ra mã lỗi 0602-01 từ hàm XemNoiDungAsync theo Bảng 4.216.");
    });
    [Fact(DisplayName="UT-27b | XL2 | So sánh đúng hai trường thay đổi")]
    public Task UT27b_Test()=>UT262728.Check("UT-27b","XL2","So sánh đúng hai trường thay đổi","Giá 800000 -> 950000, mô tả cũ -> mới","khacBiet có GiaThamKhao và MoTa kèm giá trị cũ/mới",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        var r=await UT262728.AddRevision(db);
        var service=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        observe($"Price={service.Price}->{r.Price}; Description={service.Description}->{r.Description}; KhacBietOutput=MISSING");
        Assert.NotEqual(service.Price,r.Price);Assert.NotEqual(service.Description,r.Description);
        Assert.Fail("Mã Dev chưa xuất đối tượng khacBiet gồm 2 dòng GiaThamKhao và MoTa từ XemNoiDungAsync.");
    });
    [Fact(DisplayName="UT-27c | XL2 | Bản mới chưa có phiên bản công bố")]
    public Task UT27c_Test()=>UT262728.Check("UT-27c","XL2","Bản mới chưa có phiên bản công bố","PublishedRevisionNumber=NULL","khacBiet rỗng; nhãn Dịch vụ mới",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db,"Chờ duyệt",null);
        await UT262728.AddRevision(db,1);
        var service=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        observe($"PublishedRevisionNumber={service.PublishedRevisionNumber?.ToString()??"NULL"}; DetailView=DID_NOT_EXIST");
        Assert.Null(service.PublishedRevisionNumber);
        Assert.Fail("Chưa có đầu ra 'Dịch vụ mới' và danh sách khacBiet rỗng để kiểm tra XL2.");
    });
    [Fact(DisplayName="UT-27d | XL3 | Bản đã duyệt chỉ xem, không quyết định")]
    public Task UT27d_Test()=>UT262728.Check("UT-27d","XL3","Bản đã duyệt chỉ xem, không quyết định","Revision.Status=Đã duyệt","Hiển thị trạng thái; không có thao tác quyết định",async observe=>
    {
        await using var db=UT232425.Db();
        await UT262728.Init(db);
        var r=await UT262728.AddRevision(db,3,"Đã duyệt");
        observe($"RevisionStatus={r.Status}; DetailActions=NOT_RETURNED_BY_BACKEND");
        Assert.Equal("Đã duyệt",r.Status);
        Assert.Fail("Chưa có hàm XemNoiDungAsync trả danh sách quyền thao tác của trang chi tiết.");
    });
}
