# Ôn tập và báo cáo theo vai trò dev — SUC02.01 Lập kế hoạch

Đối chiếu logic mã nguồn tại commit `cf2bb4f`; cập nhật cách trình bày code và vị trí liên kết ngày 05/11/2026. Tài liệu này hướng dẫn giải thích bản sản phẩm đã chốt, không thay đổi nghiệp vụ hay thay thế kết quả kiểm thử của tester.

## 1. Mục tiêu của buổi trình bày

Bạn cần giải thích được một đường đi hoàn chỉnh: **yêu cầu trong thiết kế → lớp tiếp nhận → quy tắc nghiệp vụ → thay đổi dữ liệu → giao dịch và phiên bản → kết quả trả về**.

Lấy **thêm hoạt động vào kế hoạch** làm luồng giải thích sâu. Tạo kế hoạch là bước chuẩn bị; xem tổng chi phí, sửa hoạt động và chấp nhận là phần nối để khép kín SUC. Không cần đọc hết mọi hàm của hệ thống.

Lời mở đầu có thể dùng:

> Em trình bày SUC02.01 Lập kế hoạch chuyến đi. Em đi sâu vào xử lý thêm hoạt động, đối chiếu Bảng 4.181, từ biểu mẫu qua Controller, lớp xử lý nghiệp vụ đến CSDL. Sau đó em minh họa việc chấp nhận đúng phiên bản và cách ngăn một phiên cũ ghi đè dữ liệu mới.

## 2. Học gì trong đêm nay, theo thứ tự

| Thời gian gợi ý | Nội dung | Tự kiểm tra đã hiểu |
|---|---|---|
| 40 phút | Thiết kế SUC02.01: Bảng 4.179–4.186, nhất là 4.181 và 4.185 | Không nhìn tài liệu vẫn kể được các bước kiểm tra khi thêm hoạt động |
| 40 phút | Lớp và dữ liệu: Trip, TripActivity, input model, quan hệ bảng | Giải thích được một hoạt động thuộc kế hoạch nào và kế hoạch thuộc ai |
| 60 phút | Theo đường đi của POST Activity qua View → Controller → Manager → Rules → DbContext | Chỉ đúng dòng kiểm tra quyền, ngày, nguồn, phiên bản và dòng ghi dữ liệu |
| 45 phút | An toàn thông tin | Phân biệt đăng nhập, vai trò, quyền đối tượng, CSRF và kiểm tra dữ liệu |
| 45 phút | Đồng thời và giao dịch | Vẽ được hai phiên cùng đọc V1; một phiên lưu V2; phiên còn lại nhận 409 |
| 40 phút | Diễn tập báo cáo 8–10 phút, quay màn hình để xem lại | Mở đúng file nhanh, nói rõ trước/sau của dữ liệu; không sa vào đọc code |

Nếu thời gian ít, ưu tiên: `JourneyManager.Activity`, `JourneyRules.ValidateActivity`, `Version`, `AppDbContext.SaveChangesAsync` và quan hệ Trips–Activities. Không dành đêm nay để thay giao diện hoặc học toàn bộ 18 SUC.

## 3. CSDL và thiết kế lớp cần hiểu

### Dữ liệu cốt lõi

```text
Accounts (tài khoản)
   1
   │ Trips.OwnerId
   N
Trips (kế hoạch)
   1
   │ Activities.TripId
   N
Activities (hoạt động)

IncidentActivities.ActivityId → Activities.Id
```

Đây là các quan hệ liên quan trực tiếp đến phần trình bày. Một tài khoản có nhiều kế hoạch; một kế hoạch có nhiều hoạt động. `IncidentActivities` liên kết hoạt động với tình huống phát sinh và ngăn bỏ hoạt động đã được tham chiếu.

