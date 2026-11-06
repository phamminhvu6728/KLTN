using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuTuc.Data;
using TuTuc.Models;
using Xunit;

namespace Integration.Test;

public class IT04_DangKyNhaCungCap_GiaoDich_Tests
{
    [Fact]
    public async Task IT_04_DangKyNCC_GhiDongBoVaRollback()
    {
        const string id = "IT-04";
        const string title = "GD-12 | Dang ky NCC - ghi dong bo va rollback";
        const string pre = "Chua dang nhap; ncc9@test.vn va ncc10@test.vn chua ton tai";
        const string input = "Dang ky ncc9 voi 2 PDF; dang ky ncc10, gay loi SQL sau khi da luu tep";
        const string expected = "Lan 1: HTTP 302, 5 nhom bang+nhat ky day du, thong bao dung dac ta; Lan 2: khong co du lieu, tep duoc don";
        var facts = new List<string>(); var errors = new List<string>();
        void Check(bool ok, string name, string observed)
        {
            facts.Add($"{name}={observed}");
            if (!ok) errors.Add($"{name}: {observed}");
        }
        try
        {
            using var factory = new IntegrationTestFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
            var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
            var upload = Path.Combine(root, "TestResults", factory.DatabaseName, "UploadFiles");
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Check(!await db.Accounts.AnyAsync(x => x.Email == "ncc9@test.vn" || x.Email == "ncc10@test.vn"),
                    "Tien DK email moi", "Kiem tra email ncc9 va ncc10");
            }
            using var page = await client.GetAsync("/Account/Register");
            Check(page.StatusCode == HttpStatusCode.OK, "Mo trang dang ky", $"HTTP {(int)page.StatusCode}");
            var token = Token(await page.Content.ReadAsStringAsync());
            using var form1 = Form("ncc9@test.vn", "0988888889", token);
            var first = await client.PostAsync("/Account/Register", form1);
            Check((int)first.StatusCode == 302, "Lan 1 HTTP", $"HTTP {(int)first.StatusCode}");
            Check((first.Headers.Location?.ToString() ?? "").Contains("Login", StringComparison.OrdinalIgnoreCase), "Lan 1 chuyen trang", first.Headers.Location?.ToString() ?? "Khong co");
            var next = await client.GetAsync(first.Headers.Location ?? new Uri("/Account/Login", UriKind.Relative));
            var message = await next.Content.ReadAsStringAsync();
            Check(message.Contains("Bạn chỉ được đăng dịch vụ sau khi Admin xác minh hồ sơ", StringComparison.OrdinalIgnoreCase),
                "Thong bao theo dac ta", message.Contains("Hồ sơ đang chờ xác minh", StringComparison.OrdinalIgnoreCase) ? "Ung dung bao: Ho so dang cho xac minh (khac dac ta)" : "Khong tim thay thong bao dac ta");
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Email == "ncc9@test.vn");
                Check(account != null, "TaiKhoan NCC", account == null ? "Khong co" : $"Id={account.Id}; Role={account.Role}");
                if (account != null)
                {
                    Check(account.Role == "Provider" && account.PasswordHash != "Ncc@12345" && account.PasswordHash.Length > 10,
                        "Vai tro va mat khau bam", $"Role={account.Role}; Hash={account.PasswordHash.Length} ky tu");
                    Check(account.VerificationStatus == "Chờ xác minh" && account.ProviderStatus == null,
                        "Quyen NCC", $"Xac minh={account.VerificationStatus}; Quyen={account.ProviderStatus ?? "NULL"}");
                    var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerId == account.Id);
                    Check(profile != null && profile.SharingScope == "Private", "HoSoNguoiDung", profile == null ? "Khong co" : profile.SharingScope);
                    var provider = await db.ProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerId == account.Id);
                    Check(provider?.CompanyName == "Nhà hàng Phố Núi", "NhaCungCap", provider?.CompanyName ?? "Khong co");
                    var dossier = await db.VerificationDossiers.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerId == account.Id);
                    Check(dossier != null && dossier.Status == "Chờ xác minh" && dossier.Submission == 0,
                        "HoSoXacMinh", dossier == null ? "Khong co" : $"{dossier.Status}; Lan={dossier.Submission}");
                    var documents = await db.PrivateDocuments.AsNoTracking().Where(x => x.OwnerId == account.Id && x.Scope == "Verification").ToListAsync();
                    Check(documents.Count == 2 && documents.All(x => x.Submission == 0), "Hai TaiLieuXacMinh", $"So tep={documents.Count}");
                    Check(documents.All(x => File.Exists(Path.Combine(upload, x.StorageKey))), "Hai file PDF tren dia", "Dem=" + documents.Count(x => File.Exists(Path.Combine(upload, x.StorageKey))));
                    var audit = await db.AuditEntries.AsNoTracking().Where(x => x.ActorId == account.Id).ToListAsync();
                    Check(audit.Any(x => x.EventCode == "SK03"), "Nhat ky SK03", string.Join(",", audit.Select(x => x.EventCode + ":" + x.Action)));
                }
            }
            // Tao trigger tren DB KIEM THU RIENG de buoc SaveChanges bi loi khi chen NCC10.
            // Co trigger, EF Core SQL Server co the tu choi UPDATE/INSERT qua OUTPUT; van phai kiem chung khong de lai du lieu/tep.
            var triggerCreated = false;
            try
            {
                using (var scope = factory.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.ExecuteSqlRawAsync("""
                        CREATE OR ALTER TRIGGER [TR_IT04_FailProvider] ON [ProviderProfiles]
                        AFTER INSERT AS
                        BEGIN
                            SET NOCOUNT ON;
                            THROW 51004, 'IT04 simulated provider registration SQL failure', 1;
                        END
                        """);
                    triggerCreated = true;
                }
                var registerAgain = await client.GetStringAsync("/Account/Register");
                using var form2 = Form("ncc10@test.vn", "0988888890", Token(registerAgain));
                var second = await client.PostAsync("/Account/Register", form2);
                // Controller tra View(200) khi dang ky that bai, khong duoc chuyen Login.
                Check((int)second.StatusCode == 200, "Lan 2 HTTP that bai", $"HTTP {(int)second.StatusCode}");
                using var scope2 = factory.Services.CreateScope();
                var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                var account10 = await db2.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Email == "ncc10@test.vn");
                Check(account10 == null, "Rollback TaiKhoan", account10 == null ? "Khong co" : "Con tai khoan");
                if (account10 != null)
                {
                    Check(!await db2.UserProfiles.AnyAsync(x => x.OwnerId == account10.Id), "Rollback HoSoNguoiDung", "Da kiem tra");
                    Check(!await db2.ProviderProfiles.AnyAsync(x => x.OwnerId == account10.Id), "Rollback NhaCungCap", "Da kiem tra");
                    Check(!await db2.VerificationDossiers.AnyAsync(x => x.OwnerId == account10.Id), "Rollback HoSoXacMinh", "Da kiem tra");
                    Check(!await db2.PrivateDocuments.AnyAsync(x => x.OwnerId == account10.Id), "Rollback TaiLieu", "Da kiem tra");
                }
                var stored = await db2.PrivateDocuments.AsNoTracking().Where(x => x.Scope == "Verification").Select(x => x.StorageKey).ToListAsync();
                var orphanFiles = Directory.Exists(upload) ? Directory.GetFiles(upload).Where(x => !stored.Contains(Path.GetFileName(x))).ToArray() : [];
                Check(orphanFiles.Length == 0, "Don 2 PDF sau loi", $"Tep mo coi={orphanFiles.Length}");
            }
            finally
            {
                if (triggerCreated)
                {
                    using var scope = factory.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS [TR_IT04_FailProvider]");
                }
            }
        }
        catch (Exception ex) { errors.Add("Ngoai le: " + ex.GetType().Name + " - " + ex.Message); }
        finally { IntegrationTestReporter.Save(id, title, pre, input, expected, string.Join("; ", facts), string.Join(" | ", errors)); }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    private static MultipartFormDataContent Form(string email, string phone, string token)
    {
        var form = new MultipartFormDataContent();
        var fields = new Dictionary<string, string>
        {
            ["Role"] = "Provider", ["Name"] = "Trần Văn Bình", ["Email"] = email, ["Phone"] = phone,
            ["Password"] = "Ncc@12345", ["ConfirmPassword"] = "Ncc@12345",
            ["CompanyName"] = "Nhà hàng Phố Núi", ["CompanyType"] = "Nhà hàng", ["ContactName"] = "Trần Văn Bình",
            ["Address"] = "12 Bùi Thị Xuân, Đà Lạt", ["ContactPhone"] = "0263888999", ["ContactEmail"] = "lienhe@phonui.vn",
            ["DossierContent"] = "Kinh doanh ăn uống", ["Representative"] = "Trần Văn Bình",
            ["__RequestVerificationToken"] = token
        };
        foreach (var (key, value) in fields) form.Add(new StringContent(value, Encoding.UTF8), key);
        for (var i = 1; i <= 2; i++)
        {
            var pdf = new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF"));
            pdf.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            form.Add(pdf, "Files", $"xac-minh-{i}.pdf");
        }
        return form;
    }
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"");
        if (!match.Success) throw new InvalidOperationException("Khong tim thay Antiforgery token");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
