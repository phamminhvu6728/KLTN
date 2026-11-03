# SUC07.01 Đặt dịch vụ du lịch

Nguồn xử lý hiện hành: thiết kế chi tiết.docx mục 4.9.12, Bảng 4.218–4.226. SUC.txt mục 6.1 cung cấp bối cảnh; khác biệt luồng nhận đề xuất dùng Bảng 4.226 để đối chiếu trong lượt cập nhật này. “Gửi yêu cầu” mới bắt đầu thương lượng; chỉ “Đã xác nhận” là kết quả thành công của SUC. Dịch vụ phải đã duyệt, được công bố, chủ còn quyền tiếp nhận và bật tiếp nhận yêu cầu mới. Các điều kiện được kiểm tra ở xem trước, gửi cuối và chốt thỏa thuận.

## Trường dữ liệu và quy tắc

| Trường | Hợp đồng hiện tại |
|---|---|
| Service | Id dịch vụ công bố và đang tiếp nhận; kiểm tra lại server, không tin hidden input |
| Date | Ngày bắt đầu từ hôm nay Việt Nam trở đi; mặc định GET là ngày mai hoặc ngày từ link kế hoạch |
| EndDate | Tùy chọn, nếu có phải >= Date; không tự nhân số đêm |
| Guests | Số người nguyên 1–100 |
| People | Số lượng đơn vị nguyên 1–100, nhãn Số phòng khi dịch vụ lưu trú |
| ArrivalDeparture | Tùy chọn, tối đa 100 ký tự |
| Options | Mỗi dòng một hạng mục; tối đa 10 dòng không rỗng, mỗi dòng <=150, toàn bộ <=1000; không trùng sau trim và so không phân biệt hoa thường |
| Note | Tối đa 1.000 ký tự sau trim ở manager; HTTP có MaxLength trên input |
| Total | Xem trước ban đầu: server tính đơn giá × People, không lấy giá client; tối đa 1 tỷ. Khi thương lượng, là tổng điều kiện hai bên phải xem và chấp thuận |
| Operation | GUID lượt gửi; cùng lượt gửi lại không sinh thêm Booking |
| PreviewToken | Chỉ tạo sau xem trước hợp lệ, được Data Protection bảo vệ, hạn 10 phút, gắn chủ và đúng nội dung |
| Version | Rowversion Base64; khi xem trước là phiên bản dịch vụ, khi phản hồi/sửa là phiên bản Booking |
| ConditionsVersion | Số bản điều kiện nghiệp vụ; không thay thế rowversion kỹ thuật |

Ban đầu tổng chỉ tính theo đơn vị dịch vụ; Options là nội dung mong muốn để nhà cung cấp phản hồi, chưa có bảng giá phụ phí theo hạng mục. Không tự suy ra giá cho ngày trả phòng hay số người. Đây là giới hạn bảng giá bản demo, cần chốt thêm trước khi vận hành thật.

## Luồng xử lý

1. GET trang dịch vụ, nhập điều kiện. Bấm Tiếp tục kiểm tra yêu cầu. Dữ liệu sai quay về form và hiện lỗi, chưa tạo Booking.
2. Màn hình xem trước hiện dịch vụ, ngày, số người, số lượng, tổng, hạng mục và cảnh báo. Quay lại chỉnh sửa giữ điều kiện; hủy rời luồng không gửi.
3. Bấm xác nhận gửi mới tạo Booking Chờ phản hồi và bản điều kiện 1. Token bị sửa/hết hạn/sai chủ hoặc nội dung khác bản đã xem thì chặn. Giá hoặc thông tin dịch vụ đã đổi thì xem trước lại. Gửi lại cùng Operation không tạo trùng.
4. NCC sở hữu dịch vụ xem yêu cầu. Chấp nhận đúng bản khách đã gửi thì Đã xác nhận. Từ chối phải có lý do; không tạo thỏa thuận.
5. Nếu NCC đề xuất khác: tạo bản điều kiện mới, trạng thái Đề xuất khác. NCC được xem là đã đồng ý bản mình đề xuất. Khách đồng ý đúng bản thì chuyển trực tiếp Đã xác nhận theo Bảng 4.226, không có bước NCC xác nhận thêm. Khách không đồng ý thì Không tiếp tục.
6. Lưu snapshot từng phiên bản điều kiện, hạng mục và người thao tác. Thông báo được ghi trong ứng dụng cho bên còn lại trong giao dịch lưu dữ liệu.

| Hiện tại | Tác nhân và hành động | Trạng thái sau |
|---|---|---|
| Chờ phản hồi | NCC chấp nhận đúng điều kiện | Đã xác nhận |
| Chờ phản hồi | NCC từ chối có lý do | Từ chối |
| Chờ phản hồi | NCC đề xuất khác | Đề xuất khác |
| Đề xuất khác | Khách chấp thuận đúng bản | Đã xác nhận, hai AcceptedVersion cùng bản hiện tại |
| Đề xuất khác | Khách không đồng ý | Không tiếp tục |
| Trạng thái đang chờ | Khách rút hợp lệ | Không tiếp tục |
| Chờ phản hồi hoặc Đề xuất khác | Khách sửa điều kiện | Chờ phản hồi, tăng ConditionsVersion |

