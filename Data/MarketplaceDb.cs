using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext
{
    public DbSet<ServiceRevision> ServiceRevisions => Set<ServiceRevision>();
    public DbSet<BookingChange> BookingChanges => Set<BookingChange>();
    public DbSet<BookingConditionVersion> BookingConditionVersions => Set<BookingConditionVersion>();
    public DbSet<BookingItemResult> BookingItemResults => Set<BookingItemResult>();
    public DbSet<BookingResultHistory> BookingResultHistories => Set<BookingResultHistory>();
}
