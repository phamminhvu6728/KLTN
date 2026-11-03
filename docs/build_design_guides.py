"""Build the two plain-language demo manuals, with technical details at the end."""
import json
import re
import xml.etree.ElementTree as ET
from guide_styles import *

summary=json.loads((ROOT.parent/'TestResults/summary.json').read_text(encoding='utf-8-sig'))
trx=ET.parse(ROOT.parent/'TestResults/acceptance.trx')
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
result=next(x for x in trx.findall('.//t:UnitTestResult',ns) if 'CompleteSUC' in x.attrib.get('testName',''))
log=result.find('t:Output/t:StdOut',ns).text
match=re.search(r'DETAILED DESIGN TEST PASSED: (\d+) assertions',log)
assertions=11+57+30+29+23+47+int(match[1])

# Short, repeated labels let the presenter follow the manual while clicking.
def label(doc,name,text):
    para=doc.add_paragraph()
    para.add_run(name+'  ').bold=True
    para.add_run(text)

def block(doc,title,actions,expected,spoken,reference):
    h(doc,title)
    steps(doc,actions)
    label(doc,'Bạn phải thấy',expected)
    label(doc,'Bạn nói','“'+spoken+'”')
    label(doc,'Mở thiết kế','Bảng '+reference+' trong mục 4.9 của file thiết kế chi tiết.docx.')


d=base('Hướng dẫn chạy và tập demo',
 'Đọc file này khi thao tác trên website. Bạn chỉ cần đi theo một câu chuyện: tạo chuyến đi Đà Lạt, lên lịch, gửi yêu cầu dịch vụ và thống nhất với nhà cung cấp. Mỗi chặng đều có kết quả cần thấy và câu nói ngắn để trình bày.')
h(d,'1 Mở website và chuẩn bị hai vai trò')
steps(d,[
 'Mở thư mục Ho tro khach du lich tu tuc. Nhấp đúp start-demo.cmd. Chờ dòng Website sẵn sàng hoặc Website đang chạy.',
 'Mở http://localhost:5080 bằng Chrome cho KHÁCH. Mở cùng địa chỉ bằng Edge hoặc cửa sổ ẩn danh cho NHÀ CUNG CẤP. Hai tab thường trong cùng trình duyệt sẽ dùng chung tài khoản.',
 'Đăng nhập theo bảng dưới. Trong lúc demo, giữ hai cửa sổ này để chuyển qua lại. Không bấm đổi vai trò trên cùng cửa sổ nếu muốn giữ cả hai bên.',
 'Mở thiết kế chi tiết.docx trong Word. Nhấn Ctrl+F, nhập số bảng như 4.179 rồi chọn kết quả đúng bảng. Chỉ mở bảng liên quan đến thao tác đang trình bày.'
])
table(d,['Cửa sổ','Tài khoản','Mật khẩu'],[
 ['Khách','khach@demo.vn','Demo12345'],['Nhà cung cấp','ncc@demo.vn','Demo12345']], [1.5,2.8,2.6])
h(d,'Nếu mở từ PowerShell')
code(d,'cd "C:\\Users\\bakav\\Demo KLTN\\Ho tro khach du lich tu tuc"\n.\\start-demo.cmd')
p(d,'Máy cần .NET SDK 10 và SQL Server LocalDB MSSQLLocalDB. Trên máy hiện tại đã có môi trường chạy. Không cần xóa DB hoặc mở thêm dotnet run khi website đang hoạt động.')
h(d,'Bạn sẽ demo theo thứ tự này')
p(d,'Tạo kế hoạch → Thêm hoạt động → Chấp nhận kế hoạch → Xem trước yêu cầu → Gửi yêu cầu → NCC đề xuất khác → Khách đồng ý → Xem lịch sử.')
p(d,'NCC nghĩa là nhà cung cấp. Mã # trên màn hình là số của bản ghi; mỗi lần bạn tạo mới sẽ có số khác. Không cần nhớ các số # của lượt demo trước.')

