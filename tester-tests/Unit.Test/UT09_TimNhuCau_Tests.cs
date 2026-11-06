using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

/// <summary>Hỗ trợ chung cho UT-09 và UT-10; không phụ thuộc UT-05.</summary>
internal static class UT910
{
    internal const int KH01=101, KH02=102, NCC01=103, NC01=901;
    internal static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("UT910_"+Guid.NewGuid().ToString("N")).Options,new HttpContextAccessor());
    internal static async Task Seed(AppDbContext db)
    {
        db.Accounts.AddRange(
            new Account{Id=KH01,Name="KH01",Email="ut910-kh01@example.test",Role="Traveler",IsActive=true},
            new Account{Id=KH02,Name="KH02",Email="ut910-kh02@example.test",Role="Traveler",IsActive=true,Bio="Giới thiệu KH02"},
            new Account{Id=NCC01,Name="NCC01",Email="ut910-ncc01@example.test",Role="Provider",IsActive=true});
        await db.SaveChangesAsync();
    }
    internal static Companion Demand(int id=NC01,string scope="Connected",decimal budget=2000000,
        DateTime? start=null,DateTime? end=null,bool published=true) => new(){
        Id=id,OwnerId=KH02,Name="KH02",Destination="Đà Lạt",Description="Nội dung riêng",
        Date=start??new DateTime(2026,12,10),EndDate=end??new DateTime(2026,12,14),
        Budget=budget,People=2,TravelStyle="Tự túc",Wishes="Mong muốn riêng",
        SharingScope=scope,IsPublished=published,Version=new byte[8]};
    internal static async Task Report(string id,string step,string title,string input,string expected,
        Func<Action<string>,Task> run)
    {
        string actual="CHUA LAY DUOC KET QUA",problem="";
        try{await run(s=>actual=s);}catch(Exception ex){problem=ex.ToString();throw;}
        finally{
            var path=Path.Combine(AppContext.BaseDirectory,"TestResults","UnitTest_Details.jsonl");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var line=JsonSerializer.Serialize(new{id,step,title,input,expected,actual,problem});
            for(int attempt=0;attempt<100;attempt++){
                try{using var fs=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read);
                    using var writer=new StreamWriter(fs);await writer.WriteLineAsync(line);break;}
                catch(IOException) when(attempt<99){await Task.Delay(20);}
            }
        }
    }
}

