# Đối chiếu sản phẩm với thiết kế xử lý chi tiết

Nguồn: `C:\Users\bakav\Downloads\thiết kế chi tiết.docx`, mục 4.9. Bản trích đọc nằm tại `docs/requirements/THIET_KE_CHI_TIET.txt` và JSON cùng tên. File gốc được giữ nguyên. Bản này tập trung các khác biệt ảnh hưởng demo sâu SUC02.01 và SUC07.01, cùng các phần liên quan; không phải biên bản xác nhận mọi action đã đạt.

Giao diện hiện tại được giữ. Các sửa đổi là xử lý, trường tùy chọn, điều hướng theo kết quả và bộ lọc/phân trang theo hợp đồng. Dùng Ctrl+F số bảng trong Word để đối chiếu; tên action và route trong source là tiếng Anh, khác tên trong báo cáo.

## Những khác biệt đã xử lý

| Nguồn | Quy tắc hiện hành | Nơi xử lý và bằng chứng |
|---|---|---|
| 4.179 | Ngân sách tùy chọn, NULL khác 0 | Trip/TripInput.Budget nullable, migration AlignDetailedProcessingDesign; DesignAlignmentTests và script |
| 4.180 | Tổng chi phí NULL tính 0; chỉ so ngân sách khi có; cảnh báo cùng ngày/cùng giờ bắt đầu | JourneyManager.Detail; kiểm thử HTTP và dữ liệu SQL |
| 4.180–4.182 | Hiển thị Ngày → Thứ tự → Giờ; thêm mới MAX thứ tự cùng ngày +1 | PlanDetails, JourneyManager.Activity; script kiểm tra nhập mục chiều trước |
| 4.181 | Chi phí nullable, nguồn đã công bố; lỗi 0201-03/04 | JourneyRules, JourneyManager.Activity; PublicationChecks |
| 4.183 | Hoạt động có tham chiếu tình huống không được bỏ | IncidentActivities/FK Restrict, RemoveActivity, ca tham chiếu |
| 4.184 | Sửa ngày không làm hoạt động rơi ngoài khoảng; lỗi 0201-07 | JourneyManager.Save, planning-request-deep |
| 4.185 | Chấp nhận đúng phiên bản; cảnh báo không chặn; không tự đặt dịch vụ | Confirm, test stale version và cảnh báo |
| 4.186 | Lưu nháp trả danh sách | JourneyController.Draft; design-alignment |
| Không có giới hạn 366 ngày trong nguồn | Bỏ chặn 367 ngày; lịch chi tiết xem từng nhóm 31 ngày | JourneyRules, PlanDetails; unit 500 ngày và HTTP 367 ngày |
| 4.219–4.220 | Preview chưa ghi Booking; gửi kiểm tra lại; GUID chống trùng; thành công mở chi tiết | MarketplaceManager.Preview/Book, MarketplaceController.Book; HTTP + DB |
| 4.221, 4.223 và 4.9.1 | 20 yêu cầu/trang, lọc trạng thái, sắp CreatedAt DESC/Id DESC | Requests; GET trang>=1; kiểm thử trang 0 |
| 4.224 | Liên hệ khách chỉ hiển thị cho NCC sở hữu khi Đã xác nhận | CustomerContacts projection; kiểm tra trước/sau thỏa thuận |
| 4.225 | Lý do từ chối bắt buộc; đề xuất phải khác; lưu điều kiện cũ và SK11 | Respond, AuditEntries.Before/After, snapshot và typed notification trong transaction |
| 4.226 | Khách nhận đúng đề xuất chuyển trực tiếp Đã xác nhận; trả chi tiết | ReplyOffer; hai AcceptedVersion cùng ConditionsVersion, không thêm bước NCC |
| 4.222 và SUC08 A2 | Khách được sửa yêu cầu chưa xác nhận cả khi đang Đề xuất khác | EditPending, form Sửa yêu cầu đang chờ; tăng bản, phản hồi bản cũ 409 |
| 4.9.14 | Phản ánh mặc định 30 ngày, không đặt lại thời điểm gốc khi đối chiếu | Business:HanPhanAnhNgay, FeedbackItem/Results |
| 4.9.14 | Toàn bộ Không thực hiện/Khách không đến → Không thực hiện; còn chưa rõ → Chờ kiểm tra | AggregateResultStatus, 8 bộ dữ liệu unit |
| 4.9.1 và 4.266 | Luồng chính trả 400/404/409/422; mã nghiệp vụ có trong response lỗi POST | JourneyOutcome, BusinessFault, BusinessErrorFilter; errorCode trong http-client |
| 4.9.1 | Đồng hồ thay được, UTC và hôm nay UTC+7 | IDongHo, DongHoHeThong; inject vào hai manager trọng tâm |

## Ánh xạ từng bảng kế hoạch

