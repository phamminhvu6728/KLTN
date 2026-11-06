# Kịch bản demo và giải thích của dev
## SUC02.01 — Lập kế hoạch chuyến đi

Thời lượng: khoảng 4–5 phút.

Phạm vi: chỉ trình bày chức năng lập kế hoạch chuyến đi. Mỗi bước đều đối chiếu với slide thiết kế xử lý chi tiết. Không trình bày slide yêu cầu phi chức năng trong phần demo này.

## A. Chuẩn bị trước khi trình bày

Mở sẵn các cửa sổ sau:

1. Slide 34–38 của file báo cáo:
   - Slide 34–36: phần thiết kế xử lý chi tiết.
   - Slide 37–38: sơ đồ tuần tự SUC02.01 — Lập kế hoạch chuyến đi.
2. Website tại `/Journey`.
3. Các file code cần mở:
   - `Views/Journey/ActivityFields.cshtml`
   - `Controllers/JourneyController.cs`
   - `Services/JourneyRules.cs`
   - `Services/JourneyManager.cs`
   - `Data/AppDbContext.cs`
4. Nếu có thời gian, mở thêm slide 42–43 để chỉ SUC02.01 trong danh sách SUC. Không mở slide 44 vì đó là phần yêu cầu phi chức năng.

Dữ liệu demo nên chuẩn bị trước:

- Tên kế hoạch: `Bao cao dev`
- Điểm đến: `Đà Lạt`
- Ngày: chọn một khoảng ngày tương lai phù hợp với ngày báo cáo
- Ngân sách: `1.000.000`
- Hoạt động: `Tham quan buổi sáng`
- Thời gian: `08:00–09:00`
- Chi phí: `300.000`

## B. Kịch bản nói 4–5 phút

### 0:00–0:25 — Xác định phạm vi trên slide

Mở slide 37 hoặc slide 38 có tiêu đề sơ đồ tuần tự SUC02.01.

Nói:

> “Em trình bày SUC02.01 — Lập kế hoạch chuyến đi. Phần demo đi theo luồng tạo kế hoạch nháp, thêm hoạt động, xem lại và chấp nhận kế hoạch. Em sẽ đối chiếu từng bước trên sơ đồ xử lý chi tiết với mã nguồn thực tế và kết quả trên giao diện.”

Chỉ trên slide:

- Khách du lịch là tác nhân thực hiện.
- Luồng chính đi qua giao diện, lớp xử lý và cơ sở dữ liệu.
- Kết quả cuối của chức năng là một kế hoạch được lưu và có thể chấp nhận.

Đối chiếu cần nói:

> “Phần này tương ứng với SUC02.01 trong slide SUC và với các bảng xử lý 4.176 đến 4.186 trong thiết kế chi tiết.”

### 0:25–0:55 — Tạo kế hoạch nháp

Chuyển sang `/Journey`, nhập thông tin và bấm **Tạo kế hoạch nháp**.

Mở nhanh `JourneyController.Save`.

Nói:

> “Theo bước khởi tạo kế hoạch trong thiết kế, hệ thống nhận tên, điểm đến, ngày bắt đầu, ngày kết thúc và ngân sách. Controller nhận form, kiểm tra dữ liệu đầu vào rồi gọi lớp xử lý nghiệp vụ. Manager kiểm tra quyền của khách và quy tắc ngày trước khi tạo Trip với trạng thái Nháp.”

Đối chiếu slide:

| Thiết kế | Code hiện tại | Kết quả demo |
|---|---|---|
| Bảng 4.179 — khởi tạo kế hoạch | `JourneyController.Save` → `JourneyManager.Save` | Kế hoạch xuất hiện ở danh sách |
| Trạng thái ban đầu là Nháp | `t.Status = "Nháp"` | Trang chi tiết hiển thị Nháp |
| Lưu chủ sở hữu kế hoạch | `OwnerId = user` | Chỉ tài khoản hiện tại xem được |

Nếu bị hỏi “Nháp có lưu thật không?”, trả lời:

> “Có. Nháp đã được lưu trong bảng Trips, không chỉ nằm trong trình duyệt. Người dùng có thể mở lại và tiếp tục thêm hoạt động.”

### 0:55–1:20 — Mở chi tiết kế hoạch