Bản cập nhật không tạo trạng thái Chờ NCC xác nhận cuối. Nếu gặp dữ liệu cũ ở trạng thái này, cần ghi rõ nguồn dữ liệu khi tái hiện; không dùng nó làm oracle luồng mới. Thông báo tạm ngừng trong khoảng yêu cầu là cảnh báo; tắt tiếp nhận hoặc mất quyền là điều kiện chặn. Không đồng nhất hai trường hợp.

## Điểm vào kiểm thử

| Route | Phương thức và dữ liệu chính |
|---|---|
| /Marketplace/Details/{id} | GET, optional date=yyyy-MM-dd |
| /Marketplace/Preview | POST BookingTermsInput, cookie khách + CSRF |
| /Marketplace/EditPreview | POST previewToken + CSRF; quay lại giữ dữ liệu |
| /Marketplace/Book | POST toàn bộ hidden fields từ form xem trước, gồm Operation, Version, Total, PreviewToken |
| /Marketplace/Requests | GET danh sách của khách |
| /Marketplace/Request/{id} | GET riêng một yêu cầu của khách; khác chủ 404 |
| /Provider/Requests | GET danh sách của NCC |
| /Provider/Request/{id} | GET riêng một yêu cầu thuộc dịch vụ của NCC; khác chủ 404 |
| /Provider/Respond | POST id,decision,version,conditionsVersion,reason; thêm terms nếu đề xuất |
| /Marketplace/ReplyOffer | POST id,accept,version,conditionsVersion |
| /Marketplace/EditPending | POST id,Version,ConditionsVersion và điều kiện đầy đủ |
| /Marketplace/Withdraw | POST id,version |

decision của Respond: `Đã xác nhận`, `Từ chối`, `Đề xuất khác`; accept là `true` hoặc `false`. Trường Version và conditionsVersion đọc lại sau mỗi thay đổi. Luồng chính trả lỗi trường 400/0701-03, thiếu lý do từ chối 400/0701-06, khác chủ 404/0701-04, xung đột 409/0701-05, không có thay đổi 422/0701-07, dịch vụ không đủ điều kiện 422/0701-01 hoặc 02. Header X-Business-Error chứa mã. Gửi thành công redirect trang riêng yêu cầu. Danh sách khách/NCC 20/trang, trang>=1, hỗ trợ trangThai và sắp CreatedAt DESC, Id DESC.

## Ma trận ca đề nghị kiểm thử

