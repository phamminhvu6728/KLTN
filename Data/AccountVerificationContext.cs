using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public partial class AppDbContext {
 public DbSet<DossierSubmission> DossierSubmissions=>Set<DossierSubmission>();
 public DbSet<UserProfile> UserProfiles=>Set<UserProfile>();public DbSet<ProviderProfile> ProviderProfiles=>Set<ProviderProfile>();public DbSet<VerificationDossier> VerificationDossiers=>Set<VerificationDossier>();public DbSet<VerificationDecision> VerificationDecisions=>Set<VerificationDecision>();public DbSet<PrivateDocument> PrivateDocuments=>Set<PrivateDocument>();
}

