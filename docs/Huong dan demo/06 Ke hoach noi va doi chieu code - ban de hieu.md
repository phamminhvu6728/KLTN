# Kế hoạch demo và đối chiếu code — bản dễ hiểu

Phần này chỉ dùng để demo SUC02.01 — Lập kế hoạch chuyến đi. Mỗi bước ghi rõ: file nào, dòng nào, nhìn đoạn nào và nói câu gì.

Lưu ý: số dòng dưới đây là số dòng của source hiện tại (05/11/2026). Nếu máy bạn lệch dòng, mở đúng file rồi nhấn Ctrl+F tìm tên hàm; không cần đọc cả file.

## 1. Bản đồ xử lý

Giao diện Razor → JourneyController → JourneyManager/JourneyRules → AppDbContext → SQL Server LocalDB.

| Thành phần | Đường dẫn |
|---|---|
| Form tạo kế hoạch | Views/Journey/PlanList.cshtml |
| Form thêm hoạt động | Views/Journey/ActivityFields.cshtml |
| Controller | Controllers/JourneyController.cs |
| Quy tắc dữ liệu | Services/JourneyRules.cs |
| Xử lý nghiệp vụ | Services/JourneyManager.cs |
| Entity Trip/TripActivity | Models/Domain.cs |
| CSDL và transaction | Data/AppDbContext.cs |

Trong VS Code: Ctrl+P mở file, Ctrl+G nhảy đến dòng, Ctrl+Shift+O tìm tên hàm.

## 2. Kịch bản nói 4–5 phút

### 0:00–0:20 — Mở slide thiết kế

Mở slide 37–38, sơ đồ tuần tự SUC02.01.

Nói:

> Em trình bày chức năng lập kế hoạch chuyến đi. Luồng demo gồm tạo kế hoạch nháp, thêm hoạt động và chấp nhận kế hoạch. Em sẽ đối chiếu sơ đồ thiết kế với các file Controller, Rules, Manager và DbContext.

### 0:20–1:00 — Tạo kế hoạch nháp

Mở website /Journey, nhập thông tin và bấm Tạo kế hoạch nháp.

Mở Views/Journey/PlanList.cshtml, dòng 15. Đây là form có asp-action="Save".

Mở Controllers/JourneyController.cs, dòng 66:

    public async Task<IActionResult> Save(TripInput input)

Nói:

> Form gửi dữ liệu đến action Save. Controller nhận form và gọi lớp xử lý nghiệp vụ.

Mở Services/JourneyManager.cs, dòng 68 (hàm xử lý nghiệp vụ):

    public async Task<JourneyOutcome> Save(int user, TripInput input)

Dòng 73 gọi kiểm tra dữ liệu:

    var invalid = JourneyRules.ValidateTrip(input);

Dòng 108 đặt trạng thái:

    t.Status = "Nháp";

Nói:

> Manager kiểm tra quyền và thông tin kế hoạch. Nếu hợp lệ, hệ thống tạo bản ghi Trip ở trạng thái Nháp. Phần này đối chiếu Bảng 4.179 — khởi tạo kế hoạch.

### 1:00–1:20 — Xem chi tiết

Mở trang chi tiết kế hoạch. Trong Services/JourneyManager.cs, hàm Detail nằm khoảng dòng 22.

Nói:

> Manager đọc kế hoạch kèm hoạt động và lọc theo Id cùng OwnerId. Vì vậy khách chỉ xem được kế hoạch của mình. Tổng tiền và cảnh báo được tính khi mở chi tiết. Đây là Bảng 4.180 — xem kế hoạch.

### 1:20–2:50 — Thêm hoạt động

Nhập Tham quan buổi sáng, 08:00–09:00, chi phí 300000, rồi bấm Thêm vào lịch trình.

Mở Views/Journey/ActivityFields.cshtml, dòng 7:

    <form asp-action="Activity" method="post">

