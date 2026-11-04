using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuTuc.Controllers;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// UT-01a..i: test cases from unit test plan, table 4.174.
// Expected results are NOT modified to match developer implementation.
public sealed class UT01_TraCuuAsync_DesignTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    private static HomeController Controller(AppDbContext db) =>
        new(db, new TravelSearchManager(db));

    private static string Errors(HomeController controller) =>
        string.Join("; ", controller.ModelState.Values
            .SelectMany(state => state.Errors)
            .Select(error => error.ErrorMessage));

    // This wrapper is common to all UT-01 cases. Only records observed values;
    // xUnit assertions still decide PASS / FAIL.
    private static async Task Check(
        string id, string title, string step, string input, string expected,
        Func<Action<string>, Task> execute)
    {
        var actual = "Chua thu thap duoc ket qua thuc te";
        string? problem = null;
        try
        {
            await execute(value => actual = value);
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            throw;
        }
        finally
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "UnitTest_Details.jsonl");
            var record = JsonSerializer.Serialize(new
            {
                id,
                title,
                step,
                input,
                expected,
                actual,
                problem = problem ?? ""
                // IMPORTANT: this is a narrative only; final result comes from TRX.
            });
            lock (typeof(UT01_TraCuuAsync_DesignTests))
            {
                File.AppendAllText(path, record + Environment.NewLine);
            }
        }
    }

    [Fact(DisplayName = "UT-01a | KT1 | giaTu > giaDen => 0101-01, khong truy van")]
    public Task UT01a_InvalidPriceRange() => Check(
        "UT-01a", "Khoang gia khong hop le", "KT1",
        "giaTu=1000000; giaDen=500000",
        "Ma loi 0101-01; khong goi TimDiaDiem/TimDichVu",
        async record =>
        {
            await using var db = NewDb();
            var controller = Controller(db);
            var result = await controller.Search(new TravelSearchInput
            {
                MinPrice = 1_000_000m,
                MaxPrice = 500_000m
            });
            string errors = Errors(controller);
            record($"ModelState.IsValid={controller.ModelState.IsValid}; " +
                   $"ketQua={result.GetType().Name}; ma/thongBao={errors}; " +
                   "soLanGoiTimDiaDiem/TimDichVu=CHUA_DO (chua co mock/spy)");
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("0101-01", errors, StringComparison.Ordinal);
            // The plan also requires proving no search calls: cannot claim that
            // condition passes without injecting observable interfaces / spies.
        });

    [Fact(DisplayName = "UT-01b | KT2 | loai ngoai danh muc => 0101-01")]
    public Task UT01b_InvalidType() => Check(
        "UT-01b", "Loai ngoai danh muc", "KT2",
        "loai=TauBien", "Ma loi 0101-01",
        async record =>
        {
            await using var db = NewDb();
            var controller = Controller(db);
            var result = await controller.Search(new TravelSearchInput { Type = "TauBien" });
            var errors = Errors(controller);
            record($"ModelState.IsValid={controller.ModelState.IsValid}; " +
                   $"ketQua={result.GetType().Name}; ma/thongBao={errors}");
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("0101-01", errors, StringComparison.Ordinal);
        });

    [Fact(DisplayName = "UT-01c | XL1/XL2 | loai trong, Da Lat => ca hai nhom")]
    public Task UT01c_PlacesAndServices() => Check(
        "UT-01c", "Tim ca dia diem va dich vu", "XL1, XL2",
        "loai=trong; khuVuc=Da Lat; khong loc gia",
        "Co ca hai nhom DiaDiem va DichVu",
        async record =>
        {
            await using var db = NewDb();
            db.Places.Add(new Place { Id = 101, Name = "Hồ Xuân Hương", Region = "Đà Lạt" });
            db.Accounts.Add(new Account { Id = 201, Name = "NCC", ProviderStatus = "Đã xác minh" });
            db.Services.Add(new TravelService
            {
                Id = 301,
                OwnerId = 201,
                Name = "Phòng đôi view đồi",
                Location = "Đà Lạt",
                Category = "Lưu trú",
                Price = 800_000m,
                ApprovalStatus = "Đã duyệt",
                IsPublished = true
            });
            await db.SaveChangesAsync();
            var items = await new TravelSearchManager(db).Search(new TravelSearchInput { Region = "Đà Lạt" });
            record($"tong={items.Count}; diaDiem={items.Count(x => x.SourceType == "Place")}; " +
                   $"dichVu={items.Count(x => x.SourceType == "Service")}; " +
                   $"ten=[{string.Join(", ", items.Select(x => x.Name))}]");
            Assert.Contains(items, item => item.SourceType == "Place" && item.Name == "Hồ Xuân Hương");
            Assert.Contains(items, item => item.SourceType == "Service" && item.Name == "Phòng đôi view đồi");
        });

    [Fact(DisplayName = "UT-01d | XL1 | loc gia: nhom dia diem rong")]
    public Task UT01d_PlacesExcludedWhenPriceFilterApplied() => Check(
        "UT-01d", "Loc gia loai nhom dia diem", "XL1",
        "giaTu=100000; diaDiem co gia=200000",
        "Nhom dia diem rong khi loc gia",
        async record =>
        {
            await using var db = NewDb();
            db.Places.Add(new Place { Id = 102, Name = "Điểm du lịch", Region = "Đà Lạt", Price = 200_000m });
            await db.SaveChangesAsync();
            var items = await new TravelSearchManager(db).Search(new TravelSearchInput { MinPrice = 100_000m });
            record($"tong={items.Count}; soDiaDiem={items.Count(x => x.SourceType == "Place")}");
            Assert.DoesNotContain(items, item => item.SourceType == "Place");
        });

    [Fact(DisplayName = "UT-01e | XL2 | dich vu gia NULL phai bi loai")]
    public Task UT01e_NullPriceIsRepresentable() => Check(
        "UT-01e", "Loai dich vu gia NULL", "XL2",
        "loai=LuuTru; giaTu=100000; giaDen=1000000; co dich vu gia NULL",
        "Dich vu gia NULL bi loai khoi ket qua",
        record =>
        {
            var prop = typeof(TravelService).GetProperty(nameof(TravelService.Price));
            record($"kieu TravelService.Price={prop?.PropertyType.Name ?? "KHONG_CO"}; " +
                   "chua the tao du lieu gia NULL neu thuoc tinh khong nullable");
            Assert.NotNull(prop);
            Assert.Equal(typeof(decimal?), prop!.PropertyType);
            // This only verifies testability; filtering behavior requires a further test.
            return Task.CompletedTask;
        });

    [Fact(DisplayName = "UT-01f | XL3 | KhongTiepNhanMoi va TamNgung")]
    public Task UT01f_ServiceMustExposeBothFlags() => Check(
        "UT-01f", "Hai trang thai dich vu", "XL3",
        "1 dich vu KhongTiepNhanMoi; 1 dich vu TamNgung",
        "Tra dung hai co KhongTiepNhan=true va TamNgung=true",
        record =>
        {
            var first = typeof(TravelSearchItem).GetProperty("KhongTiepNhan");
            var second = typeof(TravelSearchItem).GetProperty("TamNgung");
            record($"coKhongTiepNhan={first != null}; coTamNgung={second != null}; " +
                   "chua kiem duoc gia tri thuc thi neu thieu thuoc tinh");
            Assert.NotNull(first);
            Assert.NotNull(second);
            return Task.CompletedTask;
        });

    [Fact(DisplayName = "UT-01g | XL3 bien | 04-10/11, ngay 03/11 chua tam ngung")]
    public Task UT01g_DateBoundariesAreTestable() => Check(
        "UT-01g", "Bien ngay tam ngung", "XL3 (bien)",
        "tamNgungTu=04/11/2026; tamNgungDen=10/11/2026; homNay=03/11/2026",
        "TamNgung=false vi chua toi ngay bat dau",
        record =>
        {
            var from = typeof(TravelService).GetProperty("TamNgungTu");
            var to = typeof(TravelService).GetProperty("TamNgungDen");
            record($"coTamNgungTu={from != null}; coTamNgungDen={to != null}; " +
                   "chua kiem duoc gia tri thuc thi neu thieu thuoc tinh/dong ho");
            Assert.NotNull(from);
            Assert.NotNull(to);
            return Task.CompletedTask;
        });

    [Fact(DisplayName = "UT-01h | XL4 | 25 dich vu, trang 2 => 5 phan tu")]
    public Task UT01h_SecondPageHasFiveItems() => Check(
        "UT-01h", "Phan trang 25 dich vu", "XL4",
        "25 dich vu; trang=2; kichThuocTrang=20",
        "Trang 2 co 5 phan tu; sap xep theo ten, roi ma",
        async record =>
        {
            await using var db = NewDb();
            db.Accounts.Add(new Account { Id = 202, Name = "NCC", ProviderStatus = "Đã xác minh" });
            for (int i = 1; i <= 25; i++)
                db.Services.Add(new TravelService
                {
                    Id = 400 + i,
                    OwnerId = 202,
                    Name = $"Dịch vụ {i:D2}",
                    Category = "Lưu trú",
                    Location = "Đà Lạt",
                    Price = 300_000m,
                    ApprovalStatus = "Đã duyệt",
                    IsPublished = true
                });
            await db.SaveChangesAsync();
            var property = typeof(TravelSearchInput).GetProperty("Trang")
                ?? typeof(TravelSearchInput).GetProperty("Page");
            if (property == null)
            {
                record("khong co Trang/Page trong TravelSearchInput; chua goi duoc phan trang cua SUT");
                Assert.Fail("Thieu tham so trang cua bang 4.174; khong duoc Skip/Take ben ngoai SUT roi bao PASS.");
            }
            var input = new TravelSearchInput { Type = "Lưu trú" };
            property!.SetValue(input, 2);
            var items = await new TravelSearchManager(db).Search(input);
            record($"soKetQuaTrang2={items.Count}; ten=[{string.Join(", ", items.Select(x => x.Name))}]");
            Assert.Equal(5, items.Count);
            Assert.Equal(items.OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => x.Id), items.Select(x => x.Id));
        });

    [Fact(DisplayName = "UT-01i | XL5 | hai nhom rong => thong bao, giu tieu chi")]
    public Task UT01i_NoResultsMessageAndCriteria() => Check(
        "UT-01i", "Khong co ket qua tim kiem", "XL5",
        "khuVuc=Khong co du lieu; ca hai nhom rong",
        "Thong diep 'Chua tim thay thong tin phu hop'; giu tieu chi da nhap",
        async record =>
        {
            await using var db = NewDb();
            var input = new TravelSearchInput { Region = "Không có dữ liệu" };
            var controller = Controller(db);
            var view = Assert.IsType<ViewResult>(await controller.Search(input));
            var vm = Assert.IsType<TravelSearchVm>(view.Model);
            var messageProperty = typeof(TravelSearchVm).GetProperty("ThongDiep")
                ?? typeof(TravelSearchVm).GetProperty("Message");
            var message = messageProperty?.GetValue(vm)?.ToString();
            record($"soKetQua={vm.Items.Count}; khuVucGiuLai={vm.Input.Region}; " +
                   $"thongDiep={message ?? "KHONG_CO"}");
            Assert.Empty(vm.Items);
            Assert.Equal(input.Region, vm.Input.Region);
            Assert.NotNull(messageProperty);
            Assert.Equal("Chưa tìm thấy thông tin phù hợp", message);
        });
}
