# Hệ thống hỗ trợ khách du lịch tự túc

Bản cập nhật ngày 03/11/2026: giữ giao diện quản lý chuyến đi hiện tại; cập nhật xử lý SUC02.01 và SUC07.01 theo thiết kế chi tiết.docx mục 4.9. Xem [đối chiếu thiết kế](docs/ban-giao-kiem-thu/DOI_CHIEU_THIET_KE.md). Bắt đầu tại [gói bàn giao tester](docs/ban-giao-kiem-thu/README.md) và [kịch bản demo 8 phút](docs/ban-giao-kiem-thu/DEMO_8_PHUT.md). Hai file Word trong `docs/Huong dan demo` đã viết lại dễ hiểu: file 01 gồm 6 trang thao tác, file 02 gồm 7 trang giải thích. Đọc [kết quả rà soát](docs/RA_SOAT_DEMO.md) trước khi bàn giao. Chạy `pack-tester.ps1` để đóng gói source, tài liệu và bằng chứng, không mang theo DB hoặc upload riêng tư.

Bản bàn giao kiểm thử SUC V04: xem [ma trận SUC, hướng dẫn chạy test và UAT](docs/TESTING.md).

Chạy toàn bộ unit + integration/system và đo coverage (tự tạo database riêng):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\run-tests.ps1
```

Kết quả nằm trong `TestResults/acceptance.trx`, `TestResults/summary.json` và Cobertura XML. Ngưỡng tự động: combined line coverage >=80%; không đồng nghĩa branch coverage hay nghiệm thu đạt 80%.

## Mở và chạy

Website: http://localhost:5080

Nhấp `start-demo.cmd`, hoặc chạy:

```powershell
dotnet run --no-launch-profile --urls http://localhost:5080
```

Yêu cầu .NET SDK 10 và SQL Server Express LocalDB. CSDL `TuTucDemo` tự khởi tạo. Bản cập nhật bổ sung bảng/cột và giữ nguyên dữ liệu cũ. Sau khi cập nhật ứng dụng, tải lại trang trình duyệt bằng Ctrl+F5.

## Trải nghiệm nhanh

Thanh **KHÔNG GIAN DEMO** ở trên mọi trang cho phép vào ngay ba vai trò. Không cần gõ mật khẩu. Chuyển vai trò sẽ thay phiên đăng nhập hiện tại.

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Khách du lịch | khach@demo.vn | Demo12345 |
| Nhà cung cấp | ncc@demo.vn | Demo12345 |
| Quản trị viên | admin@demo.vn | Demo12345 |
| Khách thứ hai | anh@demo.vn | Demo12345 |

Tài khoản thứ hai dùng để thử xác nhận đồng hành bởi bên còn lại. Dữ liệu mẫu gồm chuyến đi có ba hoạt động, yêu cầu chờ nhà cung cấp, yêu cầu trong quá khứ để ghi kết quả và một dịch vụ chờ quản trị duyệt.

## Các chức năng đã có

- Khám phá, tìm điểm đến theo tên/tỉnh thành/loại hình, chi tiết và ngân sách gợi ý.
- Tìm kiếm và lọc điểm đến/dịch vụ công khai theo từ khóa, khu vực, loại hình và ngân sách.
- Tổng quan cá nhân, tạo/sửa/lưu nháp/chấp nhận chuyến đi; thêm/sửa/bỏ hoạt động; sắp theo ngày, thứ tự rồi giờ, kiểm tra trùng giờ và khoảng ngày, so ngân sách, xác nhận đúng phiên bản. Chỉnh sửa đưa kế hoạch về nháp.
- Tìm nhu cầu đồng hành, công bố/sửa/dừng nhu cầu, gửi/rút/chấp nhận/từ chối đề nghị. Đề xuất thỏa thuận có phiên bản, bên còn lại xác nhận; cùng ngừng cũng cần hai bên đồng ý.
- Ghi tình huống theo chuyến đi, lưu phương án, ghi riêng kết quả thực tế; phương án chưa được xem là kết quả đã xử lý.
- Danh mục dịch vụ đã duyệt, lọc và xem chi tiết, gửi yêu cầu, theo dõi trạng thái, rút khi còn chờ.
- Nhà cung cấp tạo dịch vụ và bản cập nhật chờ duyệt; nội dung đang công bố giữ nguyên đến khi bản mới được duyệt. Thông báo tạm ngừng phục vụ cũng phải duyệt. Cờ tiếp nhận mới do hệ thống quản lý theo quyền hoạt động/tài khoản.
- Thỏa thuận dịch vụ đã xác nhận: đề nghị điều chỉnh ngày/số lượng/tổng tiền hoặc hủy, chỉ thay đổi khi bên còn lại chấp thuận. Tên và giá thỏa thuận không tự đổi theo giá công bố.
- Ghi kết quả dịch vụ khi đến ngày phục vụ, khách gửi phản ánh; kết quả không bị ghi đè.
- Quản trị xác minh/tạm ngừng nhà cung cấp, duyệt/từ chối phiên bản dịch vụ và xem nhật ký thao tác.
- Thông báo nội bộ cho yêu cầu dịch vụ/kết nối, đánh dấu đã đọc. Hồ sơ cá nhân, đổi mật khẩu thu hồi phiên cũ, ngừng tài khoản có kiểm tra nghĩa vụ.
- Giao diện đáp ứng máy tính và điện thoại, thông báo kết quả và trang lỗi có điều hướng.

## Kịch bản thuyết trình

1. Trang chủ: tìm Đà Lạt, lọc theo ngân sách và mở chi tiết điểm đến/dịch vụ.
2. Chọn **Khách du lịch**: mở chuyến đi mẫu, thêm hoạt động, thử thêm hoạt động trùng giờ, sửa lại và xác nhận lịch trình.
3. Vào **Dịch vụ**, chọn dịch vụ và gửi yêu cầu. Chuyển **Nhà cung cấp** → Yêu cầu của khách → xác nhận.
4. Chuyển lại khách, mở yêu cầu, đề xuất điều chỉnh. Chuyển nhà cung cấp để đồng ý. Điều kiện chỉ đổi sau khi được đồng ý.
5. Nhà cung cấp tạo dịch vụ; chuyển **Quản trị** để duyệt. Vào danh mục để thấy nội dung vừa duyệt.
6. Khách công bố nhu cầu đồng hành; dùng tài khoản khách thứ hai gửi đề nghị, chủ nhu cầu chấp nhận. Đề xuất và xác nhận thỏa thuận bởi hai tài khoản.
7. Nhà cung cấp dùng yêu cầu mẫu trong quá khứ để ghi kết quả, khách mở lại để phản ánh.

## Kiến trúc theo tài liệu

- Client–Server, một ứng dụng ASP.NET Core MVC 10 chạy Kestrel.
- Presentation: Controllers, Razor Views và input/view models.
- Business Logic: JourneyManager, CommunityManager, MarketplaceManager, AccountManager, VerificationManager và GovernanceManager kiểm tra quyền, trạng thái và ranh giới giao dịch.
- Domain Model: Models. Data Access: EF Core 10, AppDbContext và SQL Server Express LocalDB.
- Bootstrap 5, HTML5, CSS3, JavaScript; không dùng SPA hay API riêng.
- Cookie Authentication, PasswordHasher, Antiforgery toàn cục, Data Protection, Rate Limiting; phiên đối chiếu dấu phiên tài khoản trên máy chủ.
- RowVersion cho lịch trình, yêu cầu, bản duyệt và thỏa thuận. Chỉ mục duy nhất chống gửi lặp và nhiều đề nghị chờ cùng lúc. Nhật ký chỉ ghi tham chiếu thao tác, không ghi mật khẩu.

## Kiểm tra tích hợp

Khi server đang chạy:

```powershell
node journey-community-test.mjs
node marketplace-test.mjs
node account-verification-suc-test.mjs
```

Các bài kiểm tra dùng HTTP thật, cookie, token CSRF và SQL Server thật; tạo dữ liệu kiểm thử riêng. Build: `dotnet build`.

## Giới hạn triển khai

Đây là bản demo theo SUC V04 đã đối chiếu sâu các bảng xử lý kế hoạch/yêu cầu trong thiết kế mới. Tên route, action và kiểu khóa còn khác hợp đồng thiết kế; chưa tuyên bố đã nghiệm thu toàn bộ action. Bảng 4.172 ghi 88 action trong khi Bảng 4.267 ghi 91, cần thống nhất báo cáo. Hướng dẫn Word được tạo lại bằng docs/build_design_guides.py. Chưa có thanh toán, email/SMS và triển khai production.

Không tích hợp thanh toán/bản đồ/đăng nhập bên thứ ba theo phạm vi tài liệu. Giá, tài khoản và ảnh là dữ liệu minh họa. Ảnh đã lưu cục bộ nên bản demo không cần tải ảnh mạng.

Khi triển khai ngoài máy demo, tắt `Demo:Enabled`, cấp tài khoản quản trị riêng, cấu hình HTTPS/chứng chỉ và SQL Server theo môi trường đích. Không bật đăng nhập nhanh demo trên môi trường công khai.