| Lớp / bảng | Trường cần giải thích | Ý nghĩa |
|---|---|---|
| Trip / Trips | Id, OwnerId | Khóa chính; tài khoản sở hữu kế hoạch |
| Trip / Trips | Name, Destination, Start, End | Thông tin chung và khoảng ngày |
| Trip / Trips | Budget: decimal? | NULL là chưa xác định ngân sách, 0 là ngân sách bằng 0 |
| Trip / Trips | Status, UpdatedAt | Nháp/Đã chấp nhận và lần cập nhật |
| Trip / Trips | Version: byte[] | SQL Server rowversion, dùng phát hiện cập nhật từ phiên cũ |
| TripActivity / Activities | Id, TripId | Khóa chính và khóa ngoại về kế hoạch |
| TripActivity / Activities | Start, End, HasTime, SortOrder | Khoảng thời gian, có xác định giờ hay chưa, thứ tự trong ngày |
| TripActivity / Activities | Title, Location, Notes, Cost | Nội dung và chi phí dự kiến; Cost cũng có thể NULL |
| TripActivity / Activities | PlaceId, TravelServiceId | Mã mục tham khảo được server kiểm tra; không phải mã đặt dịch vụ |

`Budget` và `Cost` dùng decimal(18,0) theo cấu hình DbContext. Mã hiện tại dùng tiền VND nguyên, không dùng float để biểu diễn tiền.

Tổng chi phí được tính từ `SUM(Cost ?? 0)`, không có cột tổng chi phí trên Trip. NULL được cộng như 0 phục vụ tính tổng, nhưng trên giao diện vẫn có nghĩa là chưa dự kiến, không khẳng định hoạt động miễn phí.

Nguồn lớp miền:

- [Domain.cs](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Models/Domain.cs:31>) — Trip và TripActivity.
- [JourneyFields.cs](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Models/JourneyFields.cs:6>) — phần thuộc tính bổ sung của TripActivity. `partial` chỉ là tách cùng một lớp ra nhiều file, không tạo thêm một bảng.
- [AppDbContext.cs](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Data/AppDbContext.cs:47>) — các quan hệ và cấu hình.
- [AppDbContextModelSnapshot.cs](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Data/Migrations/AppDbContextModelSnapshot.cs:1335>) — mô hình EF trong migration, không phải truy vấn xác nhận lược đồ của DB đang chạy.

Lưu ý khi nói về khóa ngoại: `TripId`, `OwnerId`, `IncidentActivities.ActivityId` có cấu hình quan hệ tương ứng. Trong mô hình EF hiện tại, `PlaceId`/`TravelServiceId` trên hoạt động mới là trường mã tham khảo, không được cấu hình FK như đặc tả đầy đủ. Không nói rằng tất cả mã tham khảo đều đã được FK bảo vệ.

### Vai trò của từng lớp trong chương trình

Ứng dụng hiện tại là ASP.NET Core MVC, có tách View, Controller, lớp xử lý nghiệp vụ và tầng truy cập dữ liệu bằng EF Core; triển khai trong một ứng dụng web. Không gọi đây là microservices.

| Thành phần | Trách nhiệm thực tế |
|---|---|
| PlanList, PlanDetails, ActivityFields | Hiển thị và gửi biểu mẫu; không quyết định quyền ghi dữ liệu |
| TripInput, ActivityInput | Hợp đồng dữ liệu nhận từ form; không bind trực tiếp toàn bộ entity |
| JourneyController | Nhận HTTP, kiểm tra binding/ModelState, lấy người hiện tại, gọi Manager, trả View/redirect |
| JourneyRules | Quy tắc thuần về nội dung, ngày, chi phí, loại nguồn; không truy cập SQL |
| JourneyManager | Quyền sở hữu, trạng thái, nguồn tham khảo, phiên bản, thay đổi entity |
| AppDbContext | EF theo dõi entity, ghi dữ liệu và nhật ký trong giao dịch |
| SQL Server LocalDB | Lưu bền dữ liệu, FK và rowversion; LocalDB là chế độ chạy SQL Server trên máy dev |

`TripInput` không có `OwnerId` hoặc `Status` để khách tự chọn. Người sở hữu lấy từ tài khoản đã xác thực; trạng thái do server đặt. Đây là một lý do dùng input model riêng.