page(d,'2 Tạo một chuyến đi mới')
block(d,'Bước 1 Khách tạo kế hoạch',[
 'Ở cửa sổ KHÁCH, chọn Kế hoạch của tôi. Trong khung Tạo kế hoạch mới, nhập các giá trị trong bảng dưới.',
 'Bấm Tạo kế hoạch nháp. Hộp hỏi hiện ra: bấm Quay lại một lần để cho thấy có thể kiểm tra nội dung. Sau đó bấm tạo lại và chọn Có, xác nhận.'
], 'Trang chi tiết chuyến đi mở ra; có tên bạn vừa nhập và nhãn Nháp.',
 'Em tạo một kế hoạch dự kiến. Hệ thống lưu kế hoạch cho tài khoản đang đăng nhập và cho phép bổ sung lịch trình sau.', '4.179')
table(d,['Ô trên màn hình','Bạn nhập'],[
 ['Tên chuyến đi','Demo Đà Lạt 01'],['Điểm đến','Đà Lạt'],
 ['Ngày bắt đầu','10/11/2026'],['Ngày kết thúc','12/11/2026'],['Ngân sách dự kiến','1000000']], [2.4,4.5])
p(d,'Các ngày trên dùng cho lượt chuẩn bị ngày 03/11/2026. Nếu demo vào ngày khác, chọn ba ngày liên tiếp trong tương lai; các hoạt động phải nằm trong khoảng đó. Lượt tiếp theo đổi tên thành Demo Đà Lạt 02 để dễ tìm.')
h(d,'Ghi lại số kế hoạch')
p(d,'Nhìn địa chỉ trình duyệt, ví dụ /Journey/Details/1007 thì số kế hoạch là 1007. Ghi số thật của bạn ra giấy; sau này dùng để tìm lại và xem DB.')
h(d,'Điểm cần hiểu ở bước này')
p(d,'Nháp nghĩa là kế hoạch đang soạn. Bạn chưa đặt dịch vụ và chưa thanh toán. Ngân sách được phép để trống; trong bài demo nhập 1 triệu để nhìn thấy cảnh báo ở bước sau.')

page(d,'3 Thêm hoạt động và chấp nhận kế hoạch')
block(d,'Bước 2 Thêm hai hoạt động',[
 'Trong trang kế hoạch vừa tạo, kéo đến Thêm hoạt động. Chọn Tự nhập hoạt động, giữ Khung giờ là Có giờ cụ thể.',
 'Nhập Hoạt động 1 theo bảng, bấm Thêm vào lịch trình rồi xác nhận. Sau khi trang tải lại, thêm Hoạt động 2 theo cách tương tự.'
], 'Có hai mục trong ngày đầu; tổng chi phí là 1.200.000 đồng. Màn hình nhắc trùng giờ và vượt ngân sách 200.000 đồng.',
 'Hai mục cùng bắt đầu lúc 08 giờ và tổng vượt mức dự kiến. Hệ thống cảnh báo để khách cân nhắc; vẫn lưu được lịch trình.', '4.180 và 4.181')
table(d,['Trường','Hoạt động 1','Hoạt động 2'],[
 ['Nội dung hoạt động','Tham quan buổi sáng','Trải nghiệm địa phương'],
 ['Bắt đầu','10/11/2026 08:00','10/11/2026 08:00'],
 ['Kết thúc','10/11/2026 09:00','10/11/2026 10:00'],
 ['Chi phí dự kiến','700000','500000']], [1.9,2.5,2.5])
p(d,'Nếu bạn đã đổi khoảng ngày ở bước 1 thì đổi ngày của cả hai hoạt động theo ngày đầu chuyến đi. Khi thêm mới, ô Thứ tự hiển thị “Tự xếp cuối ngày”; sau khi thêm, mở Chỉnh sửa hoạt động nếu muốn đổi số thứ tự.')
block(d,'Bước 3 Chấp nhận kế hoạch',[
 'Bấm Chấp nhận kế hoạch trong khung ngân sách; chọn Có, xác nhận.',
 'Chỉ nhãn Đã chấp nhận. Nếu được hỏi sâu, mở sửa một hoạt động, đổi ghi chú rồi lưu: kế hoạch sẽ về Nháp. Chấp nhận lại khi đã xem xong.'
], 'Trạng thái đổi thành Đã chấp nhận, dù cảnh báo vẫn còn.',
 'Theo thiết kế, cảnh báo không chặn khách chấp nhận. Việc này chỉ chốt kế hoạch dự kiến; muốn dùng dịch vụ thì phải gửi yêu cầu riêng.', '4.185')

