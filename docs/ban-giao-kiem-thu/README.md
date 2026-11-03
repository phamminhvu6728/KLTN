# Bàn giao kiểm thử lập kế hoạch và yêu cầu dịch vụ

Bản ngày 03/11/2026. Tester dùng tài liệu này để tự thiết kế và viết mã kiểm thử độc lập. Mã test có sẵn là bằng chứng hồi quy của phía phát triển, chưa phải biên bản nghiệm thu của tester.

Hai SUC trọng tâm: **SUC02.01 Lập kế hoạch chuyến đi** và **SUC07.01 Đặt dịch vụ du lịch**. Luồng demo đi từ kế hoạch dự kiến đến thỏa thuận được hai bên xác nhận. Đối chiếu xử lý mới: **thiết kế chi tiết.docx mục 4.9.4 và 4.9.12**, bản trích đọc tại `docs/requirements/THIET_KE_CHI_TIET.txt`. Bản đồ bảng → route → code và các điểm cần chốt ở [DOI_CHIEU_THIET_KE.md](DOI_CHIEU_THIET_KE.md). Xem [quy tắc kế hoạch](LAP_KE_HOACH.md), [quy tắc yêu cầu](YEU_CAU_DICH_VU.md), [kịch bản demo](DEMO_8_PHUT.md).

## Chạy phần mềm

Trên máy hiện tại, mở PowerShell tại thư mục dự án:

```powershell
cd "C:\Users\bakav\Demo KLTN\Ho tro khach du lich tu tuc"
.\start-demo.cmd
```

Mở http://localhost:5080. Launcher dùng lại server đang sẵn sàng, tránh lỗi chiếm cổng. Cần .NET SDK 10 và SQL Server LocalDB instance MSSQLLocalDB. Database và dữ liệu mẫu được khởi tạo khi ứng dụng chạy. Kiểm tra `dotnet --info`, `sqllocaldb info`, `Invoke-RestMethod http://localhost:5080/health` nếu máy khác chưa chạy được. Không khởi động thêm `dotnet run` khi server đã chiếm 5080.

| Vai trò | Email mẫu | Mật khẩu |
|---|---|---|
| Khách | khach@demo.vn | Demo12345 |
| Nhà cung cấp | ncc@demo.vn | Demo12345 |
| Quản trị | admin@demo.vn | Demo12345 |

Các nút Tài khoản mẫu là tiện ích demo. Khi kiểm thử đăng nhập thật, dùng biểu mẫu email và mật khẩu. Hai tab cùng trình duyệt dùng chung cookie; để hai vai trò cùng hoạt động, dùng hai profile/trình duyệt hoặc cửa sổ riêng tư. Nên tạo hai khách riêng để kiểm thử quyền sở hữu.

## Môi trường riêng cho tester

Không chạy test thay đổi trạng thái hàng loạt trên `TuTucDemo`. Ví dụ chạy phiên kiểm thử riêng tại cổng 5081 trong một cửa sổ PowerShell riêng:

```powershell
$env:ConnectionStrings__Default='Server=(localdb)\MSSQLLocalDB;Database=TuTucTester;Trusted_Connection=True;TrustServerCertificate=True'
$env:Demo__Enabled='true'
$env:Storage__UploadDirectory=Join-Path (Get-Location) 'TestResults\TesterUploads'
dotnet run --no-launch-profile --urls http://localhost:5081
```

Các biến chỉ áp dụng cho cửa sổ này và tiến trình con. Database `TuTucTester` được giữ giữa các lần chạy; dùng tên có hậu tố riêng nếu cần dữ liệu sạch. Không cần xóa database để chạy lại. Đặt tiền tố `TEST_<người>_<thời điểm>` cho tên kế hoạch và ghi chú yêu cầu để phân biệt dữ liệu.

## Bốn cấp kiểm thử

| Cấp | Tester tự viết gì | Bằng chứng cần lưu |
|---|---|---|
| Unit | Gọi JourneyRules.ValidateTrip, ValidateActivity, MarketplaceManager.ValidateTerms; dữ liệu biên, rỗng, sai định dạng ở tầng binding | Input, kết quả trả về và assertion |
| Tích hợp | Manager + SQL thật; quyền sở hữu, rowversion, giao dịch, lịch sử, chống gửi trùng | Trước và sau DB, số dòng, phiên bản |
| Hệ thống | HTTP hoặc Playwright qua trình duyệt; đăng nhập, CSRF, màn hình xác nhận, hai vai trò | HTTP/status, ảnh, trạng thái cuối |
| Nghiệm thu | Người dùng chạy tình huống theo SUC và xác nhận kết quả | Biên bản có người chạy, ngày, môi trường, actual, pass/fail |

Ví dụ unit mới trong dự án test:

```csharp
var input = new TripInput { Name="A14", Destination="Đà Lạt",
    Start=new DateTime(2026,12,1), End=new DateTime(2026,12,3), Budget=0 };
Assert.Equal("", JourneyRules.ValidateTrip(input));
input.End=input.Start.AddDays(-1);
Assert.NotEmpty(JourneyRules.ValidateTrip(input));
```

Ngày tương lai so với hôm nay được kiểm tra ở manager, không nằm trong pure ValidateTrip. Đừng lấy unit pass để suy ra HTTP hoặc phân quyền đã đúng.

## Hợp đồng HTTP và giao diện

