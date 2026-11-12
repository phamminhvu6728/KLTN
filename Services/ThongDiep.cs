namespace TuTuc.Services;

// Danh mục mã lỗi nghiệp vụ, chép nguyên văn Bảng 4.250 (Chương 4, mục 4.9.23): mã, HTTP, thông điệp hiển thị.
// Mọi chỗ báo lỗi nghiệp vụ lấy thông điệp từ đây để thống nhất giữa các màn hình (thiết kế dùng ThongDiep.resx).
// Tệp được sinh từ tài liệu; sửa câu chữ ở tài liệu rồi sinh lại, không sửa tay.
public static class ThongDiep
{
    public sealed record Muc(int Http, string NoiDung);

    public static readonly IReadOnlyDictionary<string, Muc> DanhMuc = new Dictionary<string, Muc>
    {
        ["0101-01"] = new(400, "Tiêu chí tìm kiếm không hợp lệ"), // SUC01.01 traCuu
        ["0101-02"] = new(500, "Chưa thể tải dữ liệu, vui lòng thử lại"), // SUC01.01 traCuu
        ["0101-03"] = new(404, "Mục này không còn được công bố hoặc chưa thể truy cập"), // SUC01.01 xemChiTiet
        ["0201-01"] = new(400, "Thông tin kế hoạch chưa hợp lệ"), // SUC02.01 khoiTaoKeHoach
        ["0201-02"] = new(404, "Không tìm thấy kế hoạch"), // SUC02.01 xemKeHoach
        ["0201-03"] = new(422, "Ngày hoạt động nằm ngoài khoảng ngày của chuyến đi"), // SUC02.01 themHoatDong
        ["0201-04"] = new(422, "Mục đã chọn không còn được công bố"), // SUC02.01 themHoatDong
        ["0201-05"] = new(409, "Kế hoạch đã được thay đổi ở phiên khác, vui lòng tải lại"), // SUC02.01 themHoatDong
        ["0201-06"] = new(422, "Hoạt động đã gắn với tình huống phát sinh nên không thể bỏ"), // SUC02.01 xoaHoatDong
        ["0201-07"] = new(422, "Có hoạt động nằm ngoài khoảng ngày mới; hãy sửa hoạt động trước"), // SUC02.01 capNhatThongTin
        ["0301-01"] = new(400, "Khoảng ngày không hợp lệ"), // SUC03.01 timNhuCau
        ["0301-02"] = new(404, "Nhu cầu không còn hiển thị"), // SUC03.01 xemChiTiet
        ["0302-01"] = new(400, "Nội dung nhu cầu chưa hợp lệ"), // SUC03.02 congBo
        ["0401-01"] = new(422, "Nhu cầu không còn công bố hoặc người đăng đã ngừng sử dụng tài khoản"), // SUC04.01 moDeNghi
        ["0401-02"] = new(422, "Không thể gửi đề nghị cho nhu cầu của chính bạn"), // SUC04.01 moDeNghi
        ["0401-03"] = new(409, "Bạn đã có đề nghị đang chờ phản hồi cho nhu cầu này"), // SUC04.01 moDeNghi
        ["0401-04"] = new(409, "Hai bạn đã được kết nối cho nhu cầu này"), // SUC04.01 moDeNghi
        ["0401-05"] = new(404, "Không tìm thấy đề nghị"), // SUC04.01 xemDeNghi
        ["0401-06"] = new(403, "Bạn không được thực hiện thao tác này với đề nghị"), // SUC04.01 phanHoi
        ["0401-07"] = new(409, "Đề nghị đã được xử lý trước đó"), // SUC04.01 phanHoi
        ["0401-08"] = new(422, "Một bên đã ngừng sử dụng tài khoản, đề nghị không còn hiệu lực"), // SUC04.01 phanHoi
        ["0402-01"] = new(404, "Không tìm thấy kết nối"), // SUC04.02 moTaoThoaThuan
        ["0402-02"] = new(422, "Thỏa thuận đã kết thúc, không thể đề xuất nội dung mới"), // SUC04.02 moTaoThoaThuan
        ["0402-03"] = new(409, "Đang có đề nghị cùng ngừng chờ xác nhận, hãy phản hồi đề nghị đó trước"), // SUC04.02 moTaoThoaThuan
        ["0402-04"] = new(400, "Nội dung thỏa thuận chưa hợp lệ"), // SUC04.02 guiDeNghiThoaThuan
        ["0402-05"] = new(422, "Tài khoản của một bên không còn hoạt động"), // SUC04.02 guiDeNghiThoaThuan
        ["0402-06"] = new(409, "Thỏa thuận vừa có phiên bản mới, hãy xem phiên bản mới nhất"), // SUC04.02 guiDeNghiThoaThuan
        ["0402-07"] = new(422, "Nội dung đề xuất trùng với thỏa thuận đang hiệu lực"), // SUC04.02 guiDeNghiThoaThuan
        ["0402-08"] = new(403, "Bạn không thể tự xác nhận đề xuất của mình"), // SUC04.02 xacNhan
        ["0402-09"] = new(400, "Vui lòng nhập lý do từ chối"), // SUC04.02 xacNhan
        ["0402-10"] = new(409, "Không còn thỏa thuận đang hiệu lực để rút"), // SUC04.02 rut
        ["0402-11"] = new(409, "Đang có đề xuất chờ xác nhận, hãy xử lý đề xuất đó trước"), // SUC04.02 deNghiNgung
        ["0501-01"] = new(404, "Không tìm thấy kế hoạch hoặc tình huống"), // SUC05.01 danhSach
        ["0501-02"] = new(400, "Vui lòng chọn loại tình huống và nhập mô tả"), // SUC05.01 ghiNhanTinhHuong
        ["0501-03"] = new(400, "Hoạt động được chọn không thuộc kế hoạch này"), // SUC05.01 ghiNhanTinhHuong
        ["0501-04"] = new(422, "Tình huống đã kết thúc, không thể cập nhật"), // SUC05.01 ghiPhuongAn
        ["0501-05"] = new(409, "Tình huống vừa được cập nhật ở phiên khác"), // SUC05.01 ghiPhuongAn
        ["0501-06"] = new(400, "Vui lòng mô tả kết quả thực tế"), // SUC05.01 ghiKetQua
        ["0601-01"] = new(422, "Nhà cung cấp chưa đủ điều kiện gửi đăng dịch vụ"), // SUC06.01 moBieuMau
        ["0601-02"] = new(404, "Không tìm thấy dịch vụ"), // SUC06.01 moBieuMau
        ["0601-03"] = new(400, "Thời gian tạm ngừng không hợp lệ"), // SUC06.01 guiDang
        ["0601-04"] = new(400, "Thông tin dịch vụ chưa hợp lệ"), // SUC06.01 guiDang
        ["0601-05"] = new(409, "Dịch vụ đang có bản cập nhật chờ duyệt"), // SUC06.01 capNhatDichVu
        ["0601-06"] = new(422, "Không có thay đổi so với nội dung hiện tại"), // SUC06.01 capNhatDichVu
        ["0601-07"] = new(409, "Dịch vụ vừa thay đổi ở phiên khác, vui lòng tải lại"), // SUC06.01 capNhatDichVu
        ["0602-01"] = new(404, "Không tìm thấy nội dung"), // SUC06.02 xemNoiDung
        ["0602-02"] = new(400, "Vui lòng nhập lý do từ chối hoặc nội dung cần sửa"), // SUC06.02 duyet
        ["0602-03"] = new(409, "Phiên bản đã được xử lý hoặc nhà cung cấp đã gửi bản mới hơn"), // SUC06.02 duyet
        ["0602-04"] = new(422, "Nhà cung cấp không còn hoạt động, chỉ có thể từ chối hoặc yêu cầu sửa"), // SUC06.02 duyet
        ["0701-01"] = new(422, "Dịch vụ không còn được công bố, không thể gửi yêu cầu mới"), // SUC07.01 moDatDichVu
        ["0701-02"] = new(422, "Dịch vụ hiện không tiếp nhận yêu cầu mới"), // SUC07.01 moDatDichVu
        ["0701-03"] = new(400, "Điều kiện đặt chưa hợp lệ"), // SUC07.01 kiemTraTruocGui
        ["0701-04"] = new(404, "Không tìm thấy yêu cầu"), // SUC07.01 chiTiet
        ["0701-05"] = new(409, "Yêu cầu đã thay đổi (khách đã sửa, rút hoặc đã được xử lý)"), // SUC07.01 chapNhan
        ["0701-06"] = new(400, "Vui lòng nhập lý do từ chối"), // SUC07.01 chapNhan
        ["0701-07"] = new(422, "Điều kiện đề xuất trùng với yêu cầu hiện tại"), // SUC07.01 chapNhan
        ["0801-01"] = new(404, "Không tìm thấy yêu cầu"), // SUC08.01 moThayDoi
        ["0801-02"] = new(422, "Yêu cầu/thỏa thuận không còn ở trạng thái được sửa hoặc hủy"), // SUC08.01 moThayDoi
        ["0801-03"] = new(409, "Yêu cầu vừa thay đổi, vui lòng tải lại"), // SUC08.01 guiDeNghi
        ["0801-04"] = new(409, "Đã có một đề nghị đang chờ trên thỏa thuận này"), // SUC08.01 guiDeNghi
        ["0801-05"] = new(400, "Điều kiện mới chưa hợp lệ"), // SUC08.01 guiDeNghi
        ["0801-06"] = new(409, "Đề nghị hoặc thỏa thuận đã thay đổi"), // SUC08.01 phanHoi
        ["0801-07"] = new(400, "Vui lòng nhập lý do từ chối"), // SUC08.01 phanHoi
        ["0901-01"] = new(404, "Không tìm thấy thỏa thuận"), // SUC09.01 moGhiNhan
        ["0901-02"] = new(422, "Thỏa thuận không còn ở trạng thái Đã xác nhận"), // SUC09.01 moGhiNhan
        ["0901-03"] = new(422, "Chưa đến thời điểm phục vụ, chưa thể ghi nhận kết quả"), // SUC09.01 moGhiNhan
        ["0901-04"] = new(409, "Thỏa thuận vừa thay đổi, vui lòng tải lại"), // SUC09.01 luuKetQua
        ["0901-05"] = new(400, "Vui lòng chọn kết quả cho mọi hạng mục"), // SUC09.01 luuKetQua
        ["0901-06"] = new(422, "Kết quả đã ghi nhận không được ghi đè"), // SUC09.01 luuKetQua
        ["0901-07"] = new(422, "Hạng mục đã có phản ánh hoặc đã hết hạn phản ánh"), // SUC09.01 guiPhanAnh
        ["1001-01"] = new(400, "Loại tài khoản không hợp lệ"), // SUC10.01 chonVaiTro
        ["1001-02"] = new(400, "Thông tin đăng ký chưa hợp lệ"), // SUC10.01 dangKy
        ["1001-03"] = new(400, "Mật khẩu chưa đạt yêu cầu hoặc xác nhận không khớp"), // SUC10.01 dangKy
        ["1001-04"] = new(409, "Email hoặc số điện thoại đã được dùng cho tài khoản khác"), // SUC10.01 dangKy
        ["1001-05"] = new(400, "Tệp không hợp lệ (chỉ PDF, JPG, PNG, tối đa 5 MB/tệp, 5 tệp)"), // SUC10.01 dangKy
        ["1002-01"] = new(404, "Không tìm thấy hồ sơ"), // SUC10.02 xemHoSo
        ["1002-02"] = new(400, "Vui lòng nhập lý do"), // SUC10.02 xacMinh
        ["1002-03"] = new(409, "Hồ sơ đã được xử lý hoặc vừa thay đổi"), // SUC10.02 xacMinh
        ["1002-04"] = new(422, "Tài khoản nhà cung cấp đã ngừng sử dụng"), // SUC10.02 xacMinh
        ["1002-05"] = new(422, "Hồ sơ hiện không ở trạng thái cần bổ sung hoặc nộp lại"), // SUC10.02 nhanBoSung
        ["1101-01"] = new(400, "Vui lòng nhập đủ tên đăng nhập và mật khẩu"), // SUC11.01 dangNhap
        ["1101-02"] = new(400, "Thông tin đăng nhập không đúng"), // SUC11.01 dangNhap
        ["1101-03"] = new(422, "Tài khoản không còn hoạt động"), // SUC11.01 dangNhap
        ["1201-01"] = new(400, "Thông tin hồ sơ chưa hợp lệ"), // SUC12.01 luuThayDoi
        ["1201-02"] = new(409, "Số điện thoại đã được tài khoản khác sử dụng"), // SUC12.01 luuThayDoi
        ["1201-03"] = new(409, "Hồ sơ vừa được cập nhật ở phiên khác"), // SUC12.01 luuThayDoi
        ["1301-01"] = new(404, "Không tìm thấy nhà cung cấp"), // SUC13.01 xemChiTiet
        ["1301-02"] = new(400, "Vui lòng nhập đủ lý do và căn cứ"), // SUC13.01 tamDinhChi
        ["1301-03"] = new(409, "Trạng thái nhà cung cấp vừa thay đổi, vui lòng xem lại"), // SUC13.01 tamDinhChi
        ["1301-04"] = new(409, "Nhà cung cấp đã ngừng sử dụng tài khoản"), // SUC13.01 tamDinhChi
        ["1301-05"] = new(422, "Thao tác không phù hợp với trạng thái hiện hành của nhà cung cấp"), // SUC13.01 xemHeQua
        ["1301-06"] = new(409, "Giải trình đã được xem xét"), // SUC13.01 xemXetGiaiTrinh
        ["1301-07"] = new(422, "Chỉ gửi giải trình khi đang bị tạm đình chỉ hoặc thu hồi"), // SUC13.01 nhanGiaiTrinh
        ["1301-08"] = new(409, "Bạn đã có giải trình đang chờ xem xét"), // SUC13.01 nhanGiaiTrinh
        ["1401-01"] = new(400, "Xác thực lại không thành công"), // SUC14.01 xacThucLai
        ["1401-02"] = new(409, "Phiên xác nhận đã hết hạn, vui lòng xác thực lại"), // SUC14.01 ngungTaiKhoan
        ["1401-03"] = new(422, "Còn nghĩa vụ chưa xử lý, chưa thể ngừng tài khoản"), // SUC14.01 ngungTaiKhoan
        ["1401-04"] = new(500, "Không thể cập nhật đồng bộ, tài khoản vẫn hoạt động; vui lòng thử lại"), // SUC14.01 ngungTaiKhoan
    };

    // Thông điệp hiển thị kèm mã, ví dụ "Đề nghị đã được xử lý (0401-07)".
    public static string Loi(string ma) => $"{DanhMuc[ma].NoiDung} ({ma})";
    public static int Http(string ma) => DanhMuc[ma].Http;
    public static BusinessFault Fault(string ma) => new(ma, DanhMuc[ma].Http, DanhMuc[ma].NoiDung);
}