page(d,'4 Khách gửi yêu cầu dịch vụ')
block(d,'Bước 4 Nhập và xem lại yêu cầu',[
 'Ở cửa sổ KHÁCH, chọn Tìm dịch vụ. Mở Khám phá Đà Lạt cùng người bản địa của Đà Lạt Local; kiểm tra có nhãn Đang tiếp nhận yêu cầu.',
 'Nhập ngày bắt đầu 10/11/2026, số người 2, số lượng 2; giờ mong muốn 08:00. Ngày kết thúc có thể để trống. Hạng mục chọn thêm: dòng 1 Đón khách, dòng 2 Bữa sáng. Ghi chú: Demo Đà Lạt 01.',
 'Bấm Tiếp tục kiểm tra yêu cầu. Đọc ngày, số lượng, tổng và cảnh báo. Bấm Quay lại sửa để cho thấy dữ liệu vẫn còn; rồi bấm kiểm tra lại.'
], 'Màn hình Kiểm tra trước khi gửi. Ở đây bạn đang xem lại, chưa có yêu cầu mới gửi cho NCC.',
 'Bước xem trước giúp khách kiểm tra toàn bộ điều kiện. Hệ thống chưa ghi nhận đặt thành công và chưa bảo đảm còn chỗ.', '4.218 và 4.219')
h(d,'Hiểu rõ hai ô dễ nhầm')
p(d,'Số người là số khách tham gia. Số lượng là số đơn vị dịch vụ muốn yêu cầu; với lưu trú có thể mang nhãn Số phòng. Tổng ban đầu lấy đơn giá đang công bố nhân số lượng, chưa tự tính số đêm hoặc phụ phí từng hạng mục.')
block(d,'Bước 5 Xác nhận gửi',[
 'Tại màn hình xem trước, bấm Tôi đã kiểm tra, xác nhận gửi.',
 'Ghi lại số yêu cầu # trên trang mới. Đây là số mà NCC cần mở ở bước tiếp theo; đừng chọn nhầm một yêu cầu mẫu cũ.'
], 'Trang riêng của yêu cầu mở ra với trạng thái Chờ phản hồi và Điều kiện phiên bản 1.',
 'Khách đã gửi điều kiện mong muốn. Yêu cầu vẫn chờ nhà cung cấp trả lời; chưa phải hai bên đã thống nhất.', '4.220')
p(d,'Màn hình xem trước đã là bước xác nhận riêng, nên nút gửi cuối không mở thêm một hộp hỏi giống nút tạo kế hoạch. Nếu chờ quá lâu và bản xem trước hết hạn, quay lại kiểm tra rồi gửi lại.')

page(d,'5 Hai bên thống nhất điều kiện')
block(d,'Bước 6 Nhà cung cấp đề xuất khác',[
 'Chuyển sang cửa sổ NCC. Chọn Yêu cầu của khách và mở đúng số # vừa ghi. Nếu chưa thấy, tải lại trang.',
 'Mở Đề xuất điều kiện khác. Đổi ngày bắt đầu sang 11/11/2026 và Tổng chi phí điều kiện đang trao đổi thành 975000. Giữ các ô còn lại.',
 'Nhập lời giải thích: Đổi ngày theo khả năng phục vụ. Bấm Gửi bản điều kiện khác cho khách rồi xác nhận.'
], 'Yêu cầu ở trạng thái Đề xuất khác; số phiên bản điều kiện là 2. Khách chưa đồng ý bản mới.',
 'Nhà cung cấp không nhận nguyên yêu cầu mà đưa phương án khác. Hệ thống tăng phiên bản và giữ điều kiện ban đầu để đối chiếu.', '4.225')
block(d,'Bước 7 Khách chấp thuận đúng bản',[
 'Quay về cửa sổ KHÁCH, tải lại đúng trang yêu cầu. Đọc ngày 11/11 và tổng 975.000 đồng của bản 2.',
 'Bấm Chấp thuận phiên bản 2 rồi Có, xác nhận. Sau đó mở Lịch sử các phiên bản điều kiện.'
], 'Đã xác nhận; lịch sử có bản 1 và bản 2, kèm lý do thay đổi. Không phải đợi NCC xác nhận thêm lần nữa.',
 'NCC đã đồng ý bản do mình đề xuất. Khi khách đồng ý đúng bản đó, cả hai phía cùng chấp thuận nên hệ thống xác nhận ngay theo Bảng 4.226.', '4.226')