| Bảng và tên xử lý trong thiết kế | Route hiện hành | Controller/Manager |
|---|---|---|
| 4.177 danhSach | GET /Journey?trang=1 | Index → List |
| 4.178 moKeHoachMoi | Form tại GET /Journey | PlanList, không tách một trang tạo riêng |
| 4.179 khoiTaoKeHoach | POST /Journey/Save, Id=0 | Save |
| 4.180 xemKeHoach | GET /Journey/Details/{id} | Details → Detail |
| 4.181 themHoatDong | POST /Journey/Activity, Id=0 | Activity |
| 4.182 suaHoatDong | Cùng route với Id>0 | Activity |
| 4.183 xoaHoatDong | POST /Journey/RemoveActivity | RemoveActivity |
| 4.184 capNhatThongTin | POST /Journey/Save, Id>0 | Save |
| 4.185 chapNhan | POST /Journey/Confirm | Confirm → ChangeStatus |
| 4.186 luuNhap | POST /Journey/Draft | Draft → ChangeStatus |

Controller: `Controllers/JourneyController.cs`. Service: `Services/JourneyManager.cs`, `JourneyRules.cs`. Input: `Models/JourneyModels.cs`. DB: Trips, Activities, IncidentActivities. View: PlanList/PlanDetails.

## Ánh xạ từng bảng yêu cầu

| Bảng và tên xử lý trong thiết kế | Route hiện hành | Controller/Manager |
|---|---|---|
| 4.218 moDatDichVu | GET /Marketplace/Details/{id} | Details → ServiceDetails |
| 4.219 kiemTraTruocGui | POST /Marketplace/Preview | Preview |
| 4.220 guiYeuCau | POST /Marketplace/Book | Book |
| 4.221 danhSachCuaToi | GET /Marketplace/Requests | Requests(provider=false) |
| 4.222 chiTiet | GET /Marketplace/Request/{id} | RequestDetails → Requests đúng chủ |
| 4.223 danhSachNCC | GET /Provider/Requests | Requests(provider=true) |
| 4.224 xemYeuCau | GET /Provider/Request/{id} | RequestDetails → Requests theo chủ dịch vụ |
| 4.225 chapNhan | POST /Provider/Respond | Respond |
| 4.226 phanHoiDeXuat | POST /Marketplace/ReplyOffer | ReplyOffer |

Controllers: MarketplaceController, ProviderController. Service: MarketplaceBookings, MarketplaceManager, MarketplacePreview. View chung Requests nhưng query, liên hệ và quyền thao tác được chiếu riêng. DB: Bookings, BookingConditionVersions, Notifications, AuditEntries. `Version` kỹ thuật khác `ConditionsVersion` nghiệp vụ.

## Bản đồ 14 SUC đề xuất cho A14

Bảng này chỉ dẫn nơi xem chức năng và bằng chứng hồi quy SUC hiện có. Chỉ hai SUC trọng tâm được đối chiếu sâu trong lượt sửa này; các dòng còn lại cần tester chạy và đối chiếu đầy đủ từng bảng xử lý trước khi đánh dấu đạt thiết kế.

| SUC | Mục và bảng trong thiết kế | Điểm vào sản phẩm | Hồi quy hiện có |
|---|---|---|---|
| 01.01 Tra cứu | 4.9.3, 4.173–4.175 | /Home/Search | security-search, PublicationChecks |
| 02.01 Lập kế hoạch | 4.9.4, 4.176–4.186 | /Journey | journey-community, planning-request-deep, design-alignment |
| 05.01 Tình huống | 4.9.9, 4.204–4.209 | /Journey/Incidents | journey-community |
| 06.01 Đăng/cập nhật dịch vụ | 4.9.10, 4.210–4.214 | /Provider | marketplace-suc |
| 06.02 Xét duyệt dịch vụ | 4.9.11, 4.215–4.217 | /Admin | marketplace-suc |
| 07.01 Yêu cầu dịch vụ | 4.9.12, 4.218–4.226 | /Marketplace | marketplace-suc, planning-request-deep, design-alignment |
| 08.01 Thay đổi/hủy | 4.9.13, 4.227–4.231 | /Marketplace/Requests | marketplace-suc và nhánh sửa đề xuất mới |
| 09.01 Kết quả thực hiện | 4.9.14, 4.232–4.235 | /Provider/Requests | marketplace-suc, DesignAlignmentTests |
| 10.01 Đăng ký | 4.9.15, 4.236–4.238 | /Account/Register | account-verification-suc |
| 10.02 Xác minh NCC | 4.9.16, 4.239–4.243 | /Verification | account-verification-suc |
| 11.01 Đăng nhập | 4.9.17, 4.244–4.247 | /Account/Login | security-search |
| 12.01 Hồ sơ | 4.9.18, 4.248–4.249 | /Account/Profile | account-verification-suc, security-search |
| 13.01 Quyền hoạt động NCC | 4.9.19, 4.250–4.258 | /Governance | GovernanceChecks |
| 14.01 Ngừng tài khoản | 4.9.20, 4.259–4.261 | /Account/Deactivate | account-verification-suc, GovernanceChecks |

