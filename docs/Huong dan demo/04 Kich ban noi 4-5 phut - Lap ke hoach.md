# Kịch bản báo cáo dev 4–5 phút — Lập kế hoạch

Mục tiêu 4 phút 30 giây, còn 30 giây dự phòng chuyển màn hình. Đi từ tạo nháp → thêm một hoạt động → chấp nhận; giải thích sâu luồng thêm hoạt động theo Bảng 4.181. Giữ nguyên bản sản phẩm đã chốt.

Source đã được tách dòng và nhóm vùng ngày 05/11/2026; liên kết phía dưới đã cập nhật theo vị trí mới, logic nghiệp vụ giữ nguyên.

## Mở code cho dễ trình bày trong VS Code

- `Ctrl+Shift+O`: gõ tên hàm như `Activity`, `Version`, `SaveChangesAsync` để nhảy thẳng tới phần cần nói.
- `Ctrl+K`, thả phím, rồi `Ctrl+0`: thu gọn code; bấm mũi tên cạnh vùng/hàm cần trình bày để mở riêng phần đó.
- `Ctrl+K`, thả phím, rồi `Ctrl+J`: mở lại toàn bộ code.
- `Alt+Z`: bật/tắt xuống dòng theo chiều rộng màn hình.
- JourneyController đã chia 4 vùng; JourneyManager chia 6 vùng. Trong bài chính, tập trung vùng xử lý kế hoạch và vùng hàm chung, để gọn vùng tình huống phát sinh.
- `JourneyManager.Activity` có chú thích 6 bước; `JourneyController.Activity` có 4 bước. Nói theo các bước này và đối chiếu Bảng 4.181, không đọc cả file.

## Chuẩn bị trước giờ trình bày

- Đăng nhập vai trò khách du lịch. Điền sẵn nhưng chưa gửi form tạo kế hoạch: `BAO_CAO_DEV`, Đà Lạt, 11/11/2026–13/11/2026, ngân sách 1.000.000đ. Nếu báo cáo ngày khác thì chọn ngày tương lai.
- Ghi sẵn dữ liệu hoạt động để nhập nhanh: `Tham quan buổi sáng`, ngày 11/11/2026, 08:00–09:00, chi phí 300.000đ, nguồn Tự nhập.
- Mở thiết kế chi tiết tại Bảng 4.181 và 4.185. Mở Chương 4 tại mô hình lớp/CSDL nếu cần trả lời thêm; số bảng của bản Chương 4 cũ có thể khác.
- Mở sẵn các tab code theo thứ tự: Domain → JourneyController → JourneyRules → JourneyManager → AppDbContext. Tăng cỡ chữ editor; đánh dấu vị trí cần nói.
- Tập hai lần bằng đồng hồ. Nếu quá giờ, bỏ minh họa lỗi và truy vấn DB trực tiếp, vẫn giữ phần giải thích luồng, quyền, Version và transaction.

## Lời nói và thao tác theo thời gian

### 0:00–0:20 — Giới thiệu, mở thiết kế

> Em trình bày SUC02.01 Lập kế hoạch chuyến đi của khách du lịch. Em thực hiện tạo nháp, thêm hoạt động và chấp nhận kế hoạch; trong đó đi sâu vào luồng thêm hoạt động ở Bảng 4.181 để đối chiếu thiết kế với mã nguồn và cách bảo vệ dữ liệu.

### 0:20–1:00 — CSDL và lớp

Mở Domain tại Trip và TripActivity.

> Dữ liệu chính gồm Trips lưu kế hoạch và Activities lưu hoạt động. Một kế hoạch có nhiều hoạt động, liên kết qua TripId; OwnerId xác định người sở hữu. Version trên kế hoạch dùng phát hiện phiên bản cũ. Về xử lý, Controller nhận yêu cầu HTTP; Manager phối hợp nghiệp vụ; Rules kiểm tra dữ liệu; DbContext ghi SQL Server. Input model nhận dữ liệu form được tách khỏi entity lưu DB.

### 1:00–1:30 — Tạo nháp trên giao diện

Bấm tạo kế hoạch và xác nhận trong dialog. Sau đó mở JourneyController.Save.

> Khi tạo mới, server lấy người sở hữu từ tài khoản đăng nhập, kiểm tra thông tin chung và ngày, rồi lưu trạng thái Nháp. Nháp đã tồn tại trong DB. Tạo thành công chuyển về chi tiết để tiếp tục lập lịch trình.

### 1:30–3:00 — Thêm hoạt động và đối chiếu xử lý

Nhập một hoạt động theo dữ liệu mẫu, bấm lưu và xác nhận. Chỉ nhanh kết quả 300.000đ trên trang, rồi mở code theo thứ tự Controller.Activity → Rules.ValidateActivity → Manager.Activity; đặt cạnh Bảng 4.181.