Thiết kế lớp ở Chương 4 dùng các tên như `KeHoachChuyenDi`, `HoatDongLichTrinh`, `LapKeHoachService` và các interface. Bản hiện tại dùng Trip, TripActivity, JourneyManager; trách nhiệm của một số Controller được gộp vào JourneyController. Các phương thức miền cũng đang đặt chủ yếu trong Manager/Rules, chưa nằm trên entity và chưa hiện thực đầy đủ các interface như sơ đồ. Đây là khác biệt cấu trúc thực tế, không chỉ là đổi tên.

## 4. Luồng thêm hoạt động: phần phải nắm chắc nhất

Mở Bảng 4.181 trong **thiết kế chi tiết.docx**, rồi mở các file theo bảng dưới. Số bảng này lấy từ file thiết kế chi tiết; bản Chương 4 cũ có thể đánh số khác. Khi tìm trong Word nên tìm cả tên xử lý `themHoatDong`.

| Bước | Yêu cầu / việc xử lý | Mã cần mở |
|---|---|---|
| 1 | Form gửi TripId, Id, Version, nguồn, ngày/giờ, nội dung, chi phí | [ActivityFields.cshtml](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Views/Journey/ActivityFields.cshtml:7>) |
| 2 | Nhận POST; kiểm tra có gửi Start/End, binding, ModelState; tách lựa chọn nguồn | [JourneyController.Activity](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Controllers/JourneyController.cs:83>) |
| 3 | Tài khoản có quyền ghi; đọc đúng kế hoạch `Id == TripId && OwnerId == user` | [JourneyManager.Activity](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:126>) |
| 4 | Kiểm tra khoảng ngày, thứ tự, chi phí, độ dài, nguồn và nội dung | [JourneyRules.ValidateActivity](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyRules.cs:27>) |
| 5 | Nếu chọn nguồn: kiểm tra tồn tại/công bố; để trống tiêu đề thì dùng tên nguồn | [JourneyManager](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:148>) |
| 6 | Kiểm tra phiên bản kế hoạch mà khách gửi | [JourneyManager](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:176>) và [Version](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:448>) |
| 7 | Tạo hoặc sửa hoạt động; thêm mới tự lấy MAX thứ tự cùng ngày + 1; kế hoạch trở về Nháp, đổi UpdatedAt | [JourneyManager](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:180>) |
| 8 | Ghi thay đổi và nhật ký; thành công thì commit, lỗi thì rollback | [AppDbContext.SaveChangesAsync](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Data/AppDbContext.cs:75>) |
| 9 | Thành công 302 về chi tiết; GET tính lại tổng, cảnh báo và dựng View | [Controller](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Controllers/JourneyController.cs:83>), [Manager.Detail](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:22>) |

Nói theo luồng, không đọc từng từ của đoạn code:

> Form chỉ mang yêu cầu của người dùng. Tại Controller, em kiểm tra dữ liệu nhận được có bind hợp lệ hay không. Manager đọc kế hoạch theo cả mã kế hoạch và người sở hữu. Rules kiểm tra ngày, chi phí và nội dung. Sau khi kiểm tra nguồn tham khảo và phiên bản, em thêm hoạt động, cập nhật kế hoạch về Nháp và ghi cả hai cùng nhật ký. Lưu thành công mới chuyển hướng để nạp lại chi tiết.

Hai nhánh cần giải thích:

- **Dữ liệu không hợp lệ:** dừng trước ghi; trả lại View cùng nội dung đang soạn. Ví dụ hoạt động ngoài khoảng chuyến đi trả 422, mã 0201-03. `JourneyController.ActivityError` giữ `DraftActivity`; không redirect làm mất form.
- **Phiên bản cũ:** trả 409, mã 0201-05; khách cần xem lại phiên bản mới. Không tự ghi đè hoặc tự chấp nhận lại thay khách.

Đối chiếu thêm: Id=0 là thêm hoạt động (4.181); Id>0 là sửa (4.182). Cùng một action trong code xử lý hai nhánh. Gộp action không làm mất yêu cầu phải kiểm tra hoạt động sửa nằm trong đúng kế hoạch.

## 5. An toàn thông tin: hiểu được từng lớp bảo vệ