Đây là MVC trả HTML, chưa có REST API JSON cho nghiệp vụ. POST dùng `application/x-www-form-urlencoded`, cookie đăng nhập và `__RequestVerificationToken` lấy từ form của **đúng phiên, đúng vai trò**. Không dùng lại CSRF của khách cho nhà cung cấp. Sau POST thành công thường là 302; client tự theo redirect sẽ thấy 200. Kiểm tra cả nội dung và DB, không chỉ status.

Luồng kế hoạch và yêu cầu chính trả lỗi trường 400, nghiệp vụ 422, phiên bản 409, khác chủ 404. Đọc cả header X-Business-Error. Gửi/nhận đề xuất thành công mới 302; lỗi giữ nội dung soạn trên trang. Route chi tiết đối tượng không thuộc quyền trả 404; sai vai trò có thể redirect AccessDenied. Login/POST tài khoản quá dày có thể 429, không được ghi nhầm là lỗi nghiệp vụ.

Các locator ổn định: `trip-create-form`, `trip-details`, `trip-edit-form`, `trip-confirm-form`, `trip-status`, `trip-total`, `trip-warnings`, `activity-form-0`, `activity-form-{id}`, `activity-remove-{id}`, `request-form`, `request-errors`, `request-preview`, `request-send-form`, `request-{id}`. Dialog xác nhận dùng role `dialog`, nút `Quay lại` và `Có, xác nhận`. Test HTTP trực tiếp không kiểm tra được dialog JavaScript.

## Xem dữ liệu đối chiếu

Ví dụ nếu có `sqlcmd` trong PATH:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TuTucTester -E -W -Q "SELECT Id,Name,OwnerId,Start,[End],Budget,Status FROM Trips ORDER BY Id DESC"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TuTucTester -E -W -Q "SELECT TripId,Title,Start,[End],Cost FROM Activities ORDER BY TripId,Start"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TuTucTester -E -W -Q "SELECT Id,OwnerId,TravelServiceId,Status,Total,ConditionsVersion,CustomerAcceptedVersion,ProviderAcceptedVersion FROM Bookings ORDER BY Id DESC"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d TuTucTester -E -W -Q "SELECT BookingId,VersionNumber,Reason,Snapshot FROM BookingConditionVersions ORDER BY BookingId,VersionNumber"
```

Các bảng phụ để đối chiếu: Accounts, Services, ServiceRevisions, Notifications, AuditEntries. Không xuất PasswordHash hoặc hồ sơ riêng tư vào báo cáo lỗi. Một thao tác bị chặn phải giữ nguyên dữ liệu nghiệp vụ trước đó; audit và thông báo cần kiểm tra theo từng thao tác.

## Chạy bộ hồi quy phía phát triển

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\run-tests.ps1
Get-Content .\TestResults\summary.json
```

Bộ hệ thống tự tạo SQL database `TuTucTest_<guid>` và upload directory riêng; giữ DB để tìm lỗi. Kết quả thật ở `TestResults/acceptance.trx`, `summary.json` và Cobertura được chỉ ra trong summary. Line coverage gộp không đồng nghĩa mỗi cấp test đạt 80%, không đồng nghĩa 80% nhánh hoặc nghiệm thu 80% SUC. Xem số đo cập nhật trong `docs/TEST-RESULTS.md`.

## Cách gửi lỗi và quyết định nghiệm thu

Mỗi lỗi ghi: mã ca, bản source/ngày, database/cổng, vai trò, dữ liệu đầu vào, bước tái hiện, expected, actual, ảnh hoặc HTTP, số dòng/phiên bản DB trước và sau. Ưu tiên lỗi mất dữ liệu, vượt quyền, xác nhận sai điều kiện, tạo trùng trước lỗi bố cục. Ma trận trong hai tài liệu là ca **đề nghị tester chạy**, không đánh dấu pass sẵn.

Phạm vi A14 vẫn là phương án chọn 14 trong 18 SUC, ưu tiên mục tiêu 1, 2, 4; bốn SUC đồng hành được đề xuất để giai đoạn sau. 14/18 là 77,78% theo số SUC, không phải khối lượng công việc hay tỷ lệ test. Việc demo sâu hai SUC không thay thế nghiệm thu 14 SUC, không có nghĩa đã được giảng viên duyệt phạm vi.

## Giới hạn cần biết khi đánh giá

- Kế hoạch không tự đặt dịch vụ. Link từ hoạt động mở một yêu cầu mới và điền ngày; chưa có liên kết Trip–Booking được lưu trong DB.
- Dự kiến ban đầu bằng đơn giá công bố × số lượng đơn vị. Chưa tự tính số đêm, phụ phí, tồn kho hoặc giữ chỗ. Ngày kết thúc hiện tùy chọn kể cả lưu trú; đây là chính sách bản demo, cần chốt khi phát triển bảng giá theo loại dịch vụ.
- Chưa thanh toán, đặt cọc, hoàn tiền hoặc thực hiện cung cấp dịch vụ thật. Thông báo nằm trong ứng dụng.
- Đã bỏ giới hạn 366 ngày; ngân sách và chi phí nullable; cùng giờ bắt đầu cảnh báo theo 4.180. Khách nhận đề xuất chuyển thẳng Đã xác nhận theo 4.226. Bản sửa này thay kỳ vọng cũ về bước NCC xác nhận cuối.
- Chưa có kết quả UAT độc lập, tải lớn hoặc kiểm thử an toàn triển khai công khai. Bản bàn giao này dùng local để tester tiếp tục đánh giá.