> Với thao tác thêm hoạt động, Controller kiểm tra binding và ModelState rồi gọi Manager. Manager đọc kế hoạch theo cả mã và OwnerId để bảo đảm đúng chủ. Rules kiểm tra ngày nằm trong chuyến đi, giờ kết thúc sau giờ bắt đầu, nội dung và chi phí hợp lệ. Nếu chọn nguồn tham khảo thì Manager kiểm tra nguồn trước khi lưu. Tiếp theo là kiểm tra phiên bản. Hợp lệ thì thêm hoạt động, tự lấy thứ tự cuối ngày bằng MAX cộng một, cập nhật kế hoạch về Nháp và lưu dữ liệu. Thành công chuyển về chi tiết, tính lại tổng chi phí. Như kết quả vừa thực hiện, kế hoạch có một hoạt động dự kiến 300.000đ.

### 3:00–3:55 — An toàn thông tin, đồng thời và giao dịch

Trong Manager, chỉ query OwnerId, hàm Version và catch concurrency; chuyển AppDbContext tại transaction. Không cần chạy hai tab trong bài chính.

> Quyền không phụ thuộc nút trên giao diện: server kiểm tra vai trò, tài khoản hoạt động và chủ sở hữu. Nếu hai tab cùng đọc V1, tab A lưu thành V2 thì tab B gửi V1 sẽ bị từ chối với 409. EF kiểm tra phiên bản ngay khi UPDATE để chặn thay đổi xảy ra sau bước so sánh. Hoạt động và kế hoạch cha được ghi trong cùng giao dịch; lỗi thì rollback. Trong bản này, transaction ghi được mở ở DbContext.SaveChangesAsync.

### 3:55–4:30 — Chấp nhận và kết quả cuối

Quay lại trang chi tiết mới nhất, bấm Chấp nhận, xác nhận dialog. Chỉ trạng thái và tổng tiền; mở ChangeStatus nếu còn thời gian.

> Cuối cùng, hệ thống kiểm tra phiên bản và dữ liệu trước khi đổi trạng thái sang Đã chấp nhận, tương ứng Bảng 4.185. Đây là chấp nhận lịch trình dự kiến, chưa tạo yêu cầu đặt dịch vụ. Nếu sửa nội dung sau đó, kế hoạch trở về Nháp để khách xem và chấp nhận lại.

## Ôn theo thứ tự ưu tiên

| Mức | Nội dung phải hiểu | Tự trả lời được câu nào? |
|---|---|---|
| Bắt buộc | Quan hệ Trips–Activities, OwnerId, TripId | Dữ liệu thuộc ai và liên kết thế nào? |
| Bắt buộc | Controller → Manager → Rules → DbContext | Bấm lưu thì yêu cầu đi qua những đâu? |
| Bắt buộc | Quy tắc ngày, giờ, chi phí; lỗi khác cảnh báo | Khi nào chặn lưu và vì sao? |
| Bắt buộc | Quyền vai trò và quyền đối tượng | Vì sao đổi ID không sửa được kế hoạch người khác? |
| Bắt buộc | Version và DbUpdateConcurrencyException | Vì sao vừa so sánh Version vừa kiểm tra khi UPDATE? |
| Bắt buộc | Transaction | Nếu thêm hoạt động nhưng cập nhật cha lỗi thì sao? |
| Bắt buộc | Nháp ↔ Đã chấp nhận | Vì sao sửa xong phải chấp nhận lại? |
| Hỏi sâu | Antiforgery, input model, Razor encode, LINQ tham số hóa | Các biện pháp ngăn request giả mạo/dữ liệu không tin cậy? |
| Hỏi sâu | Xóa hoạt động và IncidentActivities | Vì sao có hoạt động không được bỏ? |
| Hỏi sâu | Chênh lệch thiết kế–hiện thực | Điểm nào chưa khớp và giới hạn nằm ở đâu? |

## Câu hỏi và câu trả lời ngắn

**1. Vì sao tách nhiều lớp?** Controller phụ trách HTTP; Manager phụ trách phối hợp nghiệp vụ; Rules có thể kiểm thử không cần SQL; DbContext phụ trách lưu dữ liệu.

**2. Client đã kiểm tra rồi sao server kiểm tra lại?** Khách có thể sửa form hoặc gửi HTTP trực tiếp. Server quyết định dữ liệu được ghi.

**3. Vì sao không nhận OwnerId từ form?** Trường phía khách sửa được; người sở hữu phải lấy từ danh tính đã xác thực. TripInput/ActivityInput không cho khách chọn trạng thái hoặc chủ kế hoạch.

**4. Đổi ID kế hoạch người khác thì sao?** Query dùng cả ID và OwnerId. Không tìm được đối tượng thuộc mình thì trả 404.