/// <summary>UT-09, Bảng 4.188. Thực thi hiện tại: CommunityManager.Browse.</summary>
public sealed class UT09_TimNhuCau_Tests
{
    [Fact(DisplayName="UT-09a | KT1 | tuNgay > denNgay => 0301-01")]
    public Task UT09a_InvalidDates()=>UT910.Report("UT-09a","KT1","Khoảng tìm kiếm không hợp lệ",
        "tuNgay=11/12/2026; denNgay=04/12/2026","Lỗi 0301-01; không tìm kiếm",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch{
            From=new DateTime(2026,12,11),To=new DateTime(2026,12,4)});
        string? code=null;record($"ketQua={result.Total} nhu cau; code={(code??"KHONG TRA MA LOI")}");
        Assert.Equal("0301-01",code);
    });

    [Fact(DisplayName="UT-09b | XL1 | Khach vo danh va NCC01 chi xem cong khai")]
    public Task UT09b_NonTraveler()=>UT910.Report("UT-09b","XL1","Người xem không phải khách du lịch",
        "Hai lượt xem: chưa đăng nhập (0) và NCC01 (103)",
        "MaNguoiXem=null tại NguoiXemThayDuoc; chỉ dữ liệu công khai",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);
        db.Companions.AddRange(UT910.Demand(911,"Public"),UT910.Demand(912,"Connected"));await db.SaveChangesAsync();
        var manager=new CommunityManager(db);
        var anon=await manager.Browse(0,new CommunitySearch());
        var provider=await manager.Browse(UT910.NCC01,new CommunitySearch());
        var anonIds=anon.Demands.Select(x=>x.Id).ToArray();
        var providerIds=provider.Demands.Select(x=>x.Id).ToArray();
        record($"anonymous=[{string.Join(",",anonIds)}], isTraveler={anon.IsTraveler}; provider=[{string.Join(",",providerIds)}], isTraveler={provider.IsTraveler}; tham so noi bo: KHONG THE QUAN SAT");
        Assert.False(anon.IsTraveler);Assert.False(provider.IsTraveler);
        Assert.Equal(new[]{911},anonIds);Assert.Equal(new[]{911},providerIds);
        // Không thể đo trực tiếp tham số giao diện INhuCauDongHanh.NguoiXemThayDuoc nếu mã Dev không có giao diện đó.
    });

    [Fact(DisplayName="UT-09c | XL2 | Giao khoang ngay")]
    public Task UT09c_DateOverlap()=>UT910.Report("UT-09c","XL2","Tìm nhu cầu giao khoảng ngày",
        "Tìm 04-11/12; nhu cầu 01-05/12,12-15/12,20/11-03/12","Chỉ nhu cầu 01-05/12",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);
        db.Companions.AddRange(UT910.Demand(921,"Public",start:new DateTime(2026,12,1),end:new DateTime(2026,12,5)),
            UT910.Demand(922,"Public",start:new DateTime(2026,12,12),end:new DateTime(2026,12,15)),
            UT910.Demand(923,"Public",start:new DateTime(2026,11,20),end:new DateTime(2026,12,3)));
        await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch{
            From=new DateTime(2026,12,4),To=new DateTime(2026,12,11)});
        var ids=result.Demands.Select(x=>x.Id).ToArray();record($"ketQua=[{string.Join(",",ids)}]");
        Assert.Equal(new[]{921},ids);
    });

    [Fact(DisplayName="UT-09d | XL2 | Ngan sach NULL va 2 trieu hop le")]
    public Task UT09d_Budget()=>UT910.Report("UT-09d","XL2","Lọc ngân sách có NULL",
        "NganSachToiDa=3000000; ba nhu cầu NULL,2000000,4000000","Bao gồm NULL và 2000000; loại 4000000",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);
        // Model Companion.Budget là decimal không nullable; số 0 không tương đương NULL tại mức dữ liệu.
        var prop=db.Model.FindEntityType(typeof(Companion))?.FindProperty("Budget");
        if(prop?.IsNullable!=true){record($"Companion.Budget nullable={prop?.IsNullable}; không thể thiết lập giá trị NULL đúng điều kiện KT");
            Assert.Fail("UT-09d: Model Companion.Budget không hỗ trợ NULL. Không thể chứng minh nhánh ngân sách NULL bằng giá trị 0.");}
        db.Companions.AddRange(UT910.Demand(931,"Public",0),UT910.Demand(932,"Public",2000000),UT910.Demand(933,"Public",4000000));
        await db.SaveChangesAsync();var page=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch{MaxBudget=3000000});
        var ids=page.Demands.Select(x=>x.Id).ToArray();record($"ketQua=[{string.Join(",",ids)}]");
        Assert.Equal(new[]{931,932},ids);
    });

    [Fact(DisplayName="UT-09e | XL4 | KhiKetNoi chua ket noi an noi dung")]
    public Task UT09e_RedactBeforeConnected()=>UT910.Report("UT-09e","XL4","Ẩn thông tin nhu cầu KhiKetNoi",
        "KH01 xem NC01 phạm vi Connected; không có đề nghị Đã chấp nhận",
        "Chỉ DiemDen, khoảng ngày, HinhThucDi; NoiDung,NganSach,MongMuon=null",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch());
        var card=Assert.Single(result.Demands);
        record($"id={card.Id}; limited={card.Limited}; description={card.Description??"NULL"}; budget={card.Budget?.ToString()??"NULL"}; wishes={card.Wishes??"NULL"}; style={card.TravelStyle}");
        Assert.Equal(UT910.NC01,card.Id);Assert.True(card.Limited);Assert.Null(card.Description);Assert.Null(card.Budget);Assert.Null(card.Wishes);
        Assert.Equal("Đà Lạt",card.Destination);Assert.Equal("Tự túc",card.TravelStyle);
    });

    [Fact(DisplayName="UT-09f | XL4 | Ket noi DaChapNhan hien thi du thong tin")]
    public Task UT09f_AcceptedVisible()=>UT910.Report("UT-09f","XL4","Người đã kết nối thấy đầy đủ",
        "KH01 có đề nghị DaChapNhan tới NC01","NoiDung,NganSach,MongMuon đầy đủ; daKetNoi=true",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);db.Companions.Add(UT910.Demand());
        db.Connections.Add(new Connection{Id=941,CompanionId=UT910.NC01,SenderId=UT910.KH01,Status="Đã chấp nhận"});await db.SaveChangesAsync();
        var result=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch());var card=Assert.Single(result.Demands);
        record($"connected={card.Connected}; description={card.Description??"NULL"}; budget={card.Budget}; wishes={card.Wishes??"NULL"}");
        Assert.True(card.Connected);Assert.Equal("Nội dung riêng",card.Description);
        Assert.Equal(2000000m,card.Budget);Assert.Equal("Mong muốn riêng",card.Wishes);
    });

    [Fact(DisplayName="UT-09g | XL5 | Khong ket qua co thong diep va lien ket")]
    public Task UT09g_NoMatchesMessage()=>UT910.Report("UT-09g","XL5","Không tìm thấy nhu cầu",
        "Không có nhu cầu nào thỏa mãn","Thông điệp Chưa có nhu cầu phù hợp; kèm liên kết công bố nhu cầu",async record=>{
        await using var db=UT910.Db();await UT910.Seed(db);
        var result=await new CommunityManager(db).Browse(UT910.KH01,new CommunitySearch{Q="Không có kết quả"});
        // CommunityPage không chứa Message hoặc đường dẫn công bố nhu cầu.
        string? message=null,link=null;
        record($"total={result.Total}; message={message??"KHONG CO"}; link={link??"KHONG CO"}");
        Assert.Empty(result.Demands);
        Assert.Fail("UT-09g: CommunityManager.Browse chỉ trả dữ liệu. Thông điệp và liên kết được render tại Views/Community/Index.cshtml; chưa có harness kiểm thử Razor output, không được tính PASS từ service.");
    });
}
