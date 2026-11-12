# Đối chiếu website với fulltext

Rà soát ngày 12/11/2026 trên nhánh `hoan-thien-sau-a14-v2`. Nguồn đối chiếu: `docs/requirements/SUC.txt` (18 SUC), `docs/requirements/Chapter4.txt` (thiết kế, 25 bảng, mã lỗi). Bằng chứng chạy: bộ test hệ thống, log ở `TestResults/chay-test-12-11-lan2.txt`.

## 1. Kết luận

- Đủ 18 SUC. Luồng chính của cả 18 SUC chạy được trên web.
- Các luồng thay thế (A), ngoại lệ (E) và quy tắc (BR) trong đặc tả: hầu hết có test chứng minh. Những mục chưa có test riêng đã được kiểm tra trực tiếp trong code (mục 2).
- Đủ 25 bảng nghiệp vụ. Code có thêm bảng phụ trợ (mục 3).
- Đã gỡ các chức năng ngoài fulltext; còn 7 SUC chưa gắn mã lỗi (mục 4).

## 2. Đối chiếu từng SUC

| SUC | Luồng A / E / BR | Kết quả | Bằng chứng |
|---|---|---|---|
| 01.01 Tra cứu | A1 lọc, A2 không có kết quả, E1 tiêu chí sai, E2 mục không còn công bố; BR1 chỉ hiện nội dung đã duyệt, BR2 thông tin tham khảo, BR3 dịch vụ tạm ngừng vẫn tra cứu được, BR4 không có bộ sưu tập, BR5 hiện "Không tiếp nhận yêu cầu mới" | Đạt | security-search 5–10; publication regression; chức năng bộ sưu tập (Library) đã tắt bằng `[NonController]` |
| 02.01 Lập kế hoạch | A1 lưu nháp, A2 mở kế hoạch, A3 cảnh báo không chặn; E1 thông tin sai, E2 hoạt động ngoài khoảng ngày, E3 lỗi lưu giữ nội dung; BR1–BR4 | Đạt | journey-community 8–16; planning-request-deep 5–29; design-alignment 1–6, 25 |
| 03.01 Tìm đồng hành | A1 không có kết quả (gợi ý mở rộng, công bố nhu cầu); E1 ngày sai, E2 nhu cầu không còn hiển thị; BR1 phạm vi chia sẻ, BR2 chưa tạo kết nối | Đạt | journey-community 33–37; trang trống có nút "Công bố nhu cầu" |
| 03.02 Công bố nhu cầu | A1 sửa trước khi công bố, A2 hủy; E1 dữ liệu sai; BR1 phạm vi chia sẻ, BR2 không tạo thỏa thuận | Đạt | journey-community 29–32, 51–53 |
| 04.01 Kết nối | A1 từ chối, A2 rút, A3 chờ phản hồi; E1 trùng đề nghị chờ, E2 nhu cầu/tài khoản không còn, E3 gửi cho chính mình, E4 đã kết nối, E5 tài khoản ngừng; BR1–BR3 | Đạt (đã sửa E1/E4) | journey-community 38–40, 54–55; `CommunityManager` kiểm tra E2, E3, E5 |
| 04.02 Thỏa thuận | A1 từ chối, A2 đề xuất khác, A3 sửa, A4 một bên rút, A5 cùng ngừng; E1, E2; BR1–BR4 | Đạt | journey-community 42–50, 56–57 |
| 05.01 Tình huống | A1 đổi phương án giữ lịch sử, A2 chưa thực hiện giữ "Đã chọn phương án", A3 xem lại; E1 thiếu mô tả, E2 nghiệp vụ liên quan thất bại; BR1 tên trạng thái | Đạt | journey-community 17–27 |
| 06.01 Quản lý dịch vụ | A1 cập nhật, A2 tạm ngừng, A3 sửa theo yêu cầu, A4 nháp; E1 thiếu thông tin, E2 chưa xác minh/đình chỉ, E3 ngày tạm ngừng sai; BR1–BR5 | Đạt | marketplace-suc; `MarketplaceManager.ValidateService` kiểm E3 |
| 06.02 Xét duyệt dịch vụ | A1 từ chối, A2 cần sửa, A3 danh sách trống; E1 thiếu lý do, E2 phiên bản đã đổi; BR1–BR4 | Đạt | marketplace-suc; `MarketplaceManager.Review` kiểm phiên bản |
| 07.01 Đặt dịch vụ | A1 từ chối, A2 đề xuất khác, A3 rút; E1 điều kiện sai, E2 cảnh báo tạm ngừng, E3 không còn công bố, E4 không tiếp nhận; BR1–BR5 | Đạt | planning-request-deep 30–47; design-alignment 7–33 |
| 08.01 Thay đổi/hủy | A1 hủy, A2 sửa/rút khi chưa xác nhận, A3 từ chối, A4 đề xuất khác; E1–E3; BR1–BR4 | Đạt | marketplace-suc; design-alignment 26–33 |
| 09.01 Ghi kết quả | A1 phản ánh, A2 chưa đủ căn cứ; E1 không có quyền, E2 chưa đến ngày phục vụ, E3 thiếu kết quả hạng mục, E4 thỏa thuận đã đổi; BR1–BR4 | Đạt | marketplace-suc; DesignAlignmentTests (tổng hợp kết quả) |
| 10.01 Đăng ký | A1 đăng ký nhà cung cấp chờ xác minh, A2 hủy; E1–E3; BR1–BR4 | Đạt | account-verification-suc |
| 10.02 Xác minh NCC | A1 bổ sung, A2 từ chối, A3 danh sách trống; E1 thiếu lý do, E2 phiên bản đã đổi; BR1–BR3 | Đạt | account-verification-suc |
| 11.01 Đăng nhập | A1 nhà cung cấp chưa xác minh vẫn đăng nhập, A2 quay lại chức năng; E1 sai thông tin, E2 tài khoản đã ngừng, E3 bỏ trống | Đạt (đã sửa E1/E2) | security-search 2, 12, 18–23 |
| 12.01 Hồ sơ | A1 hủy, A2 bổ sung hồ sơ xác minh; E1, E2 trùng định danh; BR1–BR3 | Đạt | account-verification-suc |
| 13.01 Quyền hoạt động NCC | A1 khôi phục, A2 thu hồi, A3 giải trình, A4 đến hạn nhắc Admin, A5 giữ nguyên; E1–E4; BR1–BR5 | Đạt | GovernanceChecks |
| 14.01 Ngừng tài khoản | A1 còn nghĩa vụ, A2 hủy; E1 xác thực lại sai, E2 không cập nhật đồng bộ; BR1–BR5 | Đạt | account-verification-suc |

