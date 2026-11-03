using TuTuc.Models;
namespace TuTuc.Services;
public static class ProviderRules
{
    public static bool CanPublish(Account? provider) => provider is { IsActive:true, Role:"Provider", VerificationStatus:"Đã xác minh", ProviderStatus:"Đang hoạt động" };
    public static bool CanFulfillExisting(Account? provider) => provider is { IsActive:true, Role:"Provider" };
    public static bool IsPublic(TravelService service, Account provider) => service.ApprovalStatus == "Đã duyệt" && service.IsPublished && provider.ProviderStatus != "Đã thu hồi";
    public static bool CanReceiveNew(TravelService service, Account provider) => IsPublic(service, provider) && service.IsAccepting && CanPublish(provider);
}
