# Demo tám phút theo thiết kế xử lý

Nguồn: thiết kế chi tiết.docx mục 4.9.4 và 4.9.12. Hướng dẫn từng thao tác và lời giải thích nằm trong hai file Word ở `docs/Huong dan demo`. Tài liệu này thay kịch bản cũ có bước NCC xác nhận cuối.

Chuẩn bị hai cookie session độc lập, khách `khach@demo.vn` và NCC `ncc@demo.vn`, mật khẩu `Demo12345`. Chạy `start-demo.cmd`. Mở Word và Ctrl+F tìm số bảng. D là hôm nay Việt Nam, dùng D+7…D+9 để chạy lại được.

| Thời gian | Thao tác | Bảng thiết kế và kết quả |
|---|---|---|
| 0:00–0:40 | Giới thiệu một chuyến đi xuyên suốt, mở Kế hoạch của tôi | 4.176 |
| 0:40–2:40 | Tạo kế hoạch 1 triệu; thêm hai mục cùng bắt đầu 08:00, chi phí 700.000 và 500.000; chấp nhận | 4.179–4.181, 4.185: trùng giờ/vượt 200.000 chỉ cảnh báo; chấp nhận không đặt dịch vụ |
| 2:40–4:10 | Mở dịch vụ đủ quyền; 2 người/2 đơn vị; xem trước, quay lại, xem lại và gửi | 4.218–4.220: Preview chưa ghi; gửi mới Chờ phản hồi bản 1 |
| 4:10–5:40 | NCC mở đúng Id, đề xuất ngày D+8, tổng 975.000 và lý do | 4.224–4.225: Đề xuất khác bản 2; giữ bản 1 |
| 5:40–6:40 | Khách đọc điều kiện mới, chấp thuận đúng bản 2 | 4.226: trực tiếp Đã xác nhận; không thêm bước NCC |
| 6:40–7:30 | Mở lịch sử và terminal | Hai bản điều kiện; SK11 Before/After; thông báo có loại |
| 7:30–8:00 | Nêu kết quả kiểm thử và phạm vi A14 | Đọc TEST-RESULTS.md, không coi coverage là UAT |

Lệnh chỉ đọc DB, thay Id của lượt demo:

```powershell
.\show-demo-data.ps1 -TripId 1006 -RequestId 8
```

Id trên là lượt đối chiếu nhánh nullable, cuối cùng Nháp, không phải kế hoạch ngân sách 1 triệu. Khi báo cáo nên tự tạo tên DEMO_THIET_KE_01 rồi ghi Id thật. Dùng dữ liệu mới để thấy Type và SK11; bản cũ trước migration không được bổ sung lịch sử giả.

Nếu chỉ có 5 phút, dùng kế hoạch chuẩn bị trước và dành phần lớn thời gian cho Preview → gửi → NCC đề xuất → khách chấp thuận → lịch sử. Nếu có 12 phút, thêm lỗi ngày ngoài chuyến đi (422/0201-03) hoặc hai phiên chấp nhận bản cũ (409/0201-05). Trình bày rõ dữ liệu nào chuẩn bị trước.

Đối chiếu tên tiếng Việt trong báo cáo với route/code hiện hành ở DOI_CHIEU_THIET_KE.md. Không tuyên bố toàn bộ 88 action đã giữ nguyên hợp đồng hoặc đã nghiệm thu. Phương án A14 là 14/18 SUC theo số lượng, không phải 14 nhóm trên 14.
