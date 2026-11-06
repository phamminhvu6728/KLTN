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

// Dung chung cho UT17, UT18, UT19. Khong sua code Dev.
internal static class UT171819
{
    internal const int KH01 = 201, KH02 = 202, KH03 = 203, NC01 = 991, KN01 = 998;
    internal static readonly byte[] VersionBytes = { 1, 2, 3, 4, 5, 6, 7, 8 };
    internal static string Version => Convert.ToBase64String(VersionBytes);
    internal static DateTime Start => DateTime.Today.AddDays(30);

    internal static AppDbContext Db()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UT171819_" + Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options, new HttpContextAccessor());
    }

    internal static async Task Seed(AppDbContext db, string connectionStatus = "Đã chấp nhận", bool ownerActive = true, bool senderActive = true)
    {
        db.Accounts.AddRange(
            new Account { Id = KH01, Name = "KH01", Email = "ut17-kh01@example.test", Role = "Traveler", IsActive = senderActive },
            new Account { Id = KH02, Name = "KH02", Email = "ut17-kh02@example.test", Role = "Traveler", IsActive = ownerActive },
            new Account { Id = KH03, Name = "KH03", Email = "ut17-kh03@example.test", Role = "Traveler", IsActive = true });
        db.Companions.Add(new Companion { Id = NC01, OwnerId = KH02, Name = "KH02", Destination = "Đà Lạt", Description = "Tìm người đi cùng", Date = Start, EndDate = Start.AddDays(4), Budget = 2000000m, People = 2, TravelStyle = "Tự túc", SharingScope = "Public", IsPublished = true, Version = VersionBytes.ToArray() });
        db.Connections.Add(new Connection { Id = KN01, CompanionId = NC01, SenderId = KH01, Status = connectionStatus, Message = "Đi cùng", Version = VersionBytes.ToArray() });
        await db.SaveChangesAsync();
    }

    internal static CompanionAgreement Agreement(int id, int revision, string status = "Chờ xác nhận", string kind = "Nội dung", int proposer = KH01)
        => new CompanionAgreement { Id = id, ConnectionId = KN01, Revision = revision, ProposerId = proposer,
            Kind = kind, Status = status, Version = VersionBytes.ToArray(), Start = Start, End = Start.AddDays(4),
            Destination = "Đà Lạt", Budget = 2000000m, CostSharing = "Chia đều", Scope = "Đi cùng", Content = "Lịch trình chung" };

    internal static AgreementInput Input(int baseRevision = 0, int? pendingId = null, string? pendingVersion = null)
        => new AgreementInput { ConnectionId = KN01, BaseRevision = baseRevision, PendingId = pendingId,
            PendingVersion = pendingVersion, Start = Start, End = Start.AddDays(4), Destination = "Đà Lạt",
            Budget = 2500000m, CostSharing = "Mỗi người tự chi", Scope = "Đi cùng", Content = "Đi 4 ngày" };

    internal static async Task Check(string id, string step, string title, string input, string expected, Func<Action<string>, Task> body)
    {
        string actual = "Chưa nhận được kết quả", problem = "";
        try { await body(value => actual = value); }
        catch (Exception ex) { problem = ex.Message; throw; }
        finally
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "UnitTest_Details.jsonl");
            var line = JsonSerializer.Serialize(new { id, step, title, input, expected, actual, problem });
            for (int attempt = 0; attempt < 100; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteLineAsync(line);
                    break;
                }
                catch (IOException) when (attempt < 99) { await Task.Delay(20); }
            }
        }
    }
}