Các trường quan trọng:

    dòng 10: TripId
    dòng 11: Version
    dòng 34: Title
    dòng 41: Start
    dòng 45: End

Mở Controllers/JourneyController.cs, dòng 84:

    public async Task<IActionResult> Activity(ActivityInput input)

Nói:

> Controller nhận dữ liệu hoạt động, kiểm tra ModelState rồi chuyển yêu cầu sang Manager.

Mở Services/JourneyRules.cs, dòng 27:

    public static string ValidateActivity(ActivityInput input, Trip trip)

Nói:

> Rules kiểm tra ngày hoạt động nằm trong chuyến đi, giờ kết thúc sau giờ bắt đầu, chi phí và nội dung hợp lệ.

Mở Services/JourneyManager.cs, dòng 126:

    public async Task<JourneyOutcome> Activity(int user, ActivityInput input)

Đối chiếu các đoạn:

| Dòng | Giải thích |
|---:|---|
| 128–134 | Kiểm tra tài khoản và kế hoạch thuộc người dùng |
| 139 | Gọi ValidateActivity |
| 145–166 | Kiểm tra địa điểm/dịch vụ tham khảo |
| 170 | Kiểm tra Version |
| 173–201 | Tạo hoặc sửa hoạt động |
| 211 | Đưa kế hoạch về Nháp |
| 216 | Ghi xuống DB |

Tại dòng 211:

    t.Status = "Nháp";

Nói:

> Manager kiểm tra quyền, dữ liệu, nguồn tham khảo và Version. Nếu hợp lệ, hệ thống thêm hoạt động vào Activities, cập nhật kế hoạch về Nháp và ghi dữ liệu. Phần này đối chiếu Bảng 4.181 — thêm hoạt động. Nếu Id hoạt động lớn hơn 0 thì xử lý sửa hoạt động theo Bảng 4.182.

### 2:50–3:30 — Đối chiếu sơ đồ tuần tự

Quay lại slide 37–38.

Nói:

> Sơ đồ trên slide thể hiện khách gửi form, Controller tiếp nhận, lớp xử lý kiểm tra nghiệp vụ, Entity thay đổi và CSDL ghi dữ liệu. Trong code, các thành phần tương ứng là ActivityFields, JourneyController.Activity, JourneyManager.Activity, TripActivity và AppDbContext.

Nói thêm:

> Tên lớp trong source dùng tiếng Anh và một số action được gom vào JourneyController/JourneyManager. Em đối chiếu theo trách nhiệm xử lý, không khẳng định tên lớp trùng tuyệt đối với slide.

### 3:30–4:10 — Version và transaction

Mở Services/JourneyManager.cs, dòng 448:

    bool Version<T>(T entity, string? version)

Mở Models/Domain.cs, dòng 48:

    [Timestamp]
    public byte[] Version { get; set; } = [];

Nói:

> Version giúp phát hiện phiên bản cũ. Nếu tab khác đã lưu trước thì Version không khớp và hệ thống trả lỗi 409.

Mở Data/AppDbContext.cs, dòng 75:

    public override async Task<int> SaveChangesAsync(...)

Nói:

> SaveChangesAsync ghi các thay đổi trong transaction. Nếu ghi lỗi, hệ thống rollback để không giữ dữ liệu dở dang.

Đối chiếu phần này với slide thiết kế xử lý đồng thời và sơ đồ tuần tự slide 37–38.

### 4:10–4:45 — Chấp nhận kế hoạch

Trên trang chi tiết bấm Chấp nhận kế hoạch.

Mở Views/Journey/PlanDetails.cshtml, dòng 34, form có asp-action="Confirm".

Mở Controllers/JourneyController.cs, dòng 118:

    public async Task<IActionResult> Confirm(int id, string version)

Mở Services/JourneyManager.cs, dòng 264–270:

    Confirm(...) → ChangeStatus(..., "Đã chấp nhận")

Nói:

> Hệ thống kiểm tra lại quyền, Version và dữ liệu trước khi chuyển kế hoạch sang Đã chấp nhận. Đây là Bảng 4.185 — chấp nhận kế hoạch. Chấp nhận chỉ lưu lịch trình dự kiến, chưa tạo Booking.

## 3. Bảng đối chiếu để học thuộc

| Thiết kế | File và dòng | Nói ngắn gọn |
|---|---|---|
| 4.179 Khởi tạo | JourneyController.cs:66; JourneyManager.cs:68 | Tạo kế hoạch Nháp |
| 4.180 Xem | JourneyManager.cs:22 | Đọc đúng kế hoạch của chủ |
| 4.181 Thêm | JourneyController.cs:84; JourneyManager.cs:126 | Kiểm tra rồi thêm hoạt động |
| 4.182 Sửa | JourneyManager.cs:173–201 | Id hoạt động lớn hơn 0 thì sửa |
| 4.183 Bỏ | JourneyController.cs:113; JourneyManager.cs:226 | Bỏ hoạt động nếu không bị tham chiếu |
| 4.184 Cập nhật | JourneyController.cs:66; JourneyManager.cs:68 | Id kế hoạch lớn hơn 0 thì sửa kế hoạch |
| 4.185 Chấp nhận | JourneyController.cs:118; JourneyManager.cs:264 | Đổi sang Đã chấp nhận |
| 4.186 Lưu nháp | JourneyController.cs:123; JourneyManager.cs:265 | Đưa về Nháp |

## 4. Câu hỏi cần trả lời

**Controller làm gì?** Nhận request, kiểm tra ModelState, gọi Manager, trả View hoặc redirect.

**Manager làm gì?** Kiểm tra quyền sở hữu, quy tắc nghiệp vụ, Version và thay đổi dữ liệu.

**Rules làm gì?** Kiểm tra dữ liệu thuần như ngày, giờ, chi phí và nội dung; không truy cập DB.

**Vì sao cần OwnerId?** Người dùng có thể sửa Id trên form. Server phải kiểm tra thêm chủ sở hữu để ngăn sửa kế hoạch người khác.

**Vì sao thêm hoạt động lại thành Nháp?** Vì nội dung kế hoạch đã thay đổi, khách phải xem và chấp nhận lại.

**Version là gì?** Rowversion do SQL Server sinh để ngăn phiên bản cũ ghi đè phiên bản mới.

**Transaction là gì?** Các thay đổi trong một lần ghi cùng thành công hoặc cùng rollback.

**Chấp nhận kế hoạch có đặt dịch vụ không?** Không. Đặt dịch vụ thuộc SUC07.01; SUC02.01 chỉ lưu kế hoạch dự kiến.

## 5. Cách mở đúng file khi báo cáo

1. Thêm hoạt động: JourneyController.cs dòng 84.
2. Kiểm tra dữ liệu: JourneyRules.cs dòng 27.
3. Xử lý nghiệp vụ: JourneyManager.cs dòng 126.
4. Trạng thái Nháp sau khi thêm: JourneyManager.cs dòng 211.
5. Version: JourneyManager.cs dòng 448.
6. Entity Version: Domain.cs dòng 48.
7. Ghi transaction: AppDbContext.cs dòng 75.
8. Chấp nhận: JourneyController.cs dòng 118 và JourneyManager.cs dòng 264.

## 6. Câu kết

> Em đã đối chiếu được các bước trong thiết kế xử lý chi tiết với code hiện tại: form gửi dữ liệu, Controller tiếp nhận, Rules kiểm tra, Manager xử lý nghiệp vụ, DbContext ghi dữ liệu và giao diện hiển thị kết quả.

## 7. Nếu hội đồng hỏi về database

### 7.1. Trình bày thiết kế trước khi nói câu lệnh

Nói theo thứ tự sau:

> Em thiết kế dữ liệu theo mô hình quan hệ. Mỗi class trong `Models` là một entity và được Entity Framework ánh xạ thành bảng thông qua `AppDbContext`. Trong phạm vi lập kế hoạch, `Account` là người sở hữu; `Trip` là kế hoạch; `TripActivity` là các hoạt động nằm trong kế hoạch. Một Account có nhiều Trip, một Trip có nhiều TripActivity. Khóa ngoại `Trip.OwnerId` xác định chủ kế hoạch, còn `TripActivity.TripId` xác định hoạt động thuộc kế hoạch nào. Hoạt động có thể tham chiếu thêm `Place` hoặc `TravelService`, nhưng vẫn có thể là hoạt động tự nhập.

Sơ đồ nói nhanh:

    Account (1) ───< Trip (1) ───< TripActivity
                         ├── Place (tham khảo, tùy chọn)
                         └── TravelService (tham khảo, tùy chọn)

Đối chiếu source:

| Thành phần thiết kế | Class/file | Ý nghĩa |
|---|---|---|
| Người dùng | `Models/Domain.cs:5–17` — `Account` | Tài khoản, vai trò và trạng thái hoạt động |
| Kế hoạch | `Models/Domain.cs:31–53` — `Trip` | Tên, điểm đến, ngày, ngân sách, trạng thái |
| Hoạt động | `Models/Domain.cs:55–68` và `Models/JourneyFields.cs:5–12` | Nội dung, thời gian, chi phí, thứ tự, nguồn tham khảo |
| Ánh xạ bảng | `Data/AppDbContext.cs:10–20` | DbSet tương ứng với các bảng |
| Khóa ngoại | `Data/AppDbContext.cs:55–62` | Trip gắn với Account; dịch vụ gắn với Account |

Sau đó mới nói về cách hệ thống kết nối và ghi dữ liệu bên dưới.

### 7.2. Bài nói chi tiết khoảng 2–3 phút

Bạn có thể nói gần như nguyên văn:

> Trước hết, về thiết kế database, em sử dụng mô hình cơ sở dữ liệu quan hệ và triển khai theo hướng Code First với Entity Framework Core. Nghĩa là em mô tả cấu trúc dữ liệu bằng các class model trong thư mục `Models`; từ các model và migration, Entity Framework tạo hoặc cập nhật các bảng trong SQL Server LocalDB.
>
> Entity trung tâm của chức năng này là `Trip`, tương ứng với một kế hoạch chuyến đi. `Trip` có khóa chính `Id`, khóa ngoại `OwnerId` để biết kế hoạch thuộc tài khoản nào, cùng các thuộc tính tên chuyến đi, điểm đến, ngày bắt đầu, ngày kết thúc, ngân sách và trạng thái. Trạng thái ban đầu là `Nháp`; khi người dùng chấp nhận thì chuyển thành `Đã chấp nhận`.
>
> Một `Trip` có nhiều `TripActivity`. Mỗi hoạt động có khóa chính `Id` và khóa ngoại `TripId`, nên database biết hoạt động nào thuộc kế hoạch nào. Hoạt động lưu tiêu đề, địa điểm, ghi chú, thời gian bắt đầu, thời gian kết thúc và chi phí. Các thuộc tính `PlaceId`, `TravelServiceId`, `SortOrder` và `HasTime` được tách trong file `JourneyFields.cs`, nhưng nhờ khai báo `partial class` nên vẫn là cùng entity `TripActivity` và vẫn lưu chung trong bảng `Activities`.
>
> Quan hệ chính là một `Account` có nhiều `Trip`, một `Trip` có nhiều `TripActivity`. Hoạt động có thể tham chiếu đến `Place` hoặc `TravelService` nếu người dùng chọn dữ liệu có sẵn; hai trường này là tùy chọn vì người dùng cũng có thể tự nhập hoạt động.
>
> Các quan hệ và ràng buộc được cấu hình tại `AppDbContext`. Ở đây `DbSet<Trip>` và `DbSet<TripActivity>` đại diện cho các bảng, còn cấu hình `HasForeignKey` đảm bảo khóa ngoại liên kết đúng. Những ràng buộc này giúp không lưu hoạt động không thuộc kế hoạch và hạn chế xóa dữ liệu đang được tham chiếu.
>
> Cuối cùng, mỗi entity có `Id` làm khóa chính. `Trip` có thêm trường `Version` với thuộc tính `Timestamp`. SQL Server tự sinh giá trị rowversion này để phát hiện trường hợp hai cửa sổ cùng sửa một kế hoạch; phiên bản cũ sẽ bị từ chối thay vì ghi đè dữ liệu mới.