| Nguy cơ | Cơ chế đang có | Chỉ code ở đâu |
|---|---|---|
| Người chưa đăng nhập truy cập kế hoạch | Cookie authentication và Authorize | Program: AddAuthentication; JourneyController: Authorize |
| Nhà cung cấp gọi chức năng của khách | `Authorize(Roles="Traveler")`; Manager kiểm tra vai trò và IsActive khi ghi | JourneyController: Authorize; JourneyManager: CanWrite |
| Khách A thay ID để sửa kế hoạch khách B | Query dùng cả Id và OwnerId; hoạt động phải thuộc collection của kế hoạch đó | JourneyManager.Activity: bước 1 và 2 |
| Tự gửi OwnerId hoặc Status để chiếm quyền | Input model không có hai trường đó; server lấy Me từ claim và tự đặt trạng thái | JourneyModels: TripInput/ActivityInput; JourneyController: Me |
| Website khác lợi dụng cookie gửi POST | Antiforgery filter toàn cục; Form Tag Helper sinh token cho form POST | Program: AutoValidateAntiforgeryTokenAttribute; ActivityFields dùng `asp-action`, `method="post"` |
| Bỏ `required`, `min`, `maxlength` rồi gửi dữ liệu sai | Data Annotations, ModelState và JourneyRules ở server | JourneyModels, JourneyController.Activity, JourneyRules |
| Chèn SQL qua nội dung | Luồng này dùng truy vấn LINQ được EF tham số hóa | Các query trong JourneyManager |
| Chèn HTML/script qua tiêu đề/ghi chú | Razor xuất chuỗi qua `@...` được encode mặc định; không đưa chuỗi nhập vào Html.Raw | PlanDetails hiển thị các trường hoạt động |
| Dùng cookie cũ sau khi tài khoản bị ngừng/đổi dấu phiên | ValidatePrincipal đọc IsActive và SessionStamp từ DB | Program: OnValidatePrincipal |

Không được nhầm:

- Hộp “Bạn có chắc?” giúp tránh thao tác nhầm; không thay thế phân quyền, validation hoặc chống CSRF.
- Hidden field không phải bí mật. Version có thể nhìn thấy/sửa được, nhưng server vẫn kiểm tra. Base64 là cách biểu diễn dữ liệu nhị phân, không phải mã hóa bảo mật.
- Xác thực trả lời “ai đang dùng”; vai trò trả lời “được dùng nhóm chức năng nào”; quyền đối tượng trả lời “được thao tác kế hoạch cụ thể này không”.
- Trang đang demo bằng HTTP localhost. Không khẳng định bản này đã triển khai đầy đủ HTTPS/HSTS/CSP, tài khoản DB quyền tối thiểu hay mọi biện pháp vận hành của báo cáo. Bảo vệ cookie bằng Data Protection cũng không biến HTTP thành HTTPS.

## 6. Đồng thời và giao dịch: phần hội đồng dễ hỏi sâu

### Vấn đề mất cập nhật

1. Hai tab A và B cùng mở kế hoạch tại phiên bản V1.
2. A sửa hoạt động và lưu. DB thay đổi hoạt động, cập nhật kế hoạch và sinh rowversion mới V2.
3. B vẫn nhìn nội dung V1 và gửi Chấp nhận với V1.
4. Server thấy V1 khác phiên bản hiện tại, trả 409. B không thể chấp nhận một nội dung mới mà mình chưa xem.

Đây là **kiểm soát đồng thời lạc quan**: không khóa kế hoạch suốt thời gian khách mở trang. Khi ghi, hệ thống yêu cầu phiên bản đang sửa vẫn còn đúng.

`[Timestamp] byte[] Version` trên Trip được SQL Server ánh xạ thành rowversion 8 byte. Dù tên attribute là Timestamp, giá trị này không phải ngày giờ, không phải bộ đếm số lần người dùng sửa và không thay thế UpdatedAt. SQL Server tự sinh; ứng dụng không tự cộng 1.

### Vì sao vừa so sánh Version vừa bắt exception?

Mở hàm `Version` ở vùng “6. Hàm chung — quyền ghi, phiên bản và kết quả lỗi” trong JourneyManager:

1. Decode chuỗi Base64, kiểm tra đủ 8 byte.
2. So với Version vừa đọc từ DB; nếu khác thì từ chối sớm.
3. Gán `OriginalValue` để EF đưa phiên bản vào điều kiện UPDATE.