Mở trang chi tiết kế hoạch.

Mở `JourneyManager.Detail`.

Nói:

> “Đây là bước xem kế hoạch. Manager đọc kế hoạch kèm danh sách hoạt động, nhưng luôn lọc theo cả Id và OwnerId. Sau khi đọc dữ liệu, hệ thống tính tổng chi phí và các cảnh báo tại thời điểm hiển thị.”

Đối chiếu slide:

| Thiết kế | Code hiện tại | Kết quả demo |
|---|---|---|
| Bảng 4.180 — xem kế hoạch | `JourneyController.Details` → `JourneyManager.Detail` | Hiển thị ngày, hoạt động, tổng tiền |
| Kiểm tra đúng chủ sở hữu | `SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == user)` | Kế hoạch người khác trả 404 |
| Tính tổng chi phí | `Sum(x => x.Cost ?? 0)` | Tổng chi phí hiển thị trên trang |

### 1:20–2:50 — Thêm hoạt động, phần đối chiếu chính

Trong form chi tiết, nhập hoạt động rồi bấm **Thêm vào lịch trình**. Khi hộp xác nhận xuất hiện, xác nhận thao tác.

Mở lần lượt:

1. `ActivityFields.cshtml`
2. `JourneyController.Activity`
3. `JourneyRules.ValidateActivity`
4. `JourneyManager.Activity`

Nói theo đúng trình tự:

> “Theo Bảng 4.181 trên slide thiết kế, khi thêm hoạt động hệ thống nhận mã kế hoạch, mã phiên bản, ngày, giờ, loại hoạt động, nội dung và chi phí.”

> “Ở giao diện, form gửi các trường này. Controller kiểm tra binding và ModelState, sau đó chuyển lựa chọn nguồn tham khảo thành SourceType và SourceId.”

> “Manager kiểm tra kế hoạch có thuộc khách đang đăng nhập không. Rules kiểm tra ngày hoạt động có nằm trong khoảng chuyến đi, giờ kết thúc có sau giờ bắt đầu, chi phí và nội dung có hợp lệ không.”

> “Nếu hoạt động lấy từ địa điểm hoặc dịch vụ, Manager kiểm tra nguồn đó còn tồn tại và được công bố. Nếu người dùng bỏ trống tiêu đề, hệ thống dùng tên của nguồn tham khảo.”

> “Sau đó hệ thống kiểm tra Version. Nếu đúng, hoạt động được thêm vào Activities, thứ tự mới được lấy bằng thứ tự lớn nhất trong cùng ngày cộng một. Kế hoạch cha được đưa về Nháp và cập nhật thời gian sửa.”

Đối chiếu trực tiếp trên slide:

| Bước trên thiết kế 4.181 | Vị trí trong code | Điều cần chỉ |
|---|---|---|
| Nhận dữ liệu form | `ActivityFields.cshtml` | TripId, Version, Title, Start, End, Cost |
| Kiểm tra dữ liệu đầu vào | `JourneyController.Activity` | `ModelState.IsValid` |
| Kiểm tra ngày, giờ, chi phí | `JourneyRules.ValidateActivity` | Điều kiện ngày và miền giá trị |
| Kiểm tra kế hoạch thuộc khách | `JourneyManager.Activity` | Query có `TripId` và `OwnerId` |
| Kiểm tra nguồn tham khảo | `JourneyManager.Activity` | Nhánh Place/Service |
| Kiểm tra phiên bản | `JourneyManager.Version` | So sánh rowversion 8 byte |
| Thêm hoạt động và cập nhật kế hoạch | `JourneyManager.Activity` | `t.Activities.Add`, `t.Status`, `t.UpdatedAt` |
| Ghi dữ liệu | `AppDbContext.SaveChangesAsync` | Transaction, commit hoặc rollback |

Chỉ kết quả trên giao diện:

> “Kết quả là hoạt động đã xuất hiện trong đúng ngày, tổng chi phí tăng lên 300.000 đồng và kế hoạch vẫn ở trạng thái Nháp.”

### 2:50–3:30 — Đối chiếu sơ đồ tuần tự

Quay lại slide 37–38.

Nói:

