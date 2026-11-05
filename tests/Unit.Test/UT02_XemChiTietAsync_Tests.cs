using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TuTuc.Controllers;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
using Xunit;

namespace TuTuc.Tests.Unit;

// UT-02a..f - Bảng 4.175 (kế hoạch kiểm thử đơn vị).
// Dữ liệu/kỳ vọng KHÔNG điều chỉnh để phù hợp mã nguồn Dev.
// Các phép đo thực tế được ghi vào JSONL để run-tests.ps1 đọc tự động.
public sealed class UT02_XemChiTietAsync_Tests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT02_" + Guid.NewGuid().ToString("N")).Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    private static Account Provider(int id = 201) => new()
    {
        Id = id,
        Name = "NCC01",
        Email = $"ncc{id}@example.test",
        Role = "Provider",
        IsActive = true,
        VerificationStatus = "Đã xác minh",
        ProviderStatus = "Đang hoạt động",
        ProviderVerified = true
    };

    private static TravelService Service(int id, int ownerId = 201,
        string approval = "Đã duyệt", bool accepting = true) => new()
        {
            Id = id,
            OwnerId = ownerId,
            Name = "Phòng đôi view đồi",
            Location = "Đà Lạt",
            Category = "Lưu trú",
            Price = 800_000,
            ApprovalStatus = approval,
            IsPublished = true,
            IsAccepting = accepting
        };

    private static MarketplaceController Market(AppDbContext db, string? role = null)
    {
        // MarketplaceManager chỉ được dựng để khởi tạo Controller; Details không gọi nó.
        // Tạo qua DataProtection provider thực tế, không gọi CSDL/HTTP ngoài.
        var protection = new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider();
        var manager = new MarketplaceManager(db, protection);
        var controller = new MarketplaceController(db, manager);
        var http = new DefaultHttpContext();
        if (role != null)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, role == "Provider" ? "201" : "101"),
                new Claim(ClaimTypes.Role, role)
            }, "UT02");
            http.User = new ClaimsPrincipal(identity);
        }
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
        return controller;
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static string Describe(IActionResult result) => result switch
    {
        ViewResult view => $"ViewResult(View={view.ViewName ?? "(mac dinh)"}, Model={view.Model?.GetType().Name ?? "null"})",
        RedirectToActionResult redirect => $"RedirectToAction({redirect.ActionName})",
        ObjectResult obj => $"ObjectResult(HTTP={obj.StatusCode}, Value={obj.Value})",
        StatusCodeResult code => $"StatusCodeResult(HTTP={code.StatusCode})",
        _ => result.GetType().Name
    };

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
        catch (Exception exception)
        {
            problem = exception.Message;
            throw;
        }
        finally
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "UnitTest_Details.jsonl");
            var line = JsonSerializer.Serialize(new
            {
                id,
                title,
                step,
                input,
                expected,
                actual,
                problem = problem ?? ""
            });
            // UT-01 va UT-02 co the chay song song; retry ngan neu file dang bi ghi.
            for (int attempt = 0; attempt < 12; attempt++)
            {
                try
                {
                    lock (typeof(UT02_XemChiTietAsync_Tests))
                        File.AppendAllText(path, line + Environment.NewLine);
                    break;
                }
                catch (IOException) when (attempt < 11)
                {
                    await Task.Delay(25);
                }
            }
        }
    }

    [Fact(DisplayName = "UT-02a | KT1 | DiaDiem khong hien thi => 0101-03")]
    public Task UT02a_HiddenPlace() => Check(
        "UT-02a", "Dia diem khong hien thi", "KT1",
        "loai=DiaDiem; DD01; DangHienThi=0",
        "Tu choi xem chi tiet; ma loi 0101-03",
        async record =>
        {
            await using var db = NewDb();
            var place = new Place { Id = 101, Name = "Hồ Xuân Hương", Region = "Đà Lạt" };
            db.Places.Add(place);
            await db.SaveChangesAsync();
            var flag = typeof(Place).GetProperty("DangHienThi");
            if (flag == null)
            {
                record("Place khong co truong DangHienThi; khong thiet lap duoc dau vao DangHienThi=0 (chua the kiem tra hanh vi).");
                Assert.Fail("Thieu trang thai DangHienThi theo Bang 4.175; can bo sung moi co the thuc thi dung UT-02a.");
            }
            flag!.SetValue(place, flag.PropertyType == typeof(bool) ? false : 0);
            await db.SaveChangesAsync();
            IActionResult result = await new HomeController(db, new TravelSearchManager(db)).Place(101);
            record($"Ket qua thuc te: {Describe(result)}; ma loi 0101-03: khong quan sat duoc trong ket qua.");
            Assert.True(Describe(result).Contains("0101-03", StringComparison.Ordinal),
                "Trang thai khong hien thi phai bi tu choi voi ma 0101-03.");
        });

    [Fact(DisplayName = "UT-02b | KT1 | DichVu ChoDuyet => 0101-03")]
    public Task UT02b_PendingService() => Check(
        "UT-02b", "Dich vu cho duyet", "KT1",
        "loai=DichVu; DV01; TrangThaiCongBo=ChoDuyet",
        "Tu choi xem chi tiet; ma loi 0101-03",
        async record =>
        {
            await using var db = NewDb();
            db.Accounts.Add(Provider());
            db.Services.Add(Service(301, approval: "Chờ duyệt"));
            await db.SaveChangesAsync();
            var controller = Market(db);
            IActionResult result = await controller.Details(301, null);
            var message = controller.TempData["Message"]?.ToString() ?? "(khong co)";
            string code = controller.Response.Headers["X-Business-Error"].ToString();
            record($"Ket qua={Describe(result)}; HTTP={controller.Response.StatusCode}; X-Business-Error={code}; thongBao={message}");
            Assert.Equal("0101-03", code);
        });

    [Fact(DisplayName = "UT-02c | XL1 | DD01 chi kem hai dich vu DaDuyet")]
    public Task UT02c_PlaceWithApprovedServices() => Check(
        "UT-02c", "Dia diem kem dung 2 dich vu da duyet", "XL1",
        "DD01; co 2 dich vu DaDuyet va 1 dich vu ChoDuyet",
        "Tra dia diem kem dung 2 dich vu DaDuyet, khong kem ChoDuyet",
        async record =>
        {
            await using var db = NewDb();
            db.Places.Add(new Place { Id = 101, Name = "Hồ Xuân Hương", Region = "Đà Lạt" });
            db.Accounts.Add(Provider());
            db.Services.AddRange(
                Service(301), Service(302), Service(303, approval: "Chờ duyệt"));
            await db.SaveChangesAsync();
            var result = await new HomeController(db, new TravelSearchManager(db)).Place(101);
            var view = result as ViewResult;
            var attached = view?.ViewData["Services"] as System.Collections.IEnumerable;
            int? count = attached?.Cast<object>().Count();
            record($"Ket qua={Describe(result)}; soDichVuKemTheo={count?.ToString() ?? "KHONG CO"}; soDichVuTrongCSDL=3");
            Assert.NotNull(view);
            Assert.NotNull(attached);
            Assert.Equal(2, count);
        });

    [Fact(DisplayName = "UT-02d | XL3 | KH01 xem DV01 DangTiepNhan => coTheDat=true")]
    public Task UT02d_TravelerCanBook() => Check(
        "UT-02d", "Khach du lich co the dat dich vu", "XL3",
        "DV01 DaDuyet, DangTiepNhan=true; nguoi xem=KH01 (Traveler)",
        "coTheDat=true",
        async record =>
        {
            await using var db = NewDb();
            db.Accounts.Add(Provider());
            db.Services.Add(Service(301));
            await db.SaveChangesAsync();
            var controller = Market(db, "Traveler");
            var result = await controller.Details(301, null);
            object? flag = controller.ViewData["CanReceive"];
            record($"Ket qua={Describe(result)}; CanReceive={flag?.ToString() ?? "null"}; nguoiXem=Traveler");
            Assert.IsType<ViewResult>(result);
            Assert.Equal(true, flag);
        });

    [Fact(DisplayName = "UT-02e | XL3 | NCC01 hoac khach vang lai => coTheDat=false")]
    public Task UT02e_ProviderAndGuestCannotBook() => Check(
        "UT-02e", "NCC va khach chua dang nhap khong the dat", "XL3",
        "DV01 DangTiepNhan=true; nguoi xem=NCC01 va chua dang nhap",
        "coTheDat=false cho ca hai nguoi xem",
        async record =>
        {
            await using var db = NewDb();
            db.Accounts.Add(Provider());
            db.Services.Add(Service(301));
            await db.SaveChangesAsync();
            var ncc = Market(db, "Provider");
            var guest = Market(db);
            var resultNcc = await ncc.Details(301, null);
            var resultGuest = await guest.Details(301, null);
            object? nccFlag = ncc.ViewData["CanReceive"];
            object? guestFlag = guest.ViewData["CanReceive"];
            record($"NCC: {Describe(resultNcc)}, CanReceive={nccFlag ?? "null"}; " +
                   $"ChuaDangNhap: {Describe(resultGuest)}, CanReceive={guestFlag ?? "null"}");
            Assert.IsType<ViewResult>(resultNcc);
            Assert.IsType<ViewResult>(resultGuest);
            Assert.Equal(false, nccFlag);
            Assert.Equal(false, guestFlag);
        });

    [Fact(DisplayName = "UT-02f | XL3 | DV02 KhongTiepNhanMoi => false va nhan")]
    public Task UT02f_NotAcceptingNewRequests() => Check(
        "UT-02f", "Dich vu khong tiep nhan yeu cau moi", "XL3",
        "DV02 KhongTiepNhanMoi; nguoi xem=KH01 (Traveler)",
        "coTheDat=false; nhan 'Khong tiep nhan yeu cau moi'",
        async record =>
        {
            await using var db = NewDb();
            db.Accounts.Add(Provider());
            db.Services.Add(Service(302, accepting: false));
            await db.SaveChangesAsync();
            var controller = Market(db, "Traveler");
            var result = await controller.Details(302, null);
            object? flag = controller.ViewData["CanReceive"];
            var view = result as ViewResult;
            // XemChiTietAsync trong thiet ke phai tra nhan. Action Details hien
            // chi tra View va model; nhan neu render trong Razor View thi phai
            // kiem thu qua Integration/System Test. Khong ep phai co ViewData[StateLabel].
            var stateLabel = view?.ViewData["StateLabel"]?.ToString()
                          ?? view?.ViewData["StatusLabel"]?.ToString();
            record($"Ket qua={Describe(result)}; CanReceive={flag?.ToString() ?? "null"}; " +
                   $"nhanQuanSatDuocTaiController={stateLabel ?? "CHUA QUAN SAT DUOC (co the nam trong Razor View)"}");
            Assert.IsType<ViewResult>(result);
            Assert.Equal(false, flag);
            Assert.True(stateLabel == "Không tiếp nhận yêu cầu mới",
                "Chua xac minh duoc nhan 'Khong tiep nhan yeu cau moi' o muc Unit: " +
                "nhan co the duoc render trong Razor View. Khong danh dau PASS khi chua do duoc.");
        });
}