public sealed class UT17_MoTaoThoaThuan_Tests
{
    [Fact(DisplayName = "UT-17a | KT1 | Nguoi thu ba khong co quyen")]
    public Task UT17a_ThirdParty() => UT171819.Check("UT-17a", "KT1", "Người thứ ba mở thỏa thuận", "KH03 mở KN01", "Lỗi 0402-01", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH03, UT171819.KN01);
        observe($"EditorNull={result == null}; ErrorCode=KHONG CO");
        Assert.Null(result);
        Assert.Fail("Thiếu mã lỗi nghiệp vụ 0402-01 khi KH03 mở thỏa thuận.");
    });

    [Fact(DisplayName = "UT-17b | KT1 | Ket noi chua chap nhan")]
    public Task UT17b_PendingConnection() => UT171819.Check("UT-17b", "KT1", "Kết nối chưa chấp nhận", "KN01=ChoPhanHoi", "Lỗi 0402-01", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db, "Chờ phản hồi");
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH01, UT171819.KN01);
        observe($"EditorNull={result == null}; ErrorCode=KHONG CO");
        Assert.Null(result);
        Assert.Fail("Thiếu mã lỗi 0402-01 cho kết nối chưa chấp nhận.");
    });

    [Fact(DisplayName = "UT-17c | KT2 | Da co mot ben rut")]
    public Task UT17c_Withdrawn() => UT171819.Check("UT-17c", "KT2", "Thỏa thuận đã có một bên rút", "KN01 có bản Nội dung MotBenDaRut", "Lỗi 0402-02", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601, 1, "Một bên đã rút")); await db.SaveChangesAsync();
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH01, UT171819.KN01);
        observe($"EditorNull={result == null}; ErrorCode=KHONG CO");
        Assert.Null(result);
        Assert.Fail("Thiếu mã lỗi 0402-02 khi mở kết nối đã có một bên rút.");
    });

    [Fact(DisplayName = "UT-17d | KT3 | Co de nghi cung ngung cho xac nhan")]
    public Task UT17d_StopPending() => UT171819.Check("UT-17d", "KT3", "Có đề nghị cùng ngừng đang chờ", "Phiên bản CungNgung/ChoXacNhan", "Lỗi 0402-03", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        db.CompanionAgreements.Add(UT171819.Agreement(601, 1, "Đã xác nhận"));
        db.CompanionAgreements.Add(UT171819.Agreement(602, 2, "Chờ xác nhận", "Cùng ngừng")); await db.SaveChangesAsync();
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH01, UT171819.KN01);
        observe($"EditorNull={result == null}; PendingId={result?.Draft.PendingId}; ErrorCode=KHONG CO");
        Assert.Null(result);
        Assert.Fail("Thiếu mã lỗi 0402-03 khi đã có đề nghị cùng ngừng.");
    });

    [Fact(DisplayName = "UT-17e | XL1/XL2 | Chua co phien ban => lay thong tin nhu cau")]
    public Task UT17e_FirstDraft() => UT171819.Check("UT-17e", "XL1,XL2", "Khởi tạo biểu mẫu thỏa thuận", "KN01 hợp lệ, chưa có phiên bản", "Lấy điểm đến và ngày từ NC01; phienBanCoSo=0", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH01, UT171819.KN01);
        observe($"Null={result == null}; BaseRevision={result?.Draft.BaseRevision}; Destination={result?.Draft.Destination}; Start={result?.Draft.Start:yyyy-MM-dd}; End={result?.Draft.End:yyyy-MM-dd}");
        Assert.NotNull(result);
        Assert.Equal(0, result.Draft.BaseRevision);
        Assert.Equal("Đà Lạt", result.Draft.Destination);
        Assert.Equal(UT171819.Start.Date, result.Draft.Start.Date);
        Assert.Equal(UT171819.Start.AddDays(4).Date, result.Draft.End.Date);
    });

    [Fact(DisplayName = "UT-17f | XL1/XL2 | Phien ban moi nhat v3 cho xac nhan")]
    public Task UT17f_LatestPending() => UT171819.Check("UT-17f", "XL1,XL2", "Nạp dữ liệu từ phiên bản mới nhất", "v2=DaXacNhan; v3=ChoXacNhan", "Điền dữ liệu v3; phienBanCoSo=3", async observe =>
    {
        await using var db = UT171819.Db(); await UT171819.Seed(db);
        var old = UT171819.Agreement(601, 2, "Đã xác nhận");
        var newer = UT171819.Agreement(602, 3);
        newer.Destination = "Huế"; newer.Budget = 3000000m;
        db.CompanionAgreements.AddRange(old, newer); await db.SaveChangesAsync();
        var result = await new CommunityManager(db).AgreementEditor(UT171819.KH01, UT171819.KN01);
        observe($"Null={result == null}; BaseRevision={result?.Draft.BaseRevision}; PendingId={result?.Draft.PendingId}; Destination={result?.Draft.Destination}; Budget={result?.Draft.Budget}");
        Assert.NotNull(result);
        Assert.Equal(3, result.Draft.BaseRevision);
        Assert.Equal(602, result.Draft.PendingId);
        Assert.Equal("Huế", result.Draft.Destination);
        Assert.Equal(3000000m, result.Draft.Budget);
    });
}