> “Nếu đối chiếu với sơ đồ tuần tự, thao tác vừa thực hiện đi theo chuỗi: khách gửi form, Controller tiếp nhận, Manager kiểm tra nghiệp vụ, AppDbContext ghi Trips và Activities, sau đó hệ thống trả kết quả về trang chi tiết.”

Chỉ lần lượt các thành phần trên sơ đồ rồi chỉ file tương ứng:

| Thành phần trên sơ đồ | Thành phần code |
|---|---|
| Boundary / giao diện | `ActivityFields.cshtml`, `PlanDetails.cshtml` |
| Controller | `JourneyController.Activity` |
| Service/Control | `JourneyManager.Activity`, `JourneyRules` |
| Entity | `Trip`, `TripActivity` |
| CSDL | `AppDbContext`, SQL Server LocalDB |

Nói thêm một câu trung thực:

> “Tên lớp trong mã nguồn hiện tại là tiếng Anh và một số action được gom vào JourneyController/JourneyManager, còn slide thiết kế dùng tên nghiệp vụ tiếng Việt. Em đối chiếu theo trách nhiệm xử lý, không khẳng định tên lớp trùng tuyệt đối.”

### 3:30–4:10 — Đồng thời và an toàn dữ liệu

Mở vùng `Version` trong `JourneyManager` và `SaveChangesAsync` trong `AppDbContext`.

Nói:

> “Thiết kế yêu cầu không cho phiên bản cũ ghi đè nội dung mới. Version của Trip được gửi kèm form. Manager so sánh Version với bản vừa đọc, sau đó EF tiếp tục dùng OriginalValue trong điều kiện UPDATE. Nếu có tab khác lưu trước, phiên cũ nhận lỗi 409.”

> “Khi ghi hoạt động và cập nhật kế hoạch cha, DbContext mở transaction. Nếu một bước ghi thất bại, transaction rollback để không giữ dữ liệu dở dang.”

Đối chiếu slide:

| Nội dung thiết kế | Code hiện tại |
|---|---|
| Chống ghi đè phiên bản cũ | `Trip.Version` và hàm `Version` |
| Xử lý lỗi đồng thời | `DbUpdateConcurrencyException` trả 409 |
| Ghi cùng một lần lưu | `AppDbContext.SaveChangesAsync` |
| Kiểm tra quyền | `[Authorize(Roles = "Traveler")]` và `CanWrite` |
| Chống request POST giả mạo | `AutoValidateAntiforgeryTokenAttribute` |

### 4:10–4:45 — Chấp nhận kế hoạch và kết thúc

Quay lại trang chi tiết, bấm **Chấp nhận kế hoạch**, xác nhận.

Mở `JourneyManager.Confirm` hoặc `ChangeStatus`.

Nói:

> “Theo Bảng 4.185, khi chấp nhận, hệ thống kiểm tra lại quyền, Version và dữ liệu ngày trước khi đổi trạng thái sang Đã chấp nhận. Cảnh báo vượt ngân sách hoặc trùng giờ chỉ giúp khách cân nhắc, không tự chặn thao tác chấp nhận.”

> “Kết quả cuối là kế hoạch được lưu ở trạng thái Đã chấp nhận. Đây vẫn là kế hoạch dự kiến, chưa tạo Booking và chưa có nghĩa nhà cung cấp đã xác nhận dịch vụ.”

Kết thúc:

> “Như vậy em đã đối chiếu được luồng trên slide thiết kế với giao diện, Controller, Manager, Rules, DbContext và kết quả thực tế của chức năng lập kế hoạch.”

## C. Nếu hội đồng hỏi thêm

### 1. Vì sao không để toàn bộ xử lý trong Controller?

> “Controller chỉ tiếp nhận HTTP và trả kết quả. Manager điều phối nghiệp vụ và truy cập dữ liệu. Rules giữ các quy tắc thuần nên có thể kiểm thử riêng, không cần chạy trình duyệt.”

### 2. Vì sao phải kiểm tra OwnerId?

> “Id trên form có thể bị sửa. Server không tin Id đơn lẻ mà truy vấn đồng thời Id và OwnerId lấy từ claim đăng nhập. Vì vậy khách không thể đổi Id để sửa kế hoạch của người khác.”

### 3. Vì sao kiểm tra cả client và server?