h(d,'Bạn chỉ lịch sử và kết thúc')
p(d,'Chỉ tổng/ngày của bản 1 rồi bản 2 để chứng minh điều kiện cũ không bị mất. Dừng tại Đã xác nhận; ghi kết quả thực hiện hoặc đổi/hủy dịch vụ là các luồng khác, mở thêm khi thầy yêu cầu.')
label(d,'Câu kết','“Luồng vừa rồi đi từ kế hoạch cá nhân đến thỏa thuận dịch vụ. Mỗi bước kiểm tra quyền và điều kiện; lịch sử cho biết hai bên đã đồng ý phiên bản nào.”')

page(d,'6 Chọn thời lượng và xử lý khi bị vướng')
table(d,['Thời lượng','Cách trình bày'],[
 ['5 phút','Chuẩn bị kế hoạch trước, nói rõ đã chuẩn bị. Chỉ cảnh báo và chấp nhận; dành phần lớn thời gian cho gửi yêu cầu → NCC đề xuất → khách đồng ý → lịch sử.'],
 ['8 phút','1 phút tạo kế hoạch; 2 phút hoạt động/cảnh báo; 1,5 phút xem trước/gửi; 2 phút NCC/khách thống nhất; 1,5 phút lịch sử và giải thích.'],
 ['12 phút','Làm trọn các bước; thêm một lỗi ngày ngoài chuyến đi và một lần sửa đưa kế hoạch về Nháp.']], [1.2,5.7])
h(d,'Nếu màn hình khác hướng dẫn')
table(d,['Bạn gặp gì','Bạn làm gì'],[
 ['Website đã chạy hoặc báo cổng 5080','Mở localhost:5080. Không mở thêm server vào cùng cổng. Nếu là bản cũ, đóng đúng tiến trình/cửa sổ của bản đó rồi chạy lại launcher.'],
 ['Không thấy yêu cầu vừa gửi','Kiểm tra vai trò NCC, tải lại danh sách và tìm đúng số #. Dùng dịch vụ của tài khoản NCC đang đăng nhập.'],
 ['Báo phiên bản đã thay đổi','Tải lại đúng yêu cầu, đọc điều kiện mới rồi thao tác. Không gửi tiếp form cũ.'],
 ['Không còn tiếp nhận yêu cầu','Chọn dịch vụ đủ điều kiện trước buổi demo; không sửa DB để bỏ qua quy tắc.']], [2.0,4.9])
h(d,'Chỉ mở terminal khi thầy hỏi dữ liệu')
p(d,'Thay hai số dưới bằng số kế hoạch và yêu cầu của lượt vừa trình bày. Nếu chỉ demo giao diện, có thể bỏ phần này.')
code(d,'powershell -NoProfile -ExecutionPolicy Bypass -File .\\show-demo-data.ps1 `\n  -TripId 1006 -RequestId 8')
p(d,'Lệnh chỉ xem DB. Số 1006 và 8 là ví dụ trên máy hiện tại, không phải số cố định. Cách đọc output được giải thích ở trang 4 của file 02.')
label(d,'Trước khi vào phòng','Chạy thử một lượt; giữ hai cửa sổ đăng nhập; chọn ngày tương lai; dùng tên demo mới; mở sẵn Word tại Bảng 4.179, 4.185, 4.220 và 4.226.')
d.save(OUT/'01 Huong dan chay va demo.docx')


e=base('Giải thích hệ thống khi trình bày demo',
 'Đọc file này để hiểu điều vừa thao tác và trả lời khi thầy hỏi. Mỗi phần bắt đầu bằng cách nói dễ hiểu, sau đó mới đến tên kỹ thuật hoặc chỗ mở code. Không cần học thuộc tất cả tên hàm trước khi tập demo.')
h(e,'1 Nắm chắc ba kết quả khác nhau')
table(e,['Bạn làm gì','Kết quả','Bạn cần hiểu'],[
 ['Chấp nhận kế hoạch','Đã chấp nhận','Khách thấy lịch trình dự kiến phù hợp. Chưa gửi yêu cầu cho NCC.'],
 ['Gửi yêu cầu dịch vụ','Chờ phản hồi','Khách đã gửi điều kiện mong muốn. NCC chưa đồng ý.'],
 ['Hai bên nhận cùng điều kiện','Đã xác nhận','Hệ thống đã ghi nhận thỏa thuận dịch vụ giữa khách và NCC.']], [2.0,1.7,3.2])