**5. Chống CSRF ở đâu?** Program đăng ký AutoValidateAntiforgeryTokenAttribute; Form Tag Helper sinh token cho form POST. Dialog xác nhận không phải cơ chế chống CSRF.

**6. Nháp đã lưu chưa?** Đã lưu DB, vẫn phải có dữ liệu tối thiểu hợp lệ. Nháp khác bản nhập chưa gửi trên form.

**7. Trùng giờ/vượt ngân sách có chặn không?** Là cảnh báo để khách cân nhắc, không chặn chấp nhận. Hoạt động ngoài ngày chuyến đi hoặc chi phí âm là lỗi dữ liệu và chặn lưu.

**8. Rowversion có phải thời gian không?** Không. SQL Server sinh giá trị nhị phân 8 byte khi cập nhật dòng. Base64 chỉ dùng biểu diễn giá trị này trong form, không phải mã hóa bí mật.

**9. Vì sao so sánh Version rồi vẫn bắt exception?** Có thể có request khác ghi sau lúc so sánh. EF dùng OriginalValue trong điều kiện UPDATE; không khớp thì ném DbUpdateConcurrencyException và trả 409.

**10. Vì sao sửa hoạt động phải cập nhật Trip?** Version nằm trên Trip. Cập nhật cha làm Version thay đổi để đại diện cho nội dung lịch trình vừa sửa.

**11. Transaction khác rowversion thế nào?** Transaction giữ các bước ghi cùng thành công hoặc cùng rollback; rowversion phát hiện lần ghi dựa trên dữ liệu cũ. Hai cơ chế bổ sung nhau.

**12. Chấp nhận kế hoạch có đặt dịch vụ không?** Không. Mã dịch vụ trên hoạt động phục vụ tham khảo; luồng này không tạo Booking.

**13. Budget hoặc Cost trống khác 0 thế nào?** Trống là chưa xác định. Cost NULL được cộng như 0 khi tính tổng nhưng không có nghĩa miễn phí; Budget NULL không áp cảnh báo vượt ngân sách.

**14. Vì sao xóa hoạt động có thể bị chặn?** Nếu IncidentActivities tham chiếu, Manager và FK Restrict bảo vệ liên kết tình huống. Chưa có nghiệp vụ xóa toàn bộ kế hoạch.

**15. Có khớp hoàn toàn thiết kế không?** Chưa. Cần nêu đúng các chênh lệch đã nhận diện; không dùng một demo chạy được làm bằng chứng mọi lớp/ràng buộc đều khớp.

## Ba điểm dễ trả lời sai khi đối chiếu

1. Thiết kế mô tả trùng cùng ngày/cùng giờ bắt đầu; code hiện phát hiện giao khoảng khi cùng ngày bắt đầu. Đây là chênh lệch quy tắc cần tester ghi nhận.
2. Thiết kế bao đọc/kiểm tra/ghi trong giao dịch Service; code lập kế hoạch hiện đọc/kiểm tra trước, transaction ghi mở trong DbContext. Không nói cả luồng kiểm tra đã nằm trong transaction.
3. Tổ chức lớp/interface và lược đồ vật lý chưa trùng nguyên thiết kế; bản hiện tại sử dụng EF migrations. Không nói chỉ khác tên, hoặc tuyên bố toàn bộ CHECK/FK trong báo cáo đã được triển khai.

Không cần tự đọc dài các chênh lệch trong bài 4–5 phút, nhưng phải trả lời đúng nếu được hỏi. Không khẳng định “khớp 100%”.

## Vị trí code để mở nhanh

| Nội dung | File |
|---|---|
| Trip, TripActivity | [Domain.cs:31](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Models/Domain.cs:31>) |
| Input models | [JourneyModels.cs:6](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Models/JourneyModels.cs:6>) |
| Save / Activity | [JourneyController.cs:65](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Controllers/JourneyController.cs:65>) |
| ValidateActivity | [JourneyRules.cs:27](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyRules.cs:27>) |
| Thêm/sửa hoạt động | [JourneyManager.cs:126](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:126>) |
| Chấp nhận/lưu nháp | [JourneyManager.cs:264](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:264>) |
| Quyền ghi / Version | [JourneyManager.cs:445](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Services/JourneyManager.cs:445>) |
| Giao dịch ghi + audit | [AppDbContext.cs:75](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Data/AppDbContext.cs:75>) |
| Antiforgery | [Program.cs:14](<C:/Users/bakav/Demo KLTN/Ho tro khach du lich tu tuc/Program.cs:14>) |

Khi còn thời gian cho phần hỏi thêm, minh họa hai tab cùng đọc → A sửa → B chấp nhận bản cũ → 409 → tải lại → chấp nhận mới. Hai tab cùng tài khoản đủ cho tình huống phiên bản cũ; kiểm tra quyền giữa hai khách cần hai phiên đăng nhập độc lập.
