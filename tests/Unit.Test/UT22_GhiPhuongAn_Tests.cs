using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// Can UT2122 trong UT21_GhiNhanTinhHuong_Tests.cs.
public sealed class UT22_GhiPhuongAn_Tests
{
    [Fact(DisplayName="UT-22a | KT2 | Tinh huong da giai quyet")]
    public Task UT22a_AlreadyResolved()=>UT2122.Check("UT-22a","KT2","Không cho ghi phương án khi đã giải quyết","TH1 trạng thái DaGiaiQuyet","Lỗi 0501-04; không ghi thêm phương án",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);await UT2122.SeedIncident(db,"Đã giải quyết");
        var result=await new JourneyManager(db).Plan(UT2122.KH01,UT2122.PlanInput());
        var count=await db.IncidentPlans.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-04",result.Code);
    });
    [Fact(DisplayName="UT-22b | KT2 | Tinh huong dung xu ly")]
    public Task UT22b_Stopped()=>UT2122.Check("UT-22b","KT2","Không cho ghi phương án khi đã dừng","TH1 trạng thái DungXuLy","Lỗi 0501-04; không ghi thêm phương án",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);await UT2122.SeedIncident(db,"Dừng xử lý");
        var result=await new JourneyManager(db).Plan(UT2122.KH01,UT2122.PlanInput());
        var count=await db.IncidentPlans.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-04",result.Code);
    });
    [Fact(DisplayName="UT-22c | KT3 | RowVersion cu")]
    public Task UT22c_StaleVersion()=>UT2122.Check("UT-22c","KT3","Phiên bản tình huống không khớp","TH1 Version=[1..8]; gửi Version=[8..1]","Lỗi 0501-05; không ghi phương án",async observe=>
    {
        await using var db=UT2122.Db();await UT2122.Seed(db);await UT2122.SeedIncident(db);
        var wrong=Convert.ToBase64String(new byte[]{8,7,6,5,4,3,2,1});
        var result=await new JourneyManager(db).Plan(UT2122.KH01,UT2122.PlanInput(wrong));
        var count=await db.IncidentPlans.CountAsync();observe($"Success={result.Success}; Code={result.Code}; Message={result.Message}; Count={count}");
        Assert.False(result.Success);Assert.Equal(0,count);Assert.Equal("0501-05",result.Code);
    });
    [Fact(DisplayName="UT-22d | XL1-XL4 | Thay phuong an hien hanh")]
    public Task UT22d_ReplaceCurrent()=>UT2122.Check("UT-22d","XL1-XL4","Thay PA1 bằng PA2","TH1 có PA1 IsCurrent=true; ghi PA2","PA1 IsCurrent=false; PA2 IsCurrent=true; TH1 DaChonPhuongAn; lưu PA1 trước PA2",async observe=>
    {
        var audit=new UT2122.SaveObserver();await using var db=UT2122.Db(audit);await UT2122.Seed(db);await UT2122.SeedIncident(db,withCurrentPlan:true);audit.Snapshots.Clear();
        var result=await new JourneyManager(db).Plan(UT2122.KH01,UT2122.PlanInput());
        var plans=await db.IncidentPlans.AsNoTracking().OrderBy(x=>x.Id).ToListAsync();
        var inc=await db.Incidents.AsNoTracking().SingleAsync(x=>x.Id==UT2122.TH1);
        observe($"Success={result.Success}; Count={plans.Count}; Plans={string.Join(";",plans.Select(x=>$"{x.Content}:{x.IsCurrent}"))}; Status={inc.Status}; SaveOrder={string.Join(" | ",audit.Snapshots)}");
        Assert.True(result.Success);Assert.Equal(2,plans.Count);Assert.False(plans[0].IsCurrent);Assert.True(plans[1].IsCurrent);
        Assert.Equal("Đã chọn phương án",inc.Status);
        Assert.Equal(2,audit.Snapshots.Count);
        Assert.Contains("Added=0;OldSetFalse=1",audit.Snapshots[0]);
        Assert.Contains("Added=1",audit.Snapshots[1]);
    });
    [Fact(DisplayName="UT-22e | XL2-XL3 | Khong co phuong an cu")]
    public Task UT22e_FirstPlan()=>UT2122.Check("UT-22e","XL2-XL3","Tạo phương án đầu tiên","TH1 chưa có phương án","Một PA2 IsCurrent=true; đúng một lần SaveChanges",async observe=>
    {
        var audit=new UT2122.SaveObserver();await using var db=UT2122.Db(audit);
        await UT2122.Seed(db);await UT2122.SeedIncident(db);audit.Snapshots.Clear();
        var result=await new JourneyManager(db).Plan(UT2122.KH01,UT2122.PlanInput());
        var plans=await db.IncidentPlans.AsNoTracking().ToListAsync();
        observe($"Success={result.Success}; Count={plans.Count}; IsCurrent={plans.FirstOrDefault()?.IsCurrent}; SaveCount={audit.Snapshots.Count}; Snapshots={string.Join(" | ",audit.Snapshots)}");
        Assert.True(result.Success);var plan=Assert.Single(plans);Assert.True(plan.IsCurrent);
        Assert.Single(audit.Snapshots);
    });
}