label(e,'Nói trong 30 giây','“Hệ thống giúp khách tự lên lịch trình và làm việc với nhà cung cấp. Khách tạo kế hoạch, cân nhắc giờ và chi phí; sau đó gửi yêu cầu dịch vụ. Khi hai bên thống nhất cùng điều kiện, hệ thống xác nhận và giữ lịch sử.”')
h(e,'Bám vào bảng nào khi nói')
table(e,['Điểm đang giải thích','Bảng mở trong thiết kế'],[
 ['Tạo kế hoạch','4.179'],['Hoạt động, tổng tiền và cảnh báo','4.180 và 4.181'],
 ['Chấp nhận kế hoạch','4.185'],['Xem trước và gửi yêu cầu','4.219 và 4.220'],
 ['NCC đề xuất khác','4.225'],['Khách đồng ý và xác nhận','4.226']], [4.6,2.3])
p(e,'Trong Word, Ctrl+F tìm số bảng và xem nội dung xử lý. Các vị trí hình trống trong bản xuất của thiết kế không dùng để suy ra thêm quy tắc. Bảng xử lý là chỗ đối chiếu của kịch bản này.')

page(e,'2 Vì sao lập kế hoạch xử lý như vậy')
qa(e,'Vì sao tạo xong lại là Nháp',
 'Vì khách mới lưu thông tin chuyến đi, còn có thể thêm và sửa hoạt động. Bảng 4.179 quy định bắt đầu ở Nháp.')
qa(e,'Vì sao trùng giờ hoặc vượt tiền vẫn cho chấp nhận',
 'Bảng 4.185 cho khách quyết định sau khi xem cảnh báo. Ngày hoạt động nằm ngoài chuyến đi bị chặn; trùng giờ và vượt ngân sách là thông tin để khách cân nhắc.')
qa(e,'Hệ thống tính tiền và cảnh báo giờ như thế nào',
 'Tổng là cộng chi phí các hoạt động. Chưa nhập chi phí thì cộng như 0. Chỉ so vượt ngân sách khi có nhập ngân sách. Theo quy tắc hiện tại, hai mục cùng ngày và cùng giờ bắt đầu sẽ bị nhắc trùng giờ.',
 'Ví dụ 700.000 + 500.000 = 1.200.000; ngân sách 1.000.000 thì vượt 200.000. Hai khoảng giao nhau nhưng khác giờ bắt đầu không thuộc phép kiểm tra trùng giờ này.')
qa(e,'Để trống ngân sách có giống nhập 0 không',
 'Không. Để trống nghĩa là chưa xác định ngân sách. Nhập 0 nghĩa là có đặt ngân sách bằng 0; nếu có chi phí thì sẽ bị cảnh báo vượt. Đây là lý do DB dùng cột tiền cho phép NULL.')
qa(e,'Sửa hoặc xóa hoạt động có ảnh hưởng gì',
 'Kế hoạch trở về Nháp để khách xem lại. Xóa chỉ được khi hoạt động thuộc khách và chưa gắn với tình huống phát sinh. Thu hẹp ngày cũng không được làm hoạt động đang có rơi ra ngoài khoảng mới.')
label(e,'Nếu cần chỉ code','Mở Services/JourneyManager.cs: Save để tạo/sửa kế hoạch; Activity để thêm/sửa mục; Detail để tính cảnh báo; Confirm để chấp nhận. Các điểm tương ứng ở Bảng 4.179–4.185.')

page(e,'3 Vì sao yêu cầu dịch vụ có nhiều bước')
h(e,'Xem trước rồi mới gửi')
p(e,'Xem trước giống đọc lại phiếu yêu cầu trước khi nộp. Người dùng có thể sửa, còn server kiểm tra ngày, số người, số lượng và quyền nhận yêu cầu của dịch vụ. Bước này chưa thêm yêu cầu vào DB. Bấm xác nhận gửi mới lưu trạng thái Chờ phản hồi.')
qa(e,'Vì sao xem trước đúng mà gửi sau đó vẫn có thể lỗi',
 'Trong lúc khách xem, dịch vụ có thể đổi giá hoặc dừng nhận. Vì vậy lúc gửi server phải kiểm tra lại. Bản xem trước có hạn 10 phút trong cách triển khai hiện tại; hết hạn thì xem lại, không phải NCC bắt buộc trả lời trong 10 phút.')
