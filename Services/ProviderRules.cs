using TuTuc.Models;
namespace TuTuc.Services;
public static class ProviderRules
{
    public static bool CanPublish(Account? provider) => provider is { IsActive:true, Role:"Provider", VerificationStatus:"Đã xác minh", ProviderStatus:"Đang hoạt động" };
    // 0601-01 (E2 SUC06.01): thông điệp nêu trạng thái hiện tại và hướng dẫn tiếp theo.
    public static string PublishBlockedMessage(Account? provider) => ThongDiep.Loi("0601-01") + " " + (provider?.ProviderStatus switch
    {
        "Tạm đình chỉ" => "Quyền hoạt động đang tạm đình chỉ, hãy xem quyết định và gửi giải trình.",
        "Đã thu hồi" => "Quyền hoạt động đã bị thu hồi.",
        _ => provider?.VerificationStatus == "Đã xác minh" ? "Tài khoản chưa ở trạng thái hoạt động." : "Hồ sơ xác minh chưa được duyệt, hãy xem hồ sơ xác minh."
    });
    public static bool CanFulfillExisting(Account? provider) => provider is { IsActive:true, Role:"Provider" };
    public static bool IsPublic(TravelService service, Account provider) => service.ApprovalStatus == "Đã duyệt" && service.IsPublished && provider.ProviderStatus != "Đã thu hồi";
    public static bool CanReceiveNew(TravelService service, Account provider) => IsPublic(service, provider) && service.IsAccepting && CanPublish(provider);
}
