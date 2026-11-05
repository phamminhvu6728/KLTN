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
public sealed class UT25_GuiDangDichVu_Tests
{
    [Fact(DisplayName="UT-25a | KT1 | NCC chua du dieu kien dang")]
    public Task UT25a_Test()=>UT232425.Check("UT-25a","KT1","NCC chua du dieu kien dang","NCC02;GuiDang","Lỗi 0601-01; không thêm dịch vụ",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var before=await db.Services.CountAsync();
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC02,UT232425.Input());
        var after=await db.Services.CountAsync();
        observe($"Message={message};Count={before}->{after};ErrorCode=KHONG CO");
        Assert.Equal(before,after);Assert.Contains("0601-01",message);
    });
    [Fact(DisplayName="UT-25b | KT2 | Ten dich vu trong")]
    public Task UT25b_Test()=>UT232425.Check("UT-25b","KT2","Ten dich vu trong","NCC01;Name rỗng","Lỗi 0601-04; không thêm dịch vụ",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var input=UT232425.Input();input.Name="";var before=await db.Services.CountAsync();
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        var after=await db.Services.CountAsync();observe($"Message={message};Count={before}->{after}");
        Assert.Equal(before,after);Assert.Contains("0601-04",message);
    });
    [Fact(DisplayName="UT-25c | KT3 | Tam ngung thieu ly do")]
    public Task UT25c_Test()=>UT232425.Check("UT-25c","KT3","Tam ngung thieu ly do","ClosureStart=05/12;ClosureReason rỗng","Lỗi 0601-03",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var input=UT232425.Input();input.ClosureStart=new DateTime(2026,12,5);input.ClosureEnd=new DateTime(2026,12,8);
        input.ClosureScope="Toàn bộ";input.ClosureReason="";
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        observe($"Message={message};ErrorCode=KHONG CO");Assert.Contains("0601-03",message);
    });
    [Fact(DisplayName="UT-25d | KT3 | Tam ngung den ngay qua khu")]
    public Task UT25d_Test()=>UT232425.Check("UT-25d","KT3","Tam ngung den ngay qua khu","ClosureEnd=01/11/2026","Lỗi 0601-03",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var input=UT232425.Input();input.ClosureStart=new DateTime(2026,10,30);input.ClosureEnd=new DateTime(2026,11,1);
        input.ClosureScope="Toàn bộ";input.ClosureReason="Bảo trì";
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        observe($"Message={message};ErrorCode=KHONG CO");Assert.Contains("0601-03",message);
    });
    [Fact(DisplayName="UT-25e | XL1 | Luu nhap dich vu")]
    public Task UT25e_Test()=>UT232425.Check("UT-25e","XL1","Luu nhap dich vu","NCC01;Operation=SaveDraft","Dịch vụ Nhap,TiepNhan; không có BanCapNhatDichVu",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT232425.Input("SaveDraft"));
        var added=await db.Services.AsNoTracking().Where(x=>x.OwnerId==UT232425.NCC01&&x.Name=="Phòng tiêu chuẩn").ToListAsync();
        var revisions=await db.ServiceRevisions.AsNoTracking().Where(x=>added.Select(s=>s.Id).Contains(x.TravelServiceId)).ToListAsync();
        var s=added.SingleOrDefault();observe($"Message={message};Services={added.Count};Status={s?.ApprovalStatus};Accepting={s?.IsAccepting};Revisions={revisions.Count}");
        Assert.StartsWith("Đã lưu nháp",message);Assert.Single(added);Assert.Equal("Nháp",s!.ApprovalStatus);Assert.True(s.IsAccepting);Assert.Empty(revisions);
    });
    [Fact(DisplayName="UT-25f | XL2,XL3 | Gui dang dich vu moi")]
    public Task UT25f_Test()=>UT232425.Check("UT-25f","XL2,XL3","Gui dang dich vu moi","NCC01;Operation=Submit","Dịch vụ ChoDuyet; bản cập nhật v1 ChoDuyet cùng nội dung",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,UT232425.Input());
        var added=await db.Services.AsNoTracking().Where(x=>x.OwnerId==UT232425.NCC01&&x.Name=="Phòng tiêu chuẩn").ToListAsync();
        var s=added.SingleOrDefault();var revisions=await db.ServiceRevisions.AsNoTracking().Where(x=>x.TravelServiceId==s!.Id).ToListAsync();
        var rev=revisions.SingleOrDefault();observe($"Message={message};Services={added.Count};Status={s?.ApprovalStatus};Revisions={revisions.Count};Version={rev?.RevisionNumber};RevStatus={rev?.Status};RevName={rev?.Name}");
        Assert.StartsWith("Đã gửi",message);Assert.Single(added);Assert.Equal("Chờ duyệt",s!.ApprovalStatus);
        Assert.Single(revisions);Assert.Equal(1,rev!.RevisionNumber);Assert.Equal("Chờ duyệt",rev.Status);Assert.Equal(s.Name,rev.Name);Assert.Equal(s.Price,rev.Price);
    });
    [Fact(DisplayName="UT-25g | XL2 | Gui dang tu ban nhap da co")]
    public Task UT25g_Test()=>UT232425.Check("UT-25g","XL2","Gui dang tu ban nhap da co","DV01=Nhap; Operation=Submit; Id=DV01","Cập nhật DV01; không tạo dịch vụ thứ hai",async observe=>
    {
        await using var db=UT232425.Db();await UT232425.Seed(db);
        var input=UT232425.Input();input.Id=UT232425.DV01;input.Version=UT232425.Version;
        var before=await db.Services.CountAsync();
        var message=await UT232425.Market(db).SubmitService(UT232425.NCC01,input);
        var after=await db.Services.CountAsync();var item=await db.Services.AsNoTracking().SingleAsync(x=>x.Id==UT232425.DV01);
        var revisions=await db.ServiceRevisions.AsNoTracking().Where(x=>x.TravelServiceId==item.Id).ToListAsync();
        observe($"Message={message};Count={before}->{after};Id={item.Id};Status={item.ApprovalStatus};Revisions={revisions.Count}");
        Assert.StartsWith("Đã gửi",message);Assert.Equal(before,after);Assert.Equal("Chờ duyệt",item.ApprovalStatus);Assert.Single(revisions);
    });
}
