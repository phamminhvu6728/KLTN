# SUC02.01 Lập kế hoạch chuyến đi

Nguồn xử lý hiện hành: thiết kế chi tiết.docx mục 4.9.4, Bảng 4.176–4.186; bản trích đọc tại docs/requirements/THIET_KE_CHI_TIET.txt. SUC.txt mục 2.2 cung cấp bối cảnh nghiệp vụ. Mục tiêu: khách lưu một lịch trình dự kiến, tự xem lại và chấp nhận. Quyền: Traveler đang hoạt động, chỉ dữ liệu của chính mình. Quy ước D là ngày hôm nay tại Việt Nam (UTC+7); các ca tạo mới dùng D+7 đến D+9 để chạy lại được.

## Hợp đồng trường dữ liệu

| Trường POST | Quy tắc bản hiện tại |
|---|---|
| Name | Bắt buộc, không toàn khoảng trắng, tối đa 150 ký tự; trim khi lưu |
| Destination | Bắt buộc, không toàn khoảng trắng, tối đa 200 ký tự |
| Start, End của kế hoạch | Bắt buộc phải có trong POST, ngày kết thúc >= bắt đầu; tạo mới không được bắt đầu quá khứ |
| Khoảng kế hoạch | Ít nhất một ngày; bỏ giới hạn 366 ngày không có trong thiết kế; trang chi tiết chia nhóm 31 ngày để thao tác |
| Budget | Tùy chọn, NULL khác 0; nếu nhập thì 0–1.000.000.000; decimal(18,0) nullable |
| Id, Version | Id=0 khi tạo; khi sửa phải đúng Id thuộc chủ và rowversion Base64 mới nhất |
| TripId, Id của hoạt động | Đúng kế hoạch thuộc chủ; Id=0 thêm mới, Id>0 sửa đúng mục |
| SourceSelection | Manual hoặc Place:{id} hoặc Service:{id}; HTTP khác có thể dùng SourceType và SourceId |
| Title, Location | Mỗi trường tối đa 300; Title bắt buộc khi Manual; chọn nguồn có thể để trống Title để dùng tên nguồn |
| Start, End của hoạt động | Bắt buộc; trong ngày của kế hoạch; có giờ thì End > Start |
| HasTime | true có giờ; false chưa xác định giờ, lưu từ đầu đến cuối ngày Start và không tính xung đột giờ |
| Cost | Tùy chọn, NULL khác 0; nếu nhập thì 0–1.000.000.000; không tự lấy giá dịch vụ làm chi phí dự kiến |
| SortOrder | Sửa: số nguyên từ 1. Thêm: server tự lấy MAX thứ tự trong cùng ngày + 1 |
| Notes | Tối đa 1.000 ký tự |

Trình bày theo Ngày → SortOrder → Start → Id. Theo Bảng 4.180, cảnh báo trùng khi cùng ngày và cùng giờ bắt đầu, không phải mọi khoảng giao nhau. Tổng dự kiến là SUM(Cost ?? 0). Chỉ cảnh báo vượt ngân sách nếu Budget khác NULL; Budget=0 vẫn có ý nghĩa. Cảnh báo không lưu DB và không chặn chấp nhận. Sửa ngày kế hoạch không được làm hoạt động cũ rơi ra ngoài khoảng mới.

## Trạng thái và thao tác

Tạo/sửa thông tin/thêm/sửa/bỏ hoạt động đều lưu Nháp. Chấp nhận kế hoạch lưu Đã chấp nhận; sửa tiếp đưa về Nháp để xem lại. Lưu nháp không bỏ qua trường bắt buộc. Cảnh báo không chặn chấp nhận. Không yêu cầu tối thiểu một hoạt động ở phiên bản này. Chưa cung cấp xóa toàn bộ kế hoạch; chỉ bỏ mục không được IncidentActivities tham chiếu.