> “Kiểm tra trên giao diện giúp người dùng nhập thuận tiện. Nhưng request có thể được gửi trực tiếp, nên server vẫn phải kiểm tra ngày, chi phí, nội dung và quyền.”

### 4. Vì sao cảnh báo không chặn chấp nhận?

> “Trùng giờ hoặc vượt ngân sách là thông tin để khách tự quyết định. Dữ liệu sai như ngày ngoài chuyến đi hoặc giờ kết thúc không hợp lệ mới bị chặn.”

### 5. Chấp nhận kế hoạch có đặt dịch vụ không?

> “Không. Hoạt động có thể tham chiếu một dịch vụ để lập lịch, nhưng SUC02.01 không tạo Booking. Đặt dịch vụ thuộc SUC07.01.”

### 6. Version là gì?

> “Version là rowversion do SQL Server sinh để phát hiện bản ghi đã thay đổi. Nó không phải ngày giờ và không phải mật khẩu. Nếu form gửi Version cũ, hệ thống trả 409.”

### 7. Transaction khác Version thế nào?

> “Version phát hiện dữ liệu cũ khi nhiều phiên cùng sửa. Transaction bảo đảm các thay đổi trong một lần ghi cùng thành công hoặc cùng rollback. Hai cơ chế giải quyết hai vấn đề khác nhau.”

### 8. Vì sao sửa hoạt động làm kế hoạch về Nháp?

> “Nội dung kế hoạch đã thay đổi so với lần chấp nhận trước. Hệ thống yêu cầu khách xem lại và chấp nhận lại đúng phiên bản mới.”

### 9. Nếu xóa hoạt động đã gắn tình huống phát sinh thì sao?

> “Manager kiểm tra IncidentActivities trước. Nếu đang được tham chiếu, thao tác bị từ chối; FK Restrict là lớp bảo vệ cuối để không tạo liên kết mồ côi.”

### 10. Phần nào đang khác slide thiết kế?

> “Tên class và route trong source dùng tiếng Anh, một số action được gom vào JourneyController và JourneyManager. Mô hình thời gian hiện dùng Start, End, HasTime; thiết kế mô tả Ngày và Giờ. Em đối chiếu theo trách nhiệm và luồng xử lý, đồng thời ghi nhận đây là khác biệt hiện thực cần tester và nhóm rà soát.”

### 11. Có thể nói hệ thống khớp thiết kế 100% không?

> “Không nên nói như vậy. Em chỉ khẳng định phần đã đối chiếu trong phạm vi chức năng và các ca kiểm thử tương ứng. Những khác biệt về tên lớp, mô hình dữ liệu và một số quy tắc phải được ghi nhận rõ.”

### 12. Nếu chưa kịp mở debugger thì demo thế nào?

> “Em dùng slide thiết kế, code đã mở theo từng hàm, Network hoặc kết quả giao diện và truy vấn dữ liệu. Debugger chỉ là phương án hỗ trợ, không phải bằng chứng duy nhất.”

## D. Thứ tự mở code khi bị yêu cầu show

1. `JourneyController.Activity`: nhận form và gọi Manager.
2. `JourneyRules.ValidateActivity`: kiểm tra ngày, giờ, chi phí, độ dài.
3. `JourneyManager.Activity`: quyền, nguồn tham khảo, Version, thêm hoạt động.
4. `JourneyManager.Version`: chống phiên bản cũ.
5. `JourneyManager.ChangeStatus`: chấp nhận/lưu nháp.
6. `AppDbContext.SaveChangesAsync`: transaction và rollback.
7. `Models/Domain.cs`: Trip, TripActivity, Version và quan hệ dữ liệu.

Khi trình bày, không mở cả file rồi cuộn từ đầu đến cuối. Dùng `Ctrl+Shift+O` để tìm tên hàm, sau đó thu gọn các vùng không liên quan. Trọng tâm là chứng minh chuỗi xử lý trên slide đã có vị trí tương ứng trong code.

## E. Một câu kết ngắn

> “Phần demo của em tập trung vào một chức năng cụ thể, nhưng em không chỉ thao tác trên giao diện. Em đối chiếu từng bước của thiết kế xử lý chi tiết với Controller, lớp nghiệp vụ, quy tắc kiểm tra, cơ sở dữ liệu và cơ chế xử lý đồng thời trong mã nguồn.”