| Mã | Dữ liệu hoặc thao tác | Kết quả mong đợi |
|---|---|---|
| DV01 | Khách mở dịch vụ đang tiếp nhận | Thông tin + form + tổng dự kiến |
| DV02 | Khách chưa đăng nhập | Không gửi được; hướng dẫn đăng nhập |
| DV03 | NCC/Admin dùng POST khách | Bị chặn quyền; không tạo Booking |
| DV04 | Service không tồn tại/chưa duyệt/ẩn/thu hồi | Không gửi được kể cả ID trực tiếp |
| DV05 | Dịch vụ công bố nhưng tắt tiếp nhận | Vẫn tra cứu; không tạo mới |
| DV06 | Date D-1 / D / D+7 | Quá khứ chặn, hôm nay và tương lai hợp lệ |
| DV07 | Date bỏ trống/sai định dạng | Form hiện lỗi, chưa có preview gửi |
| DV08 | EndDate trước/bằng/sau Date hoặc rỗng | Trước chặn; các trường hợp còn lại hợp lệ theo demo |
| DV09 | Guests 0 / 1 / 100 / 101 | Đúng biên 1–100 |
| DV10 | People 0 / 1 / 100 / 101 | Đúng biên, tổng cập nhật đúng |
| DV11 | Số lượng abc/1.5/overflow | Binding chặn, không âm thầm thành 1 |
| DV12 | ArrivalDeparture 100 / 101 | 100 hợp lệ, 101 chặn |
| DV13 | Note 1000 / 1001 | Đúng biên cả POST trực tiếp |
| DV14 | Options 10 / 11 dòng | 10 hợp lệ, 11 chặn |
| DV15 | Một dòng Options 150 / 151 | Đúng biên |
| DV16 | Options trùng chữ thường/hoa và khoảng trắng | Chặn trùng |
| DV17 | Options chỉ dòng trắng / có dòng trắng xen kẽ | Dòng rỗng không tạo hạng mục |
| DV18 | Tổng server đúng 1 tỷ / vượt 1 tỷ | Biên hợp lệ, vượt chặn |
| DV19 | Client sửa Total ban đầu trước Preview | Tổng Preview vẫn lấy đơn giá server × số lượng |
| DV20 | Ngày nằm trong thông báo tạm ngừng đã duyệt | Có cảnh báo rõ trước khi gửi |
| DV21 | Dịch vụ có revision đang chờ duyệt | Hiện cảnh báo, không dùng nội dung chưa duyệt |
| DV22 | Preview hợp lệ rồi chưa gửi | Bookings chưa tăng |
| DV23 | Quay lại chỉnh sửa Preview, có ghi chú nhiều dòng | Toàn bộ điều kiện còn để sửa |
| DV24 | Hủy ở Preview | Không tạo Booking; dữ liệu đã lưu khác giữ nguyên |
| DV25 | Book thiếu/sai PreviewToken hoặc sửa số lượng/ghi chú | Chặn, không tạo dòng |
| DV26 | Token khách A gửi bằng phiên khách B | Chặn, không lộ/chuyển quyền yêu cầu |
| DV27 | Chờ hơn 10 phút mới gửi | Chặn hết hạn, yêu cầu xem lại |
| DV28 | Dịch vụ đổi giá/version sau Preview | Chặn, không chấp nhận giá cũ |
| DV29 | NCC bị đình chỉ/tắt tiếp nhận sau Preview | Gửi cuối bị chặn |
| DV30 | Gửi hợp lệ | Một Booking Chờ phản hồi, bản 1, chưa đặt thành công |
| DV31 | Double click/replay cùng Operation | Chỉ một Booking; DB unique index bảo vệ |
| DV32 | NCC khác POST phản hồi ID không thuộc mình | Dữ liệu yêu cầu giữ nguyên |
| DV33 | Khách khác đọc trang riêng/POST rút | Trang 404, không thay đổi yêu cầu |
| DV34 | NCC chấp nhận đúng bản 1 | Đã xác nhận, hai AcceptedVersion trùng bản hiện tại |
| DV35 | NCC từ chối reason trắng / 500 / 501 | Bắt buộc lý do, biên 500, không tạo thỏa thuận |
| DV36 | NCC đề xuất khác nhưng không đổi điều kiện | Chặn, không tăng bản |
| DV37 | NCC đề xuất khác có điều kiện mới và reason | Tăng bản, Đề xuất khác, khách chưa chấp thuận |
| DV38 | Lý do đề xuất trắng / 2000 / 2001 | Bắt buộc và đúng biên |
| DV39 | Đề xuất People abc/Total abc | Không dùng default; không tạo phiên bản |
| DV40 | Khách chấp thuận đề xuất đúng bản | Đã xác nhận trực tiếp; hai AcceptedVersion bằng bản hiện tại |
| DV41 | NCC gửi lại phản hồi cũ sau khi khách chấp thuận | 409/0701-05; không ghi đè thỏa thuận hoặc thêm bản điều kiện |
| DV42 | Khách không đồng ý đề xuất | Không tiếp tục, lịch sử còn |
| DV43 | Khách sửa khi Chờ phản hồi hoặc Đề xuất khác | Tăng bản, vô hiệu phản hồi trên bản cũ |
| DV44 | Khách sửa People abc hoặc ngày sai | Không tăng bản hoặc đổi điều kiện |
| DV45 | Hai NCC/khách gửi phản hồi version cũ | Không ghi đè, yêu cầu tải lại |
| DV46 | Khách rút trước xác nhận | Không tiếp tục; NCC không xác nhận lại được |
| DV47 | Khách rút trực tiếp sau Đã xác nhận | Bị chặn; cần SUC08.01 |
| DV48 | NCC ngừng tiếp nhận trước ReplyOffer | Không tiếp tục; không tạo thỏa thuận mới theo 4.226 |
| DV49 | NCC mới đề xuất; khách chưa phản hồi, reload | Vẫn Đề xuất khác; khi khách đồng ý đúng bản mới Đã xác nhận |
| DV50 | Mở lịch sử từng bản | Đủ ngày, lượng, tổng, hạng mục và lý do tương ứng |
| DV51 | Thiếu CSRF hoặc CSRF lấy từ vai trò khác | HTTP chặn, DB không đổi |
| DV52 | Mở thông báo bên còn lại sau mỗi phản hồi | Thông báo trạng thái đúng, mở được yêu cầu |
| DV53 | Dialog phản hồi bấm Quay lại | Không gửi, giữ trạng thái và điều kiện |
| DV54 | Nội dung có dấu tiếng Việt, xuống dòng, ký tự HTML | Lưu/hiện đúng, không thực thi script |
| DV55 | Danh sách NCC có một yêu cầu đã xác nhận và một yêu cầu còn chờ của cùng khách | Liên hệ chỉ hiện trong bài yêu cầu đã xác nhận; bài đang chờ không lộ liên hệ |
| DV56 | Khách/NCC mở danh sách với trang 2147483647 | 200, danh sách rỗng; không lỗi SQL hoặc tràn số |

## Kết thúc phạm vi

Demo sâu kết thúc tại Đã xác nhận và kiểm tra lịch sử. Đổi/hủy sau thỏa thuận thuộc SUC08.01; ghi nhận cung cấp thực tế thuộc SUC09.01, dùng kịch bản riêng trong TESTING.md. Không giả lập thanh toán, không sửa ngày DB để nói rằng dịch vụ đã thực hiện. Người kiểm thử cần tự chạy các ca trên, đặc biệt thời hạn token, đồng thời và lỗi hạ tầng; các ca đề nghị không mặc nhiên đã được tự động hóa hết.