| Route | Phương thức và trường chính | Kết quả |
|---|---|---|
| /Journey | GET trang>=1 | Danh sách thuộc chủ; 20/trang; Start DESC, Id DESC |
| /Journey/Details/{id} | GET | Chi tiết thuộc chủ, sai chủ 404 |
| /Journey/Save | POST Id,Name,Destination,Start,End,Budget,Version | Tạo hoặc sửa; thành công 302 |
| /Journey/Activity | POST TripId,Id,Version và trường hoạt động | Thành công 302; dữ liệu ngoài khoảng 422 |
| /Journey/Confirm | POST id,version | Chấp nhận, 302; cũ phiên bản 409 |
| /Journey/Draft | POST id,version | Đưa về Nháp và redirect danh sách /Journey theo 4.186 |
| /Journey/RemoveActivity | POST tripId,id,version | Bỏ đúng mục, không tham chiếu tình huống |
| /Journey/Delete | POST | 400 vì chưa thuộc phạm vi chốt |

400: binding/thiếu trường hoặc dữ liệu kế hoạch không hợp lệ; 422: ngày hoạt động/ràng buộc tham chiếu; 409: rowversion cũ; 404: đối tượng không thuộc chủ. Form lỗi giữ nội dung đang soạn; dữ liệu DB hợp lệ trước đó không bị ghi đè.

## Ma trận ca đề nghị kiểm thử

Mỗi dòng có thể tách thành nhiều test data. Tester ghi actual và pass/fail riêng.