h(e,'NCC đề xuất khác thì chuyện gì xảy ra')
p(e,'Khách gửi bản 1, ví dụ ngày 10/11 và 900.000 đồng. NCC đổi thành ngày 11/11, tổng 975.000 đồng: hệ thống tạo bản 2, giữ bản 1 và chờ khách phản hồi. Lý do đổi phải được nhập để khách hiểu phương án mới.')
table(e,['Thao tác','Trạng thái sau'],[
 ['Khách gửi bản 1','Chờ phản hồi'],['NCC nhận nguyên bản 1','Đã xác nhận'],
 ['NCC đưa phương án bản 2','Đề xuất khác'],['Khách đồng ý đúng bản 2','Đã xác nhận'],
 ['Khách không đồng ý đề xuất','Không tiếp tục']], [4.5,2.4])
qa(e,'Có cần NCC xác nhận thêm sau khi khách nhận bản 2 không',
 'Không. Bảng 4.226 coi NCC đã đồng ý bản do chính mình đề xuất. Khách đồng ý đúng bản đó thì đủ hai bên và xác nhận trực tiếp.')
qa(e,'Khách muốn sửa yêu cầu thay vì nhận đề xuất thì sao',
 'Khi chưa xác nhận, khách có thể gửi lại điều kiện. Hệ thống tăng số bản, trở về Chờ phản hồi; đề xuất cũ không thể được chấp thuận nữa. Sau khi đã xác nhận thì phải dùng luồng đề nghị thay đổi hoặc hủy.')

page(e,'4 Phiên bản và lịch sử được hiểu thế nào')
h(e,'Số phiên bản cho biết hai bên đang nói về điều kiện nào')
p(e,'Hãy tưởng tượng một phiếu yêu cầu có các bản sửa 1, 2, 3. Câu “tôi đồng ý” phải gắn với đúng bản. Khách đồng ý bản 1 không có nghĩa đồng ý giá hoặc ngày mà NCC vừa đổi ở bản 2.')
table(e,['Tên trong DB','Cách hiểu','Sau khi xác nhận bản 2'],[
 ['ConditionsVersion','Điều kiện hiện tại là bản mấy','2'],
 ['CustomerAcceptedVersion','Khách đã đồng ý bản mấy','2'],
 ['ProviderAcceptedVersion','NCC đã đồng ý bản mấy','2']], [2.6,2.8,1.5])
h(e,'Rowversion giúp không ghi đè khi hai người cùng thao tác')
p(e,'Ví dụ A và B cùng mở một kế hoạch. A sửa trước; trang B vẫn chứa dữ liệu cũ. Nếu B lưu ngay thì có thể làm mất thay đổi của A. SQL Server cấp một dấu phiên bản cho bản ghi; form mang dấu cũ sẽ bị chặn và yêu cầu tải lại. Đây là rowversion, khác số bản điều kiện 1, 2, 3.')
h(e,'Lịch sử và giao dịch bảo vệ dữ liệu')
p(e,'Lịch sử giữ nội dung từng bản để đối chiếu. Khi NCC đề xuất khác, nhật ký SK11 có phần Trước và Sau. Thông báo giúp bên còn lại mở đúng yêu cầu. Các lần lưu liên quan được gom trong giao dịch: chỉ hoàn tất khi toàn bộ lần ghi thành công; lỗi thì hoàn tác.')
h(e,'Đọc lệnh xem DB trong file 01')
steps(e,[
 'Ở phần YEU CAU, chỉ Status=Đã xác nhận và ba số phiên bản đều bằng 2.',
 'Ở LICH SU DIEU KIEN, chỉ bản 1 và 2 có ngày hoặc tổng khác nhau.',
 'Ở NHAT KY DE XUAT, tìm SK11 và nội dung Before/After. Ở THONG BAO, chỉ loại thông báo và đường dẫn đúng số yêu cầu.'
])
p(e,'Dùng số # của lượt mới. Dữ liệu từ trước bản cập nhật có thể thiếu nội dung nhật ký mới; không tự bù lịch sử bằng suy đoán. Lệnh này chỉ SELECT để xem DB, không đổi trạng thái.')