Vẫn có thể xuất hiện thay đổi sau bước so sánh và trước lúc ghi. Vì vậy EF phải kiểm tra tiếp khi UPDATE. Nếu không có dòng nào khớp phiên bản cũ, EF ném `DbUpdateConcurrencyException`, Manager chuyển thành 409.

Minh họa nguyên lý SQL, **không phải SQL log đã thu từ chương trình**:

```sql
UPDATE Trips
SET Status = @status, UpdatedAt = @now
WHERE Id = @id AND Version = @originalVersion;
```

### Tại sao sửa hoạt động lại phải cập nhật kế hoạch cha?

Version nằm trên Trip, còn hoạt động nằm ở bảng Activities. Nếu chỉ sửa Activities, Version của Trip không tự đổi. Code cập nhật `t.UpdatedAt` cùng trạng thái để kế hoạch cha cũng được UPDATE, nhờ đó phiên bản cha đại diện cho thay đổi của cả lịch trình.

Ví dụ hai phiên cùng thêm hoạt động: cả hai có thể tính ra cùng thứ tự tiếp theo từ dữ liệu cũ; kiểm tra phiên bản ở UPDATE kế hoạch khiến một lần ghi cũ bị từ chối, thay vì âm thầm để cả hai ghi dựa trên cùng bản đã lỗi thời.

### Giao dịch bảo vệ điều gì?

Thêm hoạt động cần INSERT Activities và UPDATE Trips. Trong request có người dùng đã xác thực, override `AppDbContext.SaveChangesAsync` mở transaction trước khi ghi nghiệp vụ, sau đó ghi AuditEntries rồi commit. Nếu ghi lỗi thì rollback transaction.

Vì vậy không được để xảy ra tình trạng “thêm hoạt động thành công nhưng cập nhật phiên bản kế hoạch thất bại mà vẫn giữ hoạt động mới”. Transaction bảo đảm các thay đổi ghi trong lần lưu đó cùng thành công hoặc cùng bị hoàn tác. FK là lớp bảo vệ quan hệ; rowversion phát hiện dữ liệu cũ; ba cơ chế có mục đích khác nhau.

Lưu ý đối chiếu: thiết kế yêu cầu Service bao cả đọc/kiểm tra/ghi trong giao dịch. Luồng lập kế hoạch hiện tại đọc và kiểm tra trước, transaction được mở trong DbContext khi SaveChanges; chưa trùng hoàn toàn cấu trúc giao dịch trong thiết kế. Rowversion vẫn bảo vệ lần ghi kế hoạch, nhưng không nên suy ra mọi thay đổi đồng thời ở tài khoản hoặc danh mục đã được bảo vệ như bản thiết kế đầy đủ. Module này cũng chưa có vòng retry deadlock riêng như mô tả thiết kế; không lấy cơ chế retry của module khác để nói thay.

### Cách minh họa đúng trên máy

- Chuẩn bị kế hoạch có một hoạt động hợp lệ. Mở cùng URL chi tiết ở hai tab **trước khi sửa**.
- Tab A sửa ghi chú hoặc chi phí, xác nhận lưu.
- Tab B giữ nguyên trang cũ, bấm Chấp nhận và xác nhận trong dialog.
- Nếu mở Network từ trước, chọn POST `/Journey/Confirm` để thấy 409 và header `X-Business-Error: 0201-05`.
- Tải lại B, đối chiếu dữ liệu mới; chấp nhận lại bằng phiên bản mới.

Hai tab cùng tài khoản đủ để minh họa phiên bản cũ. Muốn chứng minh khách B không được xem kế hoạch khách A thì phải dùng hai tài khoản với phiên đăng nhập độc lập. Minh họa hai tab chưa phải kiểm thử tải, deadlock hoặc chứng minh hai transaction đã chạy song song thật.

## 7. Kịch bản báo cáo dev khoảng 8–10 phút

