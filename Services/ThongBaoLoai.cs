using TuTuc.Data;
using TuTuc.Models;

namespace TuTuc.Services;

// Thông báo theo Bảng 4.142 (ThongBao: người nhận, loại, nội dung, đường dẫn) và Bảng 4.249 (danh mục loại).
// Loại thông báo được lưu ở cả Type và Title; giao diện hiển thị nhãn tiếng Việt theo loại (Nhan).
public static class ThongBaoLoai
{
    static readonly Dictionary<string, string> Nhans = new()
    {
        ["DeNghiKetNoiMoi"] = "Có đề nghị kết nối mới",
        ["DeNghiDuocChapNhan"] = "Đề nghị kết nối được chấp nhận",
        ["DeNghiBiTuChoi"] = "Đề nghị kết nối bị từ chối",
        ["DeNghiDaRut"] = "Đề nghị kết nối đã được rút",
        ["DeNghiHetHieuLuc"] = "Đề nghị kết nối hết hiệu lực",
        ["ThoaThuanChoXacNhan"] = "Thỏa thuận đồng hành chờ xác nhận",
        ["DeNghiCungNgung"] = "Đề nghị cùng ngừng đồng hành",
        ["ThoaThuanDaXacNhan"] = "Thỏa thuận đồng hành đã xác nhận",
        ["ThoaThuanDaNgung"] = "Thỏa thuận đồng hành đã ngừng",
        ["ThoaThuanBiTuChoi"] = "Đề xuất đồng hành bị từ chối",
        ["ThoaThuanMotBenRut"] = "Một bên đã rút khỏi thỏa thuận đồng hành",
        ["DichVuDaDuyet"] = "Dịch vụ đã được duyệt",
        ["DichVuBiTuChoi"] = "Dịch vụ bị từ chối",
        ["DichVuCanSua"] = "Dịch vụ cần sửa",
        ["YeuCauDatMoi"] = "Có yêu cầu đặt dịch vụ mới",
        ["YeuCauDaSua"] = "Khách đã sửa yêu cầu",
        ["YeuCauDaRut"] = "Khách đã rút yêu cầu",
        ["YeuCauDaXacNhan"] = "Yêu cầu đã được xác nhận",
        ["YeuCauBiTuChoi"] = "Yêu cầu bị từ chối",
        ["YeuCauCoDeXuatKhac"] = "Nhà cung cấp đề xuất điều kiện khác",
        ["YeuCauKhongTiepTuc"] = "Yêu cầu không tiếp tục",
        ["DeNghiDieuChinhMoi"] = "Có đề nghị thay đổi/hủy mới",
        ["DeNghiDieuChinhDuocChapNhan"] = "Đề nghị thay đổi/hủy được chấp nhận",
        ["DeNghiDieuChinhBiTuChoi"] = "Đề nghị thay đổi/hủy bị từ chối",
        ["DeNghiDieuChinhCoDeXuatKhac"] = "Nhà cung cấp đề xuất điều kiện thay đổi khác",
        ["KetQuaDaGhiNhan"] = "Đã ghi nhận kết quả dịch vụ",
        ["PhanAnhKetQua"] = "Khách phản ánh kết quả dịch vụ",
    };

    // Nhãn hiển thị; thông báo cũ chưa có loại thì giữ tiêu đề cũ.
    public static string Nhan(UserNotification n) => Nhans.TryGetValue(n.Type, out var label) ? label : n.Title;

    public static void Them(AppDbContext db, int nguoiNhan, string loai, string noiDung, string duongDan) =>
        db.Notifications.Add(new UserNotification { OwnerId = nguoiNhan, Type = loai, Title = loai, Description = noiDung, Url = duongDan });

    // Đường dẫn thiết kế (Bảng 4.249); DesignLinksController chuyển hướng sang trang thật.
    public static string YeuCauKhach(int id) => "/yeu-cau/" + id;
    public static string YeuCauNhaCungCap(int id) => "/nha-cung-cap/yeu-cau/" + id;
}