### Đã sửa trong lần rà soát này

| Chỗ | Đặc tả | Trước | Sau |
|---|---|---|---|
| SUC11.01 E1/E2 | Sai thông tin báo chung "thông tin đăng nhập không đúng"; tài khoản đã ngừng báo "không còn hoạt động" | Gộp một câu cho cả hai | Tách 2 câu; chỉ báo "đã ngừng" khi mật khẩu đúng (`AccountManager.IsDeactivated`) |
| SUC04.01 E1/E4 | E1 "đã có đề nghị đang chờ", E4 "hai bạn đã được kết nối" (Chương 4, 0401-03/04) | Gộp một câu | Tách 2 câu đúng chữ Chương 4 |
| BR01.01-2 | Thông tin là tham khảo, không bảo đảm còn chỗ, giá cuối cùng | Chỉ ghi "Giá chỉ mang tính tham khảo" | Ghi đủ ở trang tra cứu và trang chi tiết dịch vụ |

## 3. Cơ sở dữ liệu

25 bảng ở Chương 4 (Bảng 4.119–4.143) đều có bảng tương ứng trong code:

| Thiết kế | Code | Thiết kế | Code |
|---|---|---|---|
| TaiKhoan | Accounts | KeHoachChuyenDi | Trips |
| HoSoNguoiDung | UserProfiles | HoatDongLichTrinh | Activities |
| NhaCungCapDichVu | ProviderProfiles | TinhHuongPhatSinh | Incidents |
| HoSoXacMinhNhaCungCap | VerificationDossiers | TinhHuongHoatDong | IncidentActivities |
| QuyetDinhXacMinh | VerificationDecisions | PhuongAnXuLy | IncidentPlans |
| TaiLieuXacMinh | PrivateDocuments | NhuCauDongHanh | Companions |
| QuyetDinhQuyenHoatDong | GovernanceDecisions | DeNghiKetNoiDongHanh | Connections |
| GiaiTrinhNhaCungCap | ProviderExplanations | ThoaThuanDongHanh | CompanionAgreements |
| TaiLieuGiaiTrinh | PrivateDocuments | YeuCauDatDichVu | Bookings |
| DiaDiemDuLich | Places | DeNghiDieuChinhDichVu | BookingChanges |
| DichVuDuLich | Services | KetQuaCungCapDichVu | BookingItemResults |
| BanCapNhatDichVu | ServiceRevisions | ThongBao | Notifications |
| | | NhatKyThaoTac | AuditEntries |

Khác thiết kế:

- Tên bảng, cột, route bằng tiếng Anh; khóa kiểu `int`.
- TaiLieuXacMinh và TaiLieuGiaiTrinh dùng chung bảng `PrivateDocuments`.
- Bảng phụ trợ không có trong thiết kế: `BookingConditionVersions`, `BookingResultHistories` (lưu lịch sử điều kiện và kết quả theo BR08.01-3, BR09.01), `DossierSubmissions` (các lần bổ sung hồ sơ), `GovernanceReminders` (nhắc Admin theo SUC13.01 A4), `DemoSeedMarkers` (chỉ dùng cho dữ liệu mẫu).
- `SavedReferenceLists`, `SavedReferenceItems` là bảng của chức năng bộ sưu tập đã tắt theo BR01.01-4; còn trong CSDL nhưng không có màn hình nào dùng.

