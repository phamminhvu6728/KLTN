# Bàn giao kiểm thử — Hỗ trợ khách du lịch tự túc

Phạm vi nghiệp vụ: đặc tả SUC V04 ngày 01/11/2026. Cập nhật xử lý ngày 03/11/2026 theo thiết kế chi tiết.docx mục 4.9; ưu tiên luồng kế hoạch và yêu cầu dịch vụ trong [bản đối chiếu](ban-giao-kiem-thu/DOI_CHIEU_THIET_KE.md). Tài liệu có **14 nhóm nghiệp vụ/BUC và 18 SUC chi tiết**, không phải chỉ 14 màn hình. Kiến trúc ASP.NET Core MVC 10, Razor, EF Core 10, SQL Server LocalDB được giữ theo đề tài.

Bổ sung ngày 03/11/2026: [gói kiểm thử sâu hai SUC](ban-giao-kiem-thu/README.md) có hợp đồng trường, route, locator và 99 ca đề nghị tester tự viết test. Kết quả hiện tại xem [TEST-RESULTS.md](TEST-RESULTS.md); không lấy số liệu lịch sử trong phần dưới làm số mới nhất.

## Chạy sản phẩm

Tại thư mục dự án, chạy `start-demo.cmd`, mở http://localhost:5080. Script chờ `/health` báo sẵn sàng rồi mở trình duyệt; không tạo server thứ hai khi đã có bản đang chạy.

Yêu cầu: Windows, .NET SDK 10, SQL Server LocalDB instance MSSQLLocalDB. Database demo `TuTucDemo` tự khởi tạo/migrate. Không xóa MDF/LDF để xử lý lỗi. Nếu có database cũ bị detach thì cần attach/khôi phục có kiểm tra; không tự coi là dữ liệu rác.

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Khách A | khach@demo.vn | Demo12345 |
| Khách B | anh@demo.vn | Demo12345 |
| Nhà cung cấp | ncc@demo.vn | Demo12345 |
| Admin | admin@demo.vn | Demo12345 |

Dùng hai trình duyệt hoặc cửa sổ ẩn danh khi kiểm tra tương tác hai bên. Đổi vai trò trên cùng trình duyệt sẽ thay phiên đăng nhập hiện tại.

