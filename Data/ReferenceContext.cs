using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext {
 public DbSet<SavedReferenceList> SavedReferenceLists=>Set<SavedReferenceList>();
 public DbSet<SavedReferenceItem> SavedReferenceItems=>Set<SavedReferenceItem>();
}