| Mã | Dữ liệu hoặc thao tác | Kết quả mong đợi |
|---|---|---|
| KH01 | Tạo tên và nơi đến hợp lệ, D+7…D+9, Budget 0 | Nháp, đúng chủ, xuất hiện sau reload |
| KH02 | Name rỗng / chỉ dấu cách | Chặn, không có Trip mới |
| KH03 | Name 150 / 151 ký tự | Biên 150 hợp lệ; 151 chặn |
| KH04 | Destination rỗng / 200 / 201 | Bắt buộc và đúng biên |
| KH05 | Bỏ Start/End khỏi POST dù model có default | 400, không âm thầm dùng default |
| KH06 | Ngày sai định dạng / bỏ trống trên UI | Binding/UI chặn, DB giữ nguyên |
| KH07 | End < Start | Chặn lưu và chấp nhận |
| KH08 | Start = End = D+7 | Kế hoạch một ngày hợp lệ |
| KH09 | Start D-1 khi tạo | Chặn ở manager |
| KH10 | Khoảng 366 / 367 / 500 ngày | Đều hợp lệ nếu ngày đúng; xem tiếp các nhóm 31 ngày không mất hoạt động |
| KH11 | Budget rỗng / -1 / 0 / 1 tỷ / 1 tỷ+1 | Rỗng lưu NULL; đúng hai biên; ngoài biên chặn |
| KH12 | Budget là abc, overflow số | Chặn binding, không dùng 0 thay thế |
| KH13 | Xem chi tiết kế hoạch khách khác | 404 và không lộ nội dung |
| KH14 | Gửi POST sửa kế hoạch khách khác | Không đổi DB |
| KH15 | Hoạt động tự nhập đầy đủ | Nháp, tổng cộng đúng |
| KH16 | Manual Title rỗng | Chặn; chọn nguồn hợp lệ Title rỗng dùng tên nguồn |
| KH17 | Title/Location 300 / 301, Notes 1000 / 1001 | Đúng biên kể cả bỏ maxlength ở client |
| KH18 | Hoạt động ở D+6 hoặc D+10 | 422, bản đang nhập còn trên form |
| KH19 | Timed End = Start / End < Start | 422, không thêm mục |
| KH20 | Hai mục cùng ngày cùng giờ bắt đầu; thêm ca chỉ giao một phần khác giờ bắt đầu | Cùng giờ bắt đầu có cảnh báo; chỉ giao một phần không kích hoạt quy tắc 4.180 |
| KH21 | Hai khoảng tiếp giáp | Không cảnh báo trùng |
| KH22 | Mục không có giờ trùng ngày với mục timed | Không tự tính là xung đột giờ |
| KH23 | Cost rỗng / -1 / 0 / 1 tỷ / 1 tỷ+1 | Rỗng lưu NULL, cộng tổng như 0; đúng biên và tổng |
| KH24 | Tổng = Budget / tổng > Budget 1đ | Bằng không cảnh báo, vượt có cảnh báo |
| KH25 | Thêm mục chiều trước, mục sáng sau, cùng ngày | Thứ tự server là 1,2 và hiển thị theo thứ tự đó, không tự đảo theo giờ |
| KH26 | Cùng giờ, SortOrder khác nhau | Thứ tự theo SortOrder, không thay đổi giờ |
| KH27 | Khi sửa: SortOrder 0 / 1 hoặc abc; thêm với 99 | Sửa 0/sai kiểu chặn, 1 hợp lệ; thêm lấy MAX+1, không dùng 99 từ client |
| KH28 | SourceType lạ, id không có, dịch vụ chưa duyệt/ẩn | Chặn thêm tham chiếu |
| KH29 | Chọn dịch vụ đã công bố nhưng đang ngừng tiếp nhận | Có thể tham khảo trong kế hoạch; không suy ra gửi yêu cầu được |
| KH30 | Thu hẹp ngày kế hoạch làm mất khoảng hoạt động | 422, không đổi ngày đã lưu |
| KH31 | Chấp nhận khi còn cảnh báo | Đã chấp nhận, Bookings không tăng |
| KH32 | Sửa kế hoạch đã chấp nhận | Nháp, tổng/cảnh báo tính lại |
| KH33 | Hai phiên cùng mở; A sửa rồi B lưu version cũ | B 409, không mất dữ liệu A |
| KH34 | Bấm Quay lại trong dialog tạo/sửa/bỏ | Không POST, DB không đổi, nội dung nhập còn |
| KH35 | Bỏ hoạt động không tham chiếu | Mất đúng một mục, tổng tính lại, Nháp |
| KH36 | Bỏ mục đã gắn tình huống | 422, mục và liên kết còn nguyên |
| KH37 | Khách khác POST bỏ mục / id không thuộc trip | 404, không ảnh hưởng đối tượng khác |
| KH38 | Lưu nháp rồi reload/mở lại | Giá trị giữ đúng, chỉ của chủ |
| KH39 | Mất CSRF / chưa login / dùng vai trò NCC | Bị chặn trước ghi dữ liệu |
| KH40 | In kế hoạch | Chỉ nội dung cần in, ẩn menu và form thao tác |
| KH41 | Link dịch vụ từ hoạt động | Đúng dịch vụ, điền ngày; không tạo Booking lúc bấm link |
| KH42 | Nhấn chấp nhận ở version cũ hoặc sai Base64 | 409; không đổi trạng thái |
| KH43 | Trang 2147483647 khi danh sách có dữ liệu | 200, trang rỗng; không tràn phép tính và không hiện lại bản ghi trang đầu |

## Điểm kiểm tra nhỏ khi viết test

Unit thuần kiểm tra JourneyRules. Integration kiểm tra chủ sở hữu, EF rowversion, nguồn công bố và liên kết IncidentActivities. Browser kiểm tra tab order, nhãn, lỗi dễ đọc, dialog Quay lại, mở chi tiết sửa đúng mục, sắp xếp và bản in. Khi test đồng thời phải giữ **hai cookie jar độc lập**, không chỉ hai tab cùng phiên.

Oracle mới lấy từ các bảng xử lý 4.179–4.186. Bổ sung kiểm tra ngân sách/chi phí NULL, danh sách 20/trang, lưu nháp redirect danh sách và header X-Business-Error. Xem DOI_CHIEU_THIET_KE.md để phân biệt ánh xạ tên và phạm vi đã đối chiếu. Không thêm ca kỳ vọng tự động đặt dịch vụ hoặc xóa cả kế hoạch vì bản này chưa có nghiệp vụ đó.