page(e,'5 Giải thích kiến trúc và kiểm tra quyền')
h(e,'Một lần bấm nút đi qua đâu')
steps(e,[
 'Trình duyệt gửi form: người dùng nhập thông tin rồi bấm nút.',
 'Controller nhận form và lấy người đang đăng nhập. Ví dụ JourneyController nhận thao tác của kế hoạch.',
 'Manager kiểm tra dữ liệu, quyền trên đối tượng, trạng thái và phiên bản; quyết định có được lưu hay không.',
 'EF Core thông qua AppDbContext đọc/ghi SQL Server LocalDB. Thành công thì server dựng lại trang để người dùng thấy kết quả.'
])
label(e,'Cách trả lời kiến trúc','“Em dùng ASP.NET Core MVC với Razor cho giao diện, các Manager cho xử lý nghiệp vụ, EF Core để truy cập SQL Server LocalDB. Trình duyệt gửi form lên server; dữ liệu được lưu ở DB, không chỉ giữ trong trình duyệt.”')
table(e,['Từ kỹ thuật','Nói dễ hiểu'],[
 ['MVC','Cách tổ chức phần nhận yêu cầu, dữ liệu và trang hiển thị'],
 ['Controller','Nơi nhận thao tác từ trình duyệt'],['Manager hoặc Service','Nơi áp dụng các quy tắc nghiệp vụ'],
 ['EF Core','Thư viện giúp mã C# làm việc với DB'],['LocalDB','SQL Server chạy trên máy demo'],
 ['CSRF token','Mã kiểm tra form thuộc phiên đang thao tác']], [2.0,4.9])
qa(e,'Ẩn nút là đủ để bảo vệ chức năng chưa',
 'Chưa. Người dùng có thể tự sửa URL hoặc gửi form ngoài giao diện. Server vẫn kiểm tra vai trò và đúng chủ sở hữu. Một khách không được mở yêu cầu riêng của khách khác.')
qa(e,'NCC được xem liên hệ của khách từ lúc nào',
 'Theo Bảng 4.224, chỉ ở yêu cầu đã xác nhận thuộc dịch vụ của NCC đó. Một yêu cầu đã xác nhận không được làm lộ liên hệ trên yêu cầu khác còn chờ, kể cả cùng khách.')

page(e,'6 Câu trả lời về lỗi và kiểm thử')
table(e,['Thầy hỏi hoặc hệ thống báo','Bạn có thể trả lời'],[
 ['Vì sao có hộp hỏi trước khi thêm/sửa/bỏ','Để người dùng kiểm tra trước khi gửi. Quay lại không ghi thay đổi; server vẫn kiểm tra mọi điều kiện khi nhận form.'],
 ['Lỗi 400 là gì','Dữ liệu nhập chưa hợp lệ, ví dụ số người vượt 100 hoặc sai kiểu.'],
 ['Lỗi 404 là gì','Không tìm thấy đối tượng trong phạm vi người dùng được xem.'],
 ['Lỗi 409 là gì','Dữ liệu hoặc phiên bản đã thay đổi; cần tải lại và đọc bản mới.'],
 ['Lỗi 422 là gì','Dữ liệu có thể đọc được nhưng vi phạm nghiệp vụ, ví dụ ngày hoạt động ngoài chuyến đi.']], [2.4,4.5])
h(e,'Kết quả rà soát hiện tại')
p(e,f'Lần chạy {summary["Time"][:19]}: {summary["Passed"]}/{summary["Total"]} ca xUnit đạt, không có ca thất bại. Trong đó có 131 ca unit và một ca điều phối hệ thống. Bảy script trong ca hệ thống thực hiện {assertions} kiểm tra HTTP, kèm các kiểm tra SQL bổ sung. Line coverage {summary["LineCoveragePercent"]}%; branch coverage {summary["BranchCoveragePercent"]}%.')
p(e,'Lượt rà soát này đã sửa lỗi liên hệ xuất hiện nhầm giữa các yêu cầu cùng khách và xử lý số trang lớn gây tràn phép tính. Có ca hồi quy riêng cho cả hai. Kiểm tra trình duyệt xác nhận hộp hỏi hiện nội dung, Quay lại đóng hộp và giữ dữ liệu đang nhập.')
qa(e,'Đạt hơn 80 phần trăm coverage có nghĩa phần mềm hoàn chỉnh chưa',
 'Chưa. Coverage cho biết bao nhiêu dòng mã được chạy trong bộ test. Nhánh được báo riêng; nghiệm thu cần tester độc lập thực hiện tình huống, ghi kết quả thực tế. Hiện chưa có biên bản UAT.')
