using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public partial class Account
{
    public string VerificationStatus { get; set; } = "";
    public string? ProviderStatus { get; set; }
    public int? CurrentGovernanceDecisionId { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public partial class TravelService
{
    public bool IsPublished { get; set; } = true;
    public int? BlockedByDecisionId { get; set; }
}