| Khoảng thời gian | Màn hình cần mở | Điều cần nói / chứng minh |
|---|---|---|
| 0:00–0:45 | Thiết kế SUC02.01, Bảng 4.181 | Mục tiêu, actor, tiền điều kiện; chọn thêm hoạt động làm luồng đi sâu |
| 0:45–2:00 | Domain, JourneyModels, sơ đồ CSDL | Kế hoạch–hoạt động là 1:N; OwnerId; input khác entity; Version thuộc Trip |
| 2:00–3:00 | UI tạo nháp rồi ActivityFields và Controller.Activity | Tạo một nháp có ngày hợp lệ; chỉ trường POST, người hiện tại và bước ModelState |
| 3:00–5:00 | Bảng 4.181 cạnh Manager.Activity và Rules | Theo đúng các bước quyền → ngày/nội dung → nguồn → version → INSERT hoạt động/UPDATE kế hoạch |
| 5:00–6:00 | UI chi tiết và dữ liệu SQL | Lưu một hoạt động, nêu tổng tính lại; Trip và Activity đã được lưu; Version đổi |
| 6:00–7:00 | Program và query quyền trong Manager | Chọn giải thích quyền sở hữu và CSRF; không đọc toàn bộ cơ chế đăng nhập |
| 7:00–8:30 | Hai tab + Version + catch concurrency | Minh họa một phiên cũ nhận 409, giải thích hai lần bảo vệ phiên bản và transaction |
| 8:30–10:00 | Confirm/ChangeStatus và kết quả cuối | Chấp nhận phiên bản mới; sửa tiếp về Nháp; lưu kế hoạch không tự đặt dịch vụ |

Nếu chỉ có 5 phút: giới thiệu 20 giây, CSDL/lớp 40 giây, luồng thêm 2 phút, đồng thời/giao dịch 1 phút 20 giây, kết quả cuối 40 giây. Chuẩn bị sẵn kế hoạch, giảm thời gian nhập liệu. Phần UI phục vụ dẫn vào xử lý, không cần chạy lần lượt mọi ca lỗi của tester.

Dữ liệu mẫu dễ nói:

- Kế hoạch: `BAO_CAO_DEV_01`, Đà Lạt, 11/11/2026–13/11/2026, ngân sách 1.000.000đ. Nếu dùng vào ngày khác, đổi ngày sao cho bắt đầu không ở quá khứ.
- Hoạt động tự nhập: `Tham quan buổi sáng`, 11/11, 08:00–09:00, 300.000đ, ghi chú `Bản trước cập nhật`.
- Để minh họa đồng thời, A sửa ghi chú thành `Bản cập nhật từ tab A`; B chấp nhận trang cũ. Sau đó B tải lại và chấp nhận bản mới.
- Nếu còn thời gian, thêm hoạt động 700.001đ để cho thấy tổng 1.000.001đ có cảnh báo vượt ngân sách 1đ nhưng vẫn được chấp nhận. Không cần đưa ca này vào phần bắt buộc.

Xem DB trong PowerShell ở thư mục dự án:

```powershell
.\show-demo-data.ps1 -TripId 123
```

Thay 123 bằng ID thực trên URL chi tiết. Lệnh chỉ đọc kế hoạch, hoạt động và Version; chạy thử tối nay trong terminal của bạn. Nếu kết nối LocalDB chưa chạy được, chuẩn bị SSMS hoặc ảnh dữ liệu chụp từ lần chạy thật. Không chỉnh dữ liệu trực tiếp để tạo kết quả demo.

Nếu dùng debugger và đã quen môi trường, đặt breakpoint trong `JourneyManager.Activity` tại query đọc kế hoạch, lời gọi `Version`, cập nhật `t.Status` và `db.SaveChangesAsync`; quan sát `user`, `input.TripId`, `t.OwnerId`, `input.Version`, `t.Version`, `t.Status`, `t.Activities.Count`. Các bước đã được tách dòng và chú thích trong source. Không cần Step Into toàn bộ EF hoặc framework. Nếu chưa tập debugger thành công tối nay, dùng code + Network + truy vấn SQL; tránh cài đặt debugger lần đầu ngay trước hội đồng.

## 8. Câu hỏi cần tự trả lời được