## 4. Mã lỗi nghiệp vụ và thông báo

- Đủ 98 mã lỗi Bảng 4.250 trong `Services/ThongDiep.cs`, sinh tự động từ tài liệu, câu chữ và HTTP nguyên văn.
- Đã gắn mã cho: SUC02.01, 03.01, 03.02, 04.01, 04.02, 05.01 (kèm thứ tự kiểm tra đúng thiết kế), 06.01, 06.02, 07.01, 08.01, 09.01. Thông điệp hiển thị dạng "câu trong Bảng 4.250 (mã)". Header `X-Business-Error` mang mã ở module kế hoạch, đặt dịch vụ, đồng hành.
- Chưa gắn: SUC01.01, 10.01, 10.02, 11.01, 12.01, 13.01, 14.01 (vẫn chặn đúng và báo lý do, chưa kèm mã).
- Thông báo ghi đúng loại Bảng 4.249 (`Services/ThongBaoLoai.cs`), loại lưu ở trường Type và Title; giao diện hiển thị nhãn tiếng Việt. Đường dẫn thông báo theo thiết kế (ví dụ `/dong-hanh/de-nghi/xem-de-nghi/{id}`), `DesignLinksController` chuyển hướng sang trang thật.

## 5. Đã sửa cho đúng thiết kế

| Chỗ | Thiết kế | Trước | Sau |
|---|---|---|---|
| Chi phí yêu cầu | ChiPhiDuKien khách tự nhập, không bắt buộc (Bảng 4.139, 4.203) | Tự tính giá × số lượng | Ô nhập không bắt buộc; cột cho phép để trống (migration `AllowEstimatedCostNull`) |
| Sửa yêu cầu | Chỉ sửa trực tiếp khi Chờ phản hồi (Bảng 4.212) | Cho sửa cả khi Đề xuất khác | Chỉ Chờ phản hồi; Đề xuất khác chỉ rút hoặc chấp thuận |
| Mở form thỏa thuận | KT1–KT3 trả 0402-01/02/03 (4.197) | Luôn mở form | Chặn đúng mã (`CommunityManager.OpenAgreement`) |
| Gửi đề nghị kết nối | Thông báo DeNghiKetNoiMoi cho người nhận | Không thông báo | Có thông báo đúng loại |
| Mở trang kết nối | Thông báo cùng đường dẫn chuyển đã đọc | Không | Có |
| Ghi phương án | Chỉ lưu lần 1 khi có phương án cũ | Luôn lưu 2 lần | Đúng thiết kế |
| Trang Hỗ trợ tại điểm đến, mức độ phù hợp %, đánh giá dịch vụ | Không có trong fulltext | Có | Đã gỡ |

## 6. Kết quả kiểm thử (12/11/2026)

- Bộ test hệ thống và nghiệp vụ của nhóm: 132/132 đạt (`TestResults/chay-test-12-11-lan7.txt`). Hai bước test cũ kiểm sai thiết kế đã sửa theo Bảng 4.212 và 4.197.
- Bộ unit test của tester (`tester-tests/Unit.Test`): 161/252 đạt (trước khi sửa: 85/252). Phần còn lỗi chủ yếu không do code:
  - 53 ca viết `Assert.Fail` cố ý (ghi lỗi "chưa có hàm theo thiết kế"); tester cần đổi test sang gọi hàm mới, ví dụ `OpenAgreement` trả mã 0402-01/02/03.
  - Một số ca tester ghi chú chưa kiểm được bằng EF InMemory (lỗi SQL 2601, lỗi đồng thời).
  - Mâu thuẫn trong chính bộ test cần tester thống nhất: UT-26a và UT-25g chờ kết quả ngược nhau cho cùng lời gọi; UT-20f chờ loại `DeNghiNgung` nhưng UT17/18/19 dùng "Nội dung"/"Cùng ngừng"; UT-28g/i, UT-32i, UT-33c, UT-37b kiểm loại thông báo trong Title, còn UT-16i, UT-19 kiểm trong Type (code đã ghi cả hai).
  - Còn lại là các SUC chưa gắn mã (mục 4).

## 7. Không có trong fulltext và không làm

Chat, nhắn tin, đánh giá dịch vụ, thanh toán, bản đồ, đăng nhập bên thứ ba. Việc trao đổi sau kết nối theo Chương 4 là hiện thông tin liên hệ trong phạm vi chia sẻ; trao đổi với nhà cung cấp là chuỗi phản hồi, đề xuất điều kiện khác và xác nhận.