label(e,'Nếu cần mở bằng chứng','TestResults/summary.json và acceptance.trx là kết quả thật; docs/TEST-RESULTS.md giải thích số liệu. Không cộng số kiểm tra HTTP thành số ca xUnit riêng.')
qa(e,'Hệ thống có thanh toán hoặc giữ chỗ thực tế chưa',
 'Chưa có thanh toán, tồn kho hoặc kiểm tra chỗ còn thực tế. Tổng ban đầu là giá công bố nhân số lượng; các phụ phí và điều kiện khác cần hai bên thống nhất. Không mô tả Đã xác nhận thành đã trả tiền hoặc đã cung cấp xong.')

page(e,'7 Phạm vi báo cáo và chỗ mở code')
h(e,'Nói đúng về tiến độ A14')
p(e,'Có 18 SUC chi tiết trong 14 nhóm nghiệp vụ. Phương án A14 chọn 14/18 SUC, khoảng 77,78% theo số lượng, ưu tiên mục tiêu 1, 2 và 4; bốn SUC đồng hành được đề xuất để sau. Cần giảng viên chốt phương án này. Đếm SUC không phải tỷ lệ công sức hoặc tỷ lệ test đạt.')
p(e,'Luồng demo chính hỗ trợ mục tiêu 1 qua tra cứu và mục tiêu 2 qua lập lịch, chi phí, thương lượng. Nếu được hỏi mục tiêu 4, mở tình huống phát sinh hoặc luồng đề nghị đổi/hủy; không khẳng định đã chứng minh mục tiêu chỉ bằng việc có menu.')
h(e,'Mở những file này khi thầy hỏi mã nguồn')
table(e,['Chức năng','File và hàm cần tìm'],[
 ['Kế hoạch và hoạt động','Controllers/JourneyController.cs; Services/JourneyManager.cs: Save, Activity, Detail, Confirm'],
 ['Xem trước, gửi và phản hồi','Controllers/MarketplaceController.cs; ProviderController.cs; Services/MarketplaceBookings.cs: Preview, Book, Respond, ReplyOffer'],
 ['Giao dịch, phiên bản và snapshot','Services/MarketplaceManager.cs; MarketplacePreview.cs'],
 ['Bảng, quan hệ, audit và thông báo','Data/AppDbContext.cs; Models'],
 ['Trang người dùng nhìn thấy','Views/Journey/PlanDetails.cshtml; Views/Marketplace/Requests.cshtml']], [2.1,4.8])
p(e,'Trong VS Code, Ctrl+P nhập tên file, rồi Ctrl+F tìm tên hàm. Các đường dẫn trên tính từ thư mục dự án. Nếu thầy hỏi xử lý một nút, chỉ theo thứ tự Controller → Manager → DB → kết quả trên màn hình.')
h(e,'Điểm cần trình bày rõ khi đối chiếu thiết kế')
p(e,'Tên route/action trong code đang dùng tiếng Anh, ví dụ /Journey, khác tên tiếng Việt trong báo cáo. Dùng bảng ánh xạ ở docs/ban-giao-kiem-thu/DOI_CHIEU_THIET_KE.md. Hai luồng chính đã có hồi quy, nhưng chưa thể tuyên bố mọi action và ràng buộc DB đã khớp nguyên dạng thiết kế hoặc đã nghiệm thu.')
p(e,'Thiết kế có chỗ thống kê 88 action ở Bảng 4.172 và 91 ở Bảng 4.267; cần thống nhất khi sửa báo cáo. Khi giải thích demo, bám bảng xử lý cụ thể. Nếu chưa kiểm tra một nhánh, nói đúng phạm vi đã làm và mở ca kiểm thử tương ứng thay vì đoán.')
e.save(OUT/'02 Giai thich he thong va tra loi khi demo.docx')
print('Updated two plain-language manuals; HTTP assertions:',assertions)