**Vì sao không viết hết trong Controller?** Controller xử lý HTTP; Manager phối hợp nghiệp vụ và dữ liệu; Rules giữ quy tắc thuần để có thể kiểm thử không cần web/SQL. Khi thay giao diện, quy tắc vẫn dùng được.

**Vì sao không tin OwnerId gửi từ form?** Dữ liệu phía khách sửa được. Chủ sở hữu phải lấy từ danh tính đã xác thực và kiểm tra với bản ghi DB.

**Vì sao dùng 404 khi sai chủ?** Giữ cùng phản hồi với trường hợp không tồn tại, tránh tiết lộ nội dung/đối tượng của người khác. Đây là quy tắc thiết kế của SUC.

**Client đã có required rồi, vì sao server kiểm tra lại?** Client có thể bị tắt kiểm tra hoặc gửi HTTP trực tiếp. Server mới là nơi quyết định dữ liệu được ghi.

**Nháp có lưu DB chưa?** Có. Nháp là trạng thái nghiệp vụ của kế hoạch đã lưu, không phải dữ liệu chỉ nằm trong trình duyệt. Nháp vẫn phải thỏa các trường bắt buộc và khoảng ngày hợp lệ.

**Vì sao sửa kế hoạch đã chấp nhận lại về Nháp?** Khách đã chấp nhận nội dung trước đó. Sau khi thay đổi, phải xem và chấp nhận lại nội dung mới, đúng quyết định trong thiết kế.

**Vì sao cảnh báo vượt ngân sách không chặn?** Đó là thông tin giúp khách cân nhắc. Lỗi bắt buộc như hoạt động ngoài khoảng ngày mới chặn ghi. Thiết kế cho phép chấp nhận khi còn cảnh báo.

**Chấp nhận kế hoạch có đặt dịch vụ chưa?** Chưa. Activity chỉ có mã dịch vụ tham khảo; luồng này không INSERT Booking và không yêu cầu NCC xác nhận.

**SortOrder có phải tự sắp giờ không?** Không. Thêm mới xếp cuối ngày bằng MAX+1; khi sửa khách được chọn thứ tự. View nhóm theo ngày, sau đó SortOrder, Start, Id.

**Thao tác xóa có ảnh hưởng tình huống không?** Bỏ hoạt động bị chặn nếu IncidentActivities tham chiếu; có cả kiểm tra Manager và FK Restrict. Xóa toàn bộ kế hoạch chưa được cung cấp trong phạm vi hiện tại.

**Rowversion có phải khóa bản ghi khi mở trang?** Không. Nó phát hiện phiên bản cũ lúc cập nhật. Không giữ một transaction/khóa DB trong nhiều phút khách đọc trang.

**Chỉ so sánh Version trong C# đã đủ chưa?** Chưa. Giữa so sánh và ghi vẫn có thể có request khác. OriginalValue khiến EF kiểm tra tại UPDATE; exception là lớp chặn tiếp.

**Nếu INSERT hoạt động chạy rồi UPDATE cha bị xung đột?** Chúng nằm trong giao dịch ghi; lỗi làm rollback. Không dựa vào giả định EF luôn UPDATE trước INSERT.

**Có lưu lịch sử mọi phiên bản kế hoạch để khôi phục không?** Chưa. Audit có người thao tác, thực thể, mã và hành động. Bản này không lưu đầy đủ snapshot trước/sau của Trip và không có khôi phục lịch trình cũ.

**Tester kiểm tra phần dev này thế nào?** Unit cho Rules; integration cho quyền, nguồn, FK và rowversion; system/UAT cho luồng sử dụng. Có thể mở JourneyRulesTests để minh họa một ca đầu vào ngoài khoảng ngày, rồi quay về quy tắc tương ứng. Không nhận kết quả tester chưa chạy là đã pass.

## 9. Các điểm cần nói đúng khi đối chiếu thiết kế

Không tuyên bố bản hiện tại khớp 100% thiết kế. Các khác biệt sau đã nhìn thấy trong source và tài liệu:

| Điểm | Thiết kế / tài liệu | Mã hiện tại | Cách trình bày đúng |
|---|---|---|---|
| Tổ chức lớp | Nhiều Controller, Service/interface; entity có phương thức nghiệp vụ | Gộp JourneyController, JourneyManager; quy tắc ở Rules/Manager | Có ánh xạ trách nhiệm nhưng chưa trùng cấu trúc lớp đã thiết kế |
| Ngày/giờ hoạt động | Ngay + Gio tùy chọn | Start, End, HasTime | Đây là khác biệt mô hình dữ liệu, không chỉ đổi tên |
| Trùng giờ | Cùng ngày và cùng giờ bắt đầu | Hai khoảng giao nhau khi cùng ngày bắt đầu, có HasTime | Tester cần ghi nhận chênh lệch quy tắc; không nói đã khớp hoàn toàn |
| Giao dịch | Service bao cả đọc/kiểm tra/ghi | Đọc/kiểm tra ở Manager; transaction ghi mở trong DbContext | Nêu đúng chỗ bắt đầu transaction và giới hạn của nó |
| Sinh schema | Chương 4 mô tả database-first, script là nguồn chính | DatabaseBootstrap chạy EF migrations | Không nói đang hiện thực database-first đúng nguyên thiết kế |
| Ràng buộc vật lý | Tên, kiểu, CHECK, FK, chỉ mục theo đặc tả | Có phần mới kiểm tra ở ứng dụng; nhiều chuỗi là nvarchar(max), mã int; FK nguồn tham khảo chưa đủ | Không dùng số ràng buộc/ca test trong báo cáo làm bằng chứng DB hiện tại |
| An toàn vận hành | HTTPS/HSTS, CSP, DB quyền tối thiểu… | Bản demo localhost HTTP, cấu hình dev | Phân biệt yêu cầu thiết kế với biện pháp đã triển khai/chưa kiểm chứng |

Hai file bàn giao cũ `LAP_KE_HOACH.md` (KH20) và `DOI_CHIEU_THIET_KE.md` vẫn mô tả quy tắc cùng giờ bắt đầu; code đã tính giao khoảng cùng ngày bắt đầu. Tài liệu ôn tập này ghi nhận chênh lệch, không tự đổi tiêu chí của tester hoặc sửa bản sản phẩm đã chốt. Tương tự, hướng dẫn cũ yêu cầu hai cookie jar cho mọi ca đồng thời là quá chặt: ca phiên bản cũ có thể dùng hai tab cùng tài khoản; kiểm tra quyền giữa hai người mới cần hai phiên đăng nhập độc lập.

## 10. Checklist tự luyện cuối buổi tối

- Mở sẵn file thiết kế chi tiết tại Bảng 4.181 và 4.185; Chương 4 tại mô hình lớp/CSDL/an toàn thông tin/đồng thời.
- Đánh dấu các file thật đang được Controller trả về: PlanList, PlanDetails, ActivityFields. Trong thư mục có View cũ tên Index/Details; không lấy chúng làm màn hình đang chạy.
- Tăng cỡ chữ editor và trình duyệt để hội đồng đọc được. Chỉ mở đoạn liên quan, không cuộn tìm cả dự án trước hội đồng.
- Thử một lần tạo → thêm → sửa → chấp nhận; ghi lại đúng TripId, số hoạt động, tổng tiền và trạng thái.
- Thử hai tab trước; quan sát 409 và bản mới không bị mất. Mỗi lần thử lại phải tải hai tab trước khi A sửa.
- Chạy lệnh xem dữ liệu bằng ID thật, chụp minh chứng từ lần chạy thật nếu cần phương án dự phòng.
- Tự nói liền mạch 90 giây về Activity mà không đọc tài liệu; nói thêm 60 giây về Version và transaction.
- Biết rõ câu nào là yêu cầu thiết kế, câu nào là hiện thực, câu nào là giới hạn còn cần tester ghi nhận.

Bạn đạt mức chuẩn bị cần thiết khi chỉ vào được dòng code cho mỗi câu: **Ai có quyền? Dữ liệu được kiểm tra ở đâu? Ghi những bảng nào? Nếu lỗi giữa chừng thì sao? Nếu hai phiên cùng sửa thì sao? Điểm nào hiện chưa khớp thiết kế?**
