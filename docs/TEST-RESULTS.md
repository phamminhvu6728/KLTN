# Kết quả kiểm thử đối chiếu thiết kế ngày 03 tháng 11 năm 2026

Lần chạy thành công: 2026-11-03T22:09:37.2519281+07:00. Lệnh `powershell -NoProfile -ExecutionPolicy Bypass -File .\run-tests.ps1`.

| Chỉ số | Kết quả |
|---|---|
| xUnit | 132/132 pass, 0 fail, 0 skipped |
| Unit | 131 ca |
| Integration/System | Một test điều phối, 7 script với 233 assertion HTTP; thêm kiểm tra SQL công bố, quản trị và dữ liệu thiết kế |
| Combined line coverage | 86.13% |
| Branch coverage | 63.00% |
| Build bản demo | 0 warning, 0 error |

Phân bố assertion trong script: smoke 11; journey-community 57; marketplace-suc 30; account-verification-suc 29; security-search 23; planning-request-deep 47; design-alignment 36. Không cộng chúng thành 233 test xUnit riêng. Coverage loại migration và mã sinh obj; không đồng nghĩa 80% từng cấp test, nhánh hoặc nghiệm thu. Mã viết nhiều lệnh cùng dòng ảnh hưởng line coverage.

Bằng chứng: `../TestResults/acceptance.trx`, `../TestResults/summary.json`, Cobertura theo đường dẫn trong summary. Lần mới fail phải đọc TRX của lần đó, không dùng summary cũ để báo pass. Database tự động tách riêng `TuTucTest_<guid>`; không dùng TuTucDemo cho bộ test hàng loạt.

Bộ đối chiếu mới kiểm tra tiền nullable, thứ tự tăng theo ngày, cùng giờ bắt đầu, nhận đề xuất xác nhận trực tiếp, lỗi 400/404/409/422 và mã nghiệp vụ, chống gửi lặp, bảo vệ liên hệ trước thỏa thuận, lưu nháp về danh sách. Kiểm tra SQL bổ sung xác nhận SK11 có Before/After và thông báo có Type trong DB.

Kiểm tra trên bản demo local sau migration: chạy design-alignment-test.mjs đạt 25 assertion với dữ liệu mới, kế hoạch #1006 và yêu cầu #8. Đã đọc màn hình yêu cầu #8 trên trình duyệt: Đã xác nhận, bản 2, tổng 975.000. Lượt này cuối cùng lưu kế hoạch Nháp để kiểm tra 4.186. Dữ liệu cũ #1005/#7 có thể thiếu các cột nội dung audit/Type mới; không backfill giả lịch sử cũ.

Hai hướng dẫn Word đã được xuất PDF bằng Word và render qua renderer của bộ documents để kiểm tra 13 trang. File nguồn thiết kế được đọc riêng, không chỉnh sửa.

Gói [bàn giao tester](ban-giao-kiem-thu/README.md) giữ 43 ca kế hoạch và 56 ca dịch vụ đề nghị, đã sửa oracle theo thiết kế mới. Các ca đó cần tester chạy độc lập; không mặc định đã tự động hóa hết. [Ma trận đối chiếu](ban-giao-kiem-thu/DOI_CHIEU_THIET_KE.md) ghi rõ phạm vi và khác biệt còn lại.

Chưa có biên bản UAT độc lập, kiểm thử tải lớn hoặc chứng nhận triển khai công khai. A14 là phương án 14/18 SUC cần chốt; hai luồng demo sâu không thay thế nghiệm thu toàn bộ phạm vi.

Rà soát bổ sung: đã sửa rò rỉ liên hệ giữa yêu cầu đã xác nhận và yêu cầu còn chờ của cùng khách trên danh sách NCC; chặn tràn số khi tính trang rất lớn cho khách/NCC/kế hoạch. Có 5 assertion HTTP hồi quy bổ sung. Kiểm tra browser thực tế: form tạo kế hoạch hiện đúng dialog; Quay lại đóng dialog và giữ tên đang soạn. Xem RA_SOAT_DEMO.md.
