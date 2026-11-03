using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public partial class TravelService
{
    public string ServingHours { get; set; } = "";
    public string Conditions { get; set; } = "";
    public DateTime? ClosureStart { get; set; }
    public DateTime? ClosureEnd { get; set; }
    public string ClosureReason { get; set; } = "";
    public string ClosureScope { get; set; } = "";
    public int? PublishedRevisionNumber { get; set; }
    [Timestamp] public byte[] Version { get; set; } = [];
}
public partial class Booking
{
    public DateTime? EndDate { get; set; }
    public int Guests { get; set; } = 1;
    public string ArrivalDeparture { get; set; } = "";
    public string Options { get; set; } = "";
    public int ConditionsVersion { get; set; } = 1;
    public int CustomerAcceptedVersion { get; set; } = 1;
    public int ProviderAcceptedVersion { get; set; }
    public string ProviderOfferReason { get; set; } = "";
    public string ResponseReason { get; set; } = "";
    public DateTime? RespondedAt { get; set; }
}
