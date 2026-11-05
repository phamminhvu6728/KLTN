using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TuTuc.Controllers;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Can lop UT232425 trong UT23_GhiKetQuaTinhHuong_Tests.cs
public sealed class UT24_MoBieuMauDichVu_Tests
{
    [Fact(DisplayName="UT-24a | KT1 | NCC cho xac minh")]
    public Task UT24a_Test()=>UT232425.Check("UT-24a","KT1","NCC cho xac minh","NCC02=ChoXacMinh;Edit(null)","Lỗi 0601-01; hướng tới hồ sơ xác minh",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var ctrl=UT232425.Provider(db,UT232425.NCC02);
        var action=await ctrl.Edit((int?)null);var message=ctrl.TempData["Message"]?.ToString()??"";
        observe($"Action={action.GetType().Name};Message={message};ErrorCode=KHONG CO");
        Assert.IsType<RedirectToActionResult>(action);Assert.Contains("xác minh",message,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0601-01",message);
    });
    [Fact(DisplayName="UT-24b | KT1 | NCC tam dinh chi")]
    public Task UT24b_Test()=>UT232425.Check("UT-24b","KT1","NCC tam dinh chi","NCC03=TamDinhChi;Edit(null)","Lỗi 0601-01; hướng tới gửi giải trình",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var ctrl=UT232425.Provider(db,UT232425.NCC03);
        var action=await ctrl.Edit((int?)null);var message=ctrl.TempData["Message"]?.ToString()??"";
        observe($"Action={action.GetType().Name};Message={message};ErrorCode=KHONG CO");
        Assert.IsType<RedirectToActionResult>(action);Assert.Contains("giải trình",message,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0601-01",message);
    });
    [Fact(DisplayName="UT-24c | KT2 | Dich vu cua NCC khac")]
    public Task UT24c_Test()=>UT232425.Check("UT-24c","KT2","Dich vu cua NCC khac","NCC01 mở DV03 thuộc NCC04","Lỗi 0601-02",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var ctrl=UT232425.Provider(db,UT232425.NCC01);
        var action=await ctrl.Edit((int?)UT232425.DV03);
        observe($"Action={action.GetType().Name};Status={(action as StatusCodeResult)?.StatusCode};ErrorCode=KHONG CO");
        Assert.IsType<NotFoundResult>(action);Assert.Fail("Thiếu mã lỗi nghiệp vụ 0601-02 ở nhánh Edit của dịch vụ thuộc NCC khác.");
    });
    [Fact(DisplayName="UT-24d | KT2 | Dich vu da duyet khong duoc sua truc tiep")]
    public Task UT24d_Test()=>UT232425.Check("UT-24d","KT2","Dich vu da duyet khong duoc sua truc tiep","NCC01 mở DV02=DaDuyet","Lỗi 0601-02; chỉ mở được bản Nháp",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var ctrl=UT232425.Provider(db,UT232425.NCC01);
        var action=await ctrl.Edit((int?)UT232425.DV02);
        observe($"Action={action.GetType().Name};IsDraft={ctrl.ViewBag.IsDraft};ErrorCode=KHONG CO");
        Assert.IsNotType<ViewResult>(action);Assert.Fail("Mở được biểu mẫu cho dịch vụ đã duyệt, khác bước KT2 theo kế hoạch.");
    });
    [Fact(DisplayName="UT-24e | XL1 | Mo bieu mau tao dich vu moi")]
    public Task UT24e_Test()=>UT232425.Check("UT-24e","XL1","Mo bieu mau tao dich vu moi","NCC01;Edit(null)","Biểu mẫu trống; danh mục loại; địa điểm DangHienThi=1",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var ctrl=UT232425.Provider(db,UT232425.NCC01);
        var action=await ctrl.Edit((int?)null);var view=Assert.IsType<ViewResult>(action);
        var model=Assert.IsType<ServiceEditVm>(view.Model);
        observe($"ModelId={model.Id};Name={model.Name};Category={model.Category};Places=KHONG CO DANH SACH;ErrorCode=KHONG CO");
        Assert.Equal(0,model.Id);Assert.True(string.IsNullOrWhiteSpace(model.Name));
        Assert.Fail("Chưa xác minh được danh mục loại và tập địa điểm DangHienThi=1 trong kết quả biểu mẫu.");
    });
}