Bốn SUC đồng hành 03.01, 03.02, 04.01, 04.02 có triển khai và hồi quy cũ, nhưng được đề xuất để sau trong phương án A14. 14/18=77,78% chỉ là tỷ lệ đếm SUC; cần giảng viên chốt, không tự coi là nghiệm thu hay tỷ lệ công sức.

## Khác biệt hợp đồng và phần còn rà soát

- Tên route/action/thực thể tiếng Anh, Id int, trạng thái tiếng Việt và schema hiện hành chưa trùng nguyên dạng tiếng Việt/long/CHECK trong thiết kế. Bảng ánh xạ hỗ trợ giải thích nghiệp vụ; không chứng minh tương đương tuyệt đối về hợp đồng hoặc lược đồ.
- Form tạo kế hoạch nằm trong danh sách, không tách GET riêng như 4.178. Chi tiết dịch vụ công khai vẫn có GET cho người xem chưa đăng nhập; POST gửi yêu cầu mới bắt buộc Traveler. Đây là khác biệt truy cập so với action moDatDichVu dành cho khách trong thiết kế.
- Luồng chính đã chuẩn hóa mã lỗi POST; các module cũ chưa được chứng nhận đồng nhất toàn bộ mã, thứ tự kiểm tra và HTTP theo 4.170. GET chi tiết trả 404 nhưng không phải mọi GET có header mã nghiệp vụ. Danh sách kế hoạch lấy danh sách của chủ rồi phân trang trong Controller; yêu cầu phân trang tại query SQL. Còn cần rà soát hiệu năng khi dữ liệu lớn.
- IDongHo và cơ chế retry được bổ sung cho luồng trọng tâm; chưa rà soát/kiểm thử toàn bộ lỗi hạ tầng và thời gian ở mọi service. Không coi unit pass là đã kiểm chứng deadlock thật hoặc rollback dưới mọi lỗi vật lý.
- Nhật ký đã ghi các thao tác, SK11 trước/sau và thông báo typed cho yêu cầu. Chưa chứng nhận toàn bộ sự kiện SK01–SK10, mọi loại thông báo, chỉ mục và ràng buộc DB khớp tuyệt đối thiết kế. Dữ liệu lịch sử trước migration giữ nguyên, không tạo lại nội dung cũ bằng suy đoán.
- Token preview 10 phút và đơn giá × số lượng là biện pháp triển khai được mô tả rõ trong hướng dẫn. Nguồn có chiPhiDuKien nullable cho yêu cầu; bản hiện tại tính tổng ban đầu từ danh mục, chưa có ô nhập riêng ngân sách mong muốn cho yêu cầu. Chưa có tồn kho, giá theo đêm, thanh toán, email/SMS hoặc giữ chỗ thật.
- Bảng 4.172 ghi 88 action (45 POST, 43 GET), Bảng 4.267 ghi 91. Cần thống nhất tài liệu nguồn. Vị trí một số hình khi xuất Word là ảnh trắng; dùng bảng văn bản có nội dung để đối chiếu thay vì tự suy ra sơ đồ.

## Cách tester đối chiếu

Mỗi ca ghi bảng thiết kế, input, tiền điều kiện, trạng thái trước/sau, HTTP/mã, DB và actual riêng. Các ma trận KH01–KH43 và DV01–DV56 được sửa oracle theo thiết kế mới, vẫn là ca đề nghị. Bổ sung ca nullable, trang 20/21, sửa khi Đề xuất khác, 30 ngày phản ánh, SK11 và thông báo trong cùng giao dịch. Chạy `run-tests.ps1`, đọc summary mới; kết quả không thay thế UAT độc lập.

Script SQL chỉ đọc: `show-demo-data.ps1 -TripId <id> -RequestId <id> [-Database TuTucTester]`. Dùng GUID/tên mới cho mỗi lượt, không xóa DB hoặc sửa trạng thái để tạo bằng chứng giả.

## Rà soát bổ sung sau lần bàn giao

Đã kiểm tra thêm danh sách NCC có nhiều yêu cầu của cùng khách: liên hệ phải được chặn theo trạng thái từng yêu cầu, không chỉ dựa trên việc khách có một yêu cầu đã xác nhận. Đã sửa điều kiện hiển thị, thêm assertion cho cả bài chờ và bài xác nhận. Đã sửa phép tính phân trang bằng long, trang cực lớn trả rỗng thay vì gây tràn; có regression cho ba danh sách. Xem RA_SOAT_DEMO.md và kết quả mới trong TEST-RESULTS.md.
