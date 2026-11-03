# Kết quả rà soát trước khi tập demo

Thời điểm bộ hồi quy: 2026-11-03T22:09:37.2519281+07:00. Giao diện giữ như bản hiện tại; ô thứ tự hoạt động đã được làm rõ theo trạng thái thêm mới/chỉnh sửa.

**Kết luận:** hai luồng lập kế hoạch và yêu cầu dịch vụ đã qua bộ hồi quy và có thể dùng để tập theo kịch bản trong file 01. Đã sửa hai lỗi tìm được trong lượt rà soát này. Kết quả chưa thay thế nghiệm thu độc lập toàn bộ hệ thống hoặc chứng nhận mọi action khớp nguyên dạng thiết kế.

## Lỗi phát hiện và đã sửa

| Lỗi | Nguyên nhân | Cách sửa và bằng chứng |
|---|---|---|
| Liên hệ khách có thể hiện ở yêu cầu còn chờ nếu cùng danh sách có yêu cầu đã xác nhận của khách đó | Danh sách liên hệ được lấy theo khách, còn view chưa kiểm tra trạng thái của từng bài yêu cầu | View chỉ hiện liên hệ khi chính yêu cầu có trạng thái Đã xác nhận. Hai assertion mới kiểm tra bài còn chờ không có liên hệ và bài đã xác nhận vẫn có liên hệ. |
| Nhập trang 2147483647 có thể gây tràn số khi tính vị trí dòng | Phép nhân số trang với 20 dùng int | Tính bằng long; trang vượt phạm vi trả danh sách rỗng. Ba assertion cho danh sách kế hoạch, yêu cầu khách và yêu cầu NCC. |

File thay đổi: Views/Marketplace/Requests.cshtml, Views/Journey/PlanList.cshtml, Controllers/JourneyController.cs, Services/MarketplaceBookings.cs. Kiểm thử bổ sung nằm trong design-alignment-test.mjs.

## Đã kiểm tra bằng cách nào

- Build khởi động lại: 0 warning, 0 error. /health trả application=TuTuc, status=ready.
- Bộ run-tests.ps1: 132/132 xUnit pass, 0 fail; line coverage 86.13%, branch coverage 63.00%.
- 131 ca unit và một ca điều phối integration/system. Bảy script thực hiện 233 assertion HTTP: 11 + 57 + 30 + 29 + 23 + 47 + 36. Có kiểm tra SQL bổ sung; không cộng các assertion thành 233 ca xUnit.
- Test tự động dùng TuTucTest_<guid>, không sửa dữ liệu demo hàng loạt. Database lượt này: TuTucTest_849ecd2b3cc842f696bace6cbc3998ed.
- Browser thực tế: mở form tạo kế hoạch, nhập RA_SOAT_CHUA_LUU, hiện dialog với đúng dữ liệu; bấm Quay lại đóng dialog và giữ tên trong form. Không xác nhận tạo dữ liệu lượt này.
- Browser thực tế: form thêm hoạt động hiển thị “Tự xếp cuối ngày”; form Chỉnh sửa hoạt động có ô SortOrder số, bật nhập và ghi chú số nhỏ được xếp trước.
- Đọc màn hình yêu cầu #8 trên bản demo: Đã xác nhận, điều kiện bản 2, tổng 975.000. Bản ghi này được tạo ở lượt đối chiếu trước; không coi việc đọc bản đã có là một lượt demo mới từ đầu.
- Hai hướng dẫn Word được xuất qua Word và render để xem toàn bộ 13 trang: file 01 có 6 trang, file 02 có 7 trang. Đã kiểm tra bảng, tiếng Việt, ngắt trang và nội dung không bị cắt.

Bằng chứng máy đọc: TestResults/summary.json, acceptance.trx và Cobertura theo đường dẫn trong summary. Không dùng summary cũ khi một lần chạy mới thất bại.

## Các hướng dẫn mới

File 01 đi theo bảy bước. Mỗi bước có thao tác, dữ liệu mẫu, kết quả phải thấy, câu nói gợi ý và số bảng cần mở trong thiết kế. Phần terminal là tùy chọn khi được hỏi DB.

File 02 giải thích bằng ví dụ trước, sau đó mới giới thiệu phiên bản, transaction, MVC và code. Kết quả kiểm thử và phạm vi A14 được tách ở phần cuối. Đọc trang 1–4 trước để nắm nghiệp vụ; tra trang 5–7 khi hỏi kiến trúc, kiểm thử hoặc phạm vi.

## Những phần chưa được coi là hoàn tất nghiệm thu

Xem ban-giao-kiem-thu/DOI_CHIEU_THIET_KE.md. Các điểm còn chốt gồm ánh xạ tên route/action và kiểu khóa, quy ước lỗi/nhật ký của mọi module, chi phí mong muốn nullable trong yêu cầu so với cách tính tổng từ giá danh mục hiện tại, và rà soát đầy đủ mọi action trong thiết kế. Chưa có UAT độc lập, thử tải lớn, thanh toán hoặc tồn chỗ thật.

Phạm vi A14 là phương án 14/18 SUC ưu tiên mục tiêu 1, 2, 4; cần giảng viên chốt. Các ma trận cho tester có 43 ca kế hoạch và 56 ca dịch vụ đề nghị, chưa được mặc định đánh dấu pass toàn bộ.