Khi vừa nói vừa mở code, đi theo thứ tự: `Domain.cs:5` → `Domain.cs:31` → `Domain.cs:55` → `JourneyFields.cs:5` → `AppDbContext.cs:10` → `AppDbContext.cs:55` → `Domain.cs:47–49`.

### Câu trả lời 40 giây

> Hệ thống dùng SQL Server LocalDB với database `TuTucDemo`, truy cập bằng Entity Framework Core theo hướng Code First. `AppDbContext` khai báo các DbSet và quan hệ giữa các bảng. Với chức năng lập kế hoạch, bảng chính là `Trips`, bảng hoạt động là `Activities`; mỗi hoạt động có `TripId` liên kết về một kế hoạch, còn `OwnerId` liên kết tài khoản sở hữu. Khi người dùng lưu, Manager thay đổi entity rồi gọi `SaveChangesAsync`. DbContext ghi dữ liệu và audit trong transaction; nếu lỗi thì rollback. Trường `Version` là rowversion để không cho dữ liệu cũ ghi đè dữ liệu mới.

### Mở đúng file để đối chiếu

| Nội dung bị hỏi | File và dòng | Nói ngắn gọn |
|---|---|---|
| Chuỗi kết nối | `appsettings.json:1` | SQL Server LocalDB, database `TuTucDemo` |
| Đăng ký EF Core | `Program.cs:20` | Ứng dụng dùng `AppDbContext` với SQL Server |
| Các bảng chính | `Data/AppDbContext.cs:10–20` | DbSet cho Accounts, Trips, Activities, Services... |
| Entity kế hoạch | `Models/Domain.cs:31–53` | Trip chứa chủ sở hữu, ngày, ngân sách, trạng thái |
| Entity hoạt động | `Models/Domain.cs:55–68` | TripActivity liên kết với Trip bằng `TripId` |
| Quan hệ/ràng buộc | `Data/AppDbContext.cs:22–62` | Cấu hình khóa ngoại và không xóa dây chuyền tùy tiện |
| Ghi dữ liệu | `Data/AppDbContext.cs:75–106` | Ghi nghiệp vụ trong transaction |
| Migration | `Data/Migrations/20261102092927_InitialSUC.cs` | Lịch sử tạo/cập nhật schema |

### Câu hỏi thường gặp

**Vì sao dùng EF Core thay vì viết SQL trực tiếp?** Ứng dụng thao tác bằng entity và LINQ, giảm SQL thủ công; migration giúp version hóa schema.

**Quan hệ chính của lập kế hoạch là gì?** Một `Account` có nhiều `Trip`; một `Trip` có nhiều `TripActivity`. `TripActivity` có thể tham chiếu `Place` hoặc `TravelService` nếu chọn nguồn có sẵn.

**Có xóa kế hoạch trực tiếp không?** Hiện luồng demo tập trung tạo, sửa, thêm/bỏ hoạt động và đổi trạng thái; việc xóa phải kiểm tra các bản ghi tham chiếu trước để không phá khóa ngoại và dữ liệu nghiệp vụ.

**Database có lưu mật khẩu thô không?** Không, tài khoản lưu `PasswordHash` trong `Models/Domain.cs`; không lưu mật khẩu nguyên văn.