## Chạy tự động và xem bằng chứng

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\run-tests.ps1
```

Cần Node.js 20+ có `fetch` và `headers.getSetCookie`. Bộ test khởi tạo server HTTP thật và database tên `TuTucTest_<guid>`, không chạy các luồng thay đổi trạng thái trên `TuTucDemo`. Tệp tải lên của test nằm trong `TestResults/TuTucTest_<guid>/UploadFiles`. Database kiểm thử được giữ lại phục vụ đối chiếu, có tên trong TRX. Không cần tự mở website trước khi chạy.

Kết quả:

- `TestResults/acceptance.trx`: kết quả xUnit và log từng assertion HTTP/nghiệp vụ.
- `TestResults/summary.json`: số test pass/fail, line coverage, branch coverage, đường dẫn Cobertura.
- `TestResults/<guid>/coverage.cobertura.xml`: coverage thô.

Runner trả lỗi nếu có test fail hoặc **combined line coverage <80%**. Coverage loại mã sinh tự động `obj` và EF migration, không loại controller/service nghiệp vụ đang dùng. Đây là coverage kết hợp unit + integration/system, không được ghi thành “unit riêng 80%, integration riêng 80%”. Branch coverage được báo riêng và chưa đặt ngưỡng 80%. Do nhiều hàm đang viết gọn trên cùng dòng, line coverage không chứng minh mọi nhánh đã được thử.

Không sử dụng `account-library-test.mjs` và `marketplace-test.mjs` cũ làm bằng chứng SUC V04; test được runner chọn là bản hiện hành.

## Bốn mức kiểm thử

| Mức | Đã chuẩn bị | Cách đánh giá |
|---|---|---|
| Unit | BusinessRulesTests: quyền NCC theo role/trạng thái, chuẩn hóa điện thoại, biên đặt dịch vụ, ảnh, tạm ngừng, snapshot/hạng mục | xUnit assertion trực tiếp, không DB |
| Integration | EF Core + SQL Server thật, giao dịch, rowversion, xác minh, quyết định quyền, nghĩa vụ ngừng tài khoản | SystemTests và các script HTTP |
| System | HTTP thật, cookie, CSRF, 3 vai trò, luồng nhiều bên, dữ liệu đầu vào và trạng thái | 7 script được runner gọi, test hồi quy công bố |
| Acceptance/UAT | Ma trận và kịch bản bên dưới | Người kiểm thử thực hiện trên trình duyệt, ghi kết quả thực tế và ký xác nhận; chưa tự coi là đã nghiệm thu |

## Truy vết 18 SUC thuộc 14 nhóm

| Nhóm/SUC | Điểm vào | Điều kiện cần kiểm | Bằng chứng tự động |
|---|---|---|---|
| 01 / 01.01 | /Home/Search | Bộ lọc hợp lệ, rỗng kết quả, chỉ dịch vụ đã duyệt; thu hồi bị ẩn | security-search, PublicationChecks |
| 02 / 02.01 | /Journey | Nháp/chấp nhận; ngoài ngày bị chặn; trùng giờ/vượt tiền chỉ cảnh báo; đúng chủ sở hữu | journey-community |
| 03 / 03.01 | /Community | Lọc nhu cầu, phạm vi chia sẻ, không lộ liên hệ | journey-community |
| 03 / 03.02 | /Community/Compose | Xem trước chưa lưu, công bố đúng nội dung/phạm vi, dừng công bố | journey-community |
| 04 / 04.01 | /Community/Dashboard | Gửi/nhận/rút/từ chối, chỉ đúng bên được phản hồi | journey-community |
| 04 / 04.02 | /Community/Dashboard | Cùng phiên bản mới có hiệu lực, giữ thỏa thuận cũ khi sửa; rút một bên và cùng ngừng | journey-community |
| 05 / 05.01 | /Journey/Incidents | Tình huống gắn chuyến đi/hoạt động; phương án khác kết quả; 5 trạng thái; giữ lịch sử | journey-community |
| 06 / 06.01 | /Provider | Nháp/gửi duyệt; chỉ NCC đủ quyền; sửa không đổi bản công khai đã duyệt | marketplace-suc |
| 06 / 06.02 | /Admin | Duyệt/cần sửa/từ chối; bắt buộc lý do khi không duyệt | marketplace-suc |
| 07 / 07.01 | /Marketplace | Xem trước/gửi; không trùng; đề xuất khác được khách đồng ý đúng bản thì xác nhận trực tiếp theo 4.226 | marketplace-suc |
| 08 / 08.01 | /Marketplace/Requests | Đổi/hủy có xác nhận, giữ snapshot cũ, không tự ghi đè | marketplace-suc |
| 09 / 09.01 | /Provider/Requests | Kết quả theo hạng mục; chưa rõ phải đối chiếu; phản ánh không đè kết quả | marketplace-suc |
| 10 / 10.01 | /Account/Register | Đăng ký khách/NCC, điện thoại, hồ sơ và tệp đúng định dạng, tạo nguyên tử | account-verification-suc |
| 10 / 10.02 | /Verification | Admin xác minh; yêu cầu bổ sung/từ chối có lý do; phiên bản và hồ sơ cũ được giữ | account-verification-suc |
| 11 / 11.01 | /Account/Login | Đúng/sai mật khẩu, role, logout, URL chuyển tiếp, thu hồi phiên cũ | security-search |
| 12 / 12.01 | /Account/Profile | Chỉ sửa thông tin cho phép; chặn phiên bản cũ; mật khẩu thu hồi phiên | account-verification-suc, security-search |
| 13 / 13.01 | /Governance | Preview chưa áp dụng; đình chỉ/giải trình/khôi phục/thu hồi; bảo toàn thỏa thuận đã chốt | GovernanceChecks |
| 14 / 14.01 | /Account/Deactivate | Xác thực lại; kiểm nghĩa vụ; token xác nhận; ngừng đồng bộ và mất phiên | account-verification-suc, GovernanceChecks |

## Liên hệ mục tiêu đề tài 1, 2, 4

| Mục tiêu | Phần sản phẩm để kiểm thử |
|---|---|
| 1 — Tìm kiếm/tổng hợp thông tin | Tra cứu điểm đến và dịch vụ đã công bố, bộ lọc, chi tiết và đưa nguồn tham khảo vào lịch trình |
| 2 — Chọn phương án phù hợp cá nhân | Lịch trình theo ngày, chi phí dự kiến, cảnh báo giờ/ngân sách; xem trước dịch vụ, thương lượng điều kiện và lưu phiên bản |
| 4 — Xử lý nhu cầu phát sinh | Tình huống gắn chuyến đi, lịch sử phương án, kết quả thực tế; liên kết tìm phương án thay thế và quy trình đổi/hủy dịch vụ |

Hệ thống cũng có nhóm chức năng đồng hành phục vụ mục tiêu 3. Việc đánh giá đạt mục tiêu cần dựa trên thực hiện kịch bản, không chỉ đếm chức năng.

## UAT — người kiểm thử thực hiện và ghi kết quả

| ID | Thao tác | Kết quả mong đợi | Kết quả thực tế |
|---|---|---|---|
| UAT01 | Chưa login, tìm Đà Lạt; lọc vùng/giá; thử giá âm và từ > đến | Kết quả đúng; sai dữ liệu có thông báo; không công bố bản chưa duyệt | Chưa thực hiện |
| UAT02 | Khách tạo chuyến đi; thêm 2 hoạt động trùng giờ, vượt ngân sách; xác nhận | Có cảnh báo nhưng được lưu; trạng thái Đã chấp nhận | Chưa thực hiện |
| UAT03 | Thêm hoạt động nằm ngoài ngày chuyến đi; mở ID chuyến của khách khác | Không lưu hoạt động sai; không được truy cập kế hoạch của người khác | Chưa thực hiện |
| UAT04 | Tạo tình huống; chọn rồi đổi phương án; lưu kết quả một phần rồi hoàn tất | Lịch sử phương án còn; trạng thái phản ánh kết quả thật; hoàn tất chỉ xem | Chưa thực hiện |
| UAT05 | Khách B tìm nhu cầu Public/Connected; gửi đề nghị; khách A phản hồi | Đúng phạm vi riêng tư; chấp nhận kết nối chưa tự tạo thỏa thuận | Chưa thực hiện |
| UAT06 | Hai bên đề xuất/sửa/chấp nhận thỏa thuận; thử tự xác nhận đề xuất mình | Chỉ bên kia xác nhận; cùng phiên bản mới hiệu lực; cũ được giữ | Chưa thực hiện |
| UAT07 | Rút một bên khi đang có đề nghị sửa; kiểm tra bên còn lại | Thỏa thuận kết thúc ngay Một bên đã rút; thông báo chưa đọc không giữ hiệu lực | Chưa thực hiện |
| UAT08 | NCC đăng dịch vụ; Admin yêu cầu sửa rồi duyệt; xem danh mục khách | Chỉ bản đã duyệt xuất hiện; thấy lý do cần sửa | Chưa thực hiện |
| UAT09 | Khách gửi yêu cầu; NCC đề xuất khác; khách chấp nhận đúng bản và xác nhận trực tiếp | Đúng từng trạng thái, gửi lặp không tạo 2 yêu cầu | Chưa thực hiện |
| UAT10 | Khách đề nghị đổi/hủy; NCC phản hồi; mở lịch sử điều kiện | Chỉ thay đổi sau chấp thuận, snapshot cũ còn để đối chiếu | Chưa thực hiện |
| UAT11 | Yêu cầu ngày hôm nay: NCC ghi từng hạng mục, có mục chưa rõ; khách phản ánh | Không kết luận hoàn tất sớm; phản ánh không đè kết quả; lưu lịch sử đối chiếu | Chưa thực hiện |
| UAT12 | Đăng ký NCC mới với tệp sai rồi đúng; Admin yêu cầu bổ sung, từ chối, nhận hồ sơ mới, xác minh | Không lưu hồ sơ sai; đúng người đọc tệp; giữ các lần gửi/duyệt | Chưa thực hiện |
| UAT13 | Đăng nhập cùng tài khoản 2 trình duyệt; đổi mật khẩu ở A; dùng B | B bị yêu cầu login; A tiếp tục; mật khẩu cũ không đăng nhập được | Chưa thực hiện |
| UAT14 | Admin xem trước đình chỉ rồi hủy; sau đó xác nhận; NCC gửi giải trình; Admin khôi phục/thu hồi | Preview không đổi trạng thái; không nhận mới khi bị đình chỉ; thỏa thuận cũ giữ nguyên; thu hồi ẩn dịch vụ | Chưa thực hiện |
| UAT15 | Thử ngừng tài khoản còn nghĩa vụ; xử lý nghĩa vụ rồi xác thực lại, xác nhận ngừng | Còn nghĩa vụ bị chặn; đủ điều kiện ngừng đồng bộ; phiên cũ hết hiệu lực | Chưa thực hiện |
| UAT16 | Kiểm giao diện ở 1366px/390px, zoom 125%, tab bàn phím; chạy launcher 2 lần | Đọc được form/thông báo, bảng cuộn khi cần; không tạo 2 server | Chưa thực hiện |
| UAT17 | Bấm tạo/sửa/bỏ hoạt động; chọn Quay lại rồi thử lại và chọn Có, xác nhận; thử Escape | Có hộp xác nhận, tóm tắt mục đang xử lý; Quay lại/Escape giữ dữ liệu; xác nhận mới gửi; không gửi trùng | Đã kiểm trực tiếp trên trình duyệt với chuyến/hoạt động kiểm thử |

Màn hình xác nhận dùng chung áp dụng cho form thay đổi dữ liệu; không hỏi lặp với các luồng đã có trang xem trước/xác nhận riêng (công bố nhu cầu, đặt dịch vụ, quyết định quyền hoạt động, ngừng tài khoản). Mật khẩu và trường ẩn không xuất hiện trong tóm tắt. Lưu ý các test HTTP trực tiếp không chạy JavaScript; UAT17 là kiểm tra giao diện bổ sung, không thay thế kiểm tra quyền và dữ liệu phía server.

Ghi lỗi theo mẫu: mã UAT/SUC; phiên bản build; tài khoản/vai trò; bước tái hiện; dữ liệu đầu vào; mong đợi; thực tế; ảnh/log; mức độ. Không ghi mật khẩu thật hoặc tệp xác minh nhạy cảm vào báo cáo.

## Ranh giới bàn giao

Bản local đủ để bắt đầu kiểm thử nghiệp vụ; UAT và kiểm thử giao diện trình duyệt vẫn cần người kiểm thử xác nhận. Chưa có bằng chứng kiểm thử tải lớn, pentest hay vận hành production. Không tích hợp thanh toán, email/SMS hoặc cứu hộ thực địa. Các “điểm cần chốt” trong SUC vẫn là giả định nhóm cần bảo vệ/đồng bộ với FR, không tự coi là đã khảo sát hoặc được thầy duyệt.
