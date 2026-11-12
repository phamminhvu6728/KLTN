using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using TuTuc.Services;
using System.Security.Claims;

namespace TuTuc.Data;

public partial class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor http) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripActivity> Activities => Set<TripActivity>();
    public DbSet<Companion> Companions => Set<Companion>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<TravelService> Services => Set<TravelService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Account>().Property(x => x.Phone).HasMaxLength(20);
        b.Entity<Connection>().HasIndex(x => new { x.CompanionId, x.SenderId }).IsUnique().HasFilter("[Status] IN (N'Chờ phản hồi',N'Đã chấp nhận')");
        b.Entity<Booking>().HasIndex(x => x.OperationId).IsUnique();
        b.Entity<BookingConditionVersion>().HasIndex(x => new { x.BookingId, x.VersionNumber }).IsUnique();
        b.Entity<BookingItemResult>().HasIndex(x => new { x.BookingId, x.ItemKey }).IsUnique();
        b.Entity<ServiceRevision>().HasIndex(x => new { x.TravelServiceId, x.RevisionNumber }).IsUnique();
        b.Entity<Incident>().HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GovernanceDecision>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GovernanceDecision>().HasOne<Account>().WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<VerificationDossier>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<VerificationDecision>().HasOne<VerificationDossier>().WithMany().HasForeignKey(x => x.DossierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<VerificationDecision>().HasOne<Account>().WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PrivateDocument>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProviderProfile>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<UserProfile>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ServiceRevision>().HasIndex(x => x.TravelServiceId).IsUnique().HasFilter("[Status] = N'Chờ duyệt'");
        b.Entity<BookingChange>().HasIndex(x => x.BookingId).IsUnique().HasFilter("[Status] IN (N'Chờ phản hồi',N'Đề xuất khác')");
        b.Entity<Account>().HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] <> N''");
        b.Entity<VerificationDossier>().HasIndex(x => x.OwnerId).IsUnique().HasFilter("[Status] IN (N'Chờ xác minh',N'Cần bổ sung')");
        b.Entity<ProviderExplanation>().HasIndex(x => new { x.OwnerId, x.DecisionId }).IsUnique().HasFilter("[Status] = N'Chờ xem xét'");
        b.Entity<CompanionAgreement>().HasIndex(x => x.ConnectionId, "UX_CompanionAgreement_Pending").IsUnique().HasFilter("[Status] = N'Chờ xác nhận'");
        b.Entity<CompanionAgreement>().HasIndex(x => x.ConnectionId, "UX_CompanionAgreement_Active").IsUnique().HasFilter("[Status] = N'Đã xác nhận'");
        b.Entity<IncidentActivity>().HasIndex(x => new { x.IncidentId, x.ActivityId }).IsUnique();
        // Lớp chặn cuối: không bỏ hoạt động đã được tình huống tham chiếu.
        b.Entity<IncidentActivity>()
            .HasOne(x => x.Activity)
            .WithMany()
            .HasForeignKey(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<IncidentPlan>().HasIndex(x => x.IncidentId).IsUnique().HasFilter("[IsCurrent] = 1");
        b.Entity<Trip>()
            .HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Companion>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Booking>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TravelService>().HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<UserNotification>().HasIndex(x => new { x.OwnerId, x.CreatedAt });
        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties().Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(decimal)))
            {
                p.SetPrecision(18);
                p.SetScale(0);
            }
    }

    public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();

    // Luồng có người dùng: ghi nghiệp vụ và nhật ký trong cùng transaction.
    public override async Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        // 1. Xác định người thao tác và các entity đang chờ ghi.
        var actorClaim = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actorClaim, out var actor))
            return await base.SaveChangesAsync(token);
        var changes = ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(x => x.Entity is not UserNotification && x.Entity is not AuditEntry)
            .ToList();
        if (changes.Count == 0)
            return await base.SaveChangesAsync(token);
        var summaries = changes.Select(x => (
            Entry: x,
            Action: x.State == EntityState.Added
                ? "Tạo mới"
                : x.State == EntityState.Deleted ? "Xóa" : "Cập nhật",
            Before: x.Entity is Booking && x.State != EntityState.Added
                ? MarketplaceManager.DescribeSnapshot(MarketplaceManager.Snapshot((Booking)x.OriginalValues.ToObject()))
                : "",
            After: x.Entity is Booking booking
                ? MarketplaceManager.DescribeSnapshot(MarketplaceManager.Snapshot(booking))
                : ""
        )).ToList();

        // 2. Mở transaction nếu bên gọi chưa mở; chỉ commit transaction tự sở hữu.
        var ownsTransaction = Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction ? await Database.BeginTransactionAsync(token) : null;
        try
        {
            // 3. Ghi các thay đổi nghiệp vụ, gồm hoạt động và kế hoạch cha.
            var count = await base.SaveChangesAsync(token);

            // 4. Thêm nhật ký; snapshot trước/sau ở đây chỉ dành cho Booking.
            foreach (var change in summaries)
            {
                var entity = change.Entry.Entity;
                var type = entity.GetType().Name;
                var key = entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? "";
                AuditEntries.Add(new AuditEntry
                {
                    ActorId = actor,
                    Entity = type,
                    EntityId = key,
                    Action = change.Action,
                    EventCode = entity is Booking offer && offer.Status == "Đề xuất khác" ? "SK11" : "",
                    Before = change.Before.Length > 500 ? change.Before[..500] : change.Before,
                    After = change.After.Length > 500 ? change.After[..500] : change.After
                });

                #region Thông báo của dịch vụ và đồng hành

                if (entity is Booking booking)
                {
                    var providerId = await Services.Where(x => x.Id == booking.TravelServiceId).Select(x => x.OwnerId).SingleOrDefaultAsync(token);
                    var recipients = new[]
                    {
                        booking.OwnerId,
                        providerId
                    }.Where(x => x > 0 && x != actor).Distinct();
                    foreach (var recipient in recipients)
                        Notifications.Add(new UserNotification
                        {
                            OwnerId = recipient,
                            Title = "",
                            Description = string.IsNullOrEmpty(booking.SnapshotName)
                                ? "Yêu cầu dịch vụ vừa được cập nhật."
                                : booking.SnapshotName,
                            Type = booking.Status switch
                            {
                                "Chờ phản hồi" => change.Action == "Tạo mới" ? "YeuCauDatMoi" : "YeuCauDaSua",
                                "Đề xuất khác" => "YeuCauCoDeXuatKhac",
                                "Đã xác nhận" => "YeuCauDaXacNhan",
                                "Từ chối" => "YeuCauBiTuChoi",
                                "Không tiếp tục" => "YeuCauKhongTiepTuc",
                                _ => "YeuCauCapNhat"
                            },
                            Url = recipient == providerId
                                ? "/nha-cung-cap/yeu-cau/" + booking.Id
                                : "/yeu-cau/" + booking.Id
                        });
                    foreach (var n in Notifications.Local.Where(n => n.Title == "")) n.Title = n.Type;
                }

                #endregion
            }

            // 5. Ghi nhật ký/thông báo xong mới cam kết toàn bộ dữ liệu.
            await base.SaveChangesAsync(token);
            if (transaction != null)
                await transaction.CommitAsync(token);
            return count;
        }
        catch
        {
            // Bất kỳ bước ghi nào lỗi: hoàn tác transaction tự sở hữu.
            if (transaction != null)
                await transaction.RollbackAsync(token);
            throw;
        }
    }
}
