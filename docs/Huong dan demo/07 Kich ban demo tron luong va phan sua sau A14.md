# Kịch bản demo trọn luồng và phần đã sửa sau A14

File này nối tiếp file 06 (file 06 giải thích riêng SUC02.01 – Lập kế hoạch). File gồm:

1. Đã sửa gì so với bản báo cáo A14.
2. Bản đồ xử lý chung.
3. Chuẩn bị trước buổi demo.
4. Kịch bản demo trọn luồng: mỗi bước ghi làm gì trên web, mở file nào, dòng nào, nói câu gì.
5. Bảng tra cứu theo hàm.
6. Câu hỏi thầy có thể hỏi.
7. Giới hạn và việc còn tồn.

Lưu ý: số dòng là của source hiện tại (12/11/2026, nhánh `hoan-thien-sau-a14-v2`). Nếu lệch, mở đúng file rồi nhấn Ctrl+F tìm tên hàm.

---

## 1. Đã sửa gì so với bản báo cáo A14

Mốc so sánh: commit `aab5082` (06/11/2026), là bản đã demo trong buổi A14 (07/11/2026). Mọi thay đổi dưới đây làm sau buổi đó. CSDL giữ nguyên 25 bảng như Chương 4, không thêm bảng mới.

### 1.1. Nhận xét của hội đồng và cách xử lý

| Nhận xét A14 | Đã xử lý | Xem ở bước |
|---|---|---|
| Sản phẩm mới là khung giao diện và chức năng cơ bản | Hoàn thiện các chức năng còn thiếu trong SUC đã có (mục 1.2) | Cả phần 4 |
| Chưa demo được một luồng từ thao tác đầu đến kết quả cuối | Có luồng liền mạch: tra cứu → chọn dịch vụ → lập kế hoạch → gửi yêu cầu → xác nhận → xử lý phát sinh → ghi kết quả | Bước 1–7 |
| Còn nhiều phần để trống | Trang địa điểm có dịch vụ tại chỗ; dữ liệu mẫu có 7 điểm đến, mỗi nơi đủ 4 loại dịch vụ | Bước 1 |
| Kế hoạch đi Đà Lạt mà danh sách thêm hoạt động vẫn hiện nơi khác | Form chia 2 ô: chọn địa điểm tại điểm đến trước, ô sau chỉ hiện hoạt động và dịch vụ của địa điểm đó | Bước 3 |
| Form đặt xe, ăn uống vẫn ví dụ "nhận phòng 14:00" | Nhãn, ví dụ và đơn vị giá đổi theo loại dịch vụ | Bước 4 |
| Phía nhà cung cấp cũng phải có logic theo địa điểm | Ô "Nơi phục vụ" gợi ý từ cùng danh sách địa điểm với phía khách; Admin thấy cảnh báo nếu không khớp | Bước 8 |

### 1.2. Danh sách thay đổi theo mục tiêu

| Mục tiêu / SUC | Trước A14 | Sau A14 | Hàm chính |
|---|---|---|---|
| MT1 – SUC01.01 Tra cứu | Trang địa điểm chỉ có mô tả và giá gợi ý | Hiện dịch vụ tại nơi đó và địa điểm cùng khu vực | `HomeController.Place` |
| MT2 – SUC01.01 Chọn dịch vụ | Chỉ tìm theo chữ, lọc giá | Thêm lọc theo điểm đến và sắp xếp theo giá | `MarketplaceController.Index` |
| MT2 – SUC02.01 Lập kế hoạch | Danh sách gợi ý có mọi địa điểm, dịch vụ trong hệ thống | Chỉ gợi ý những gì ở điểm đến; chọn địa điểm trước rồi chọn hoạt động; thêm từ trang tra cứu chỉ hiện kế hoạch cùng điểm đến | `DestinationFilter`, `JourneyManager.Detail`, `JourneyManager.TripsForSource` |
| MT2 – SUC07.01 Gửi yêu cầu | Một bộ nhãn cho mọi loại; giá "/ đơn vị"; chi phí tự tính từ giá × số lượng | Nhãn và ví dụ theo loại; đơn vị phòng/đêm, suất, lượt, vé; **chi phí dự kiến do khách tự nhập, không bắt buộc** (Bảng 4.139, 4.203) | `ServiceFormHints`, `_Terms.cshtml` |
| SUC06.01 / SUC06.02 Dịch vụ | Nơi phục vụ gõ tự do | Gợi ý từ danh sách địa điểm; Admin thấy nơi phục vụ có khớp địa điểm không | `DestinationOptionsViewComponent`, `AdminController.Index` |
| Tổng quan của khách | Chuyến đang đi vẫn ghi "đang chuẩn bị", mở ngày 1; có lúc hiện nhầm chuyến khác | Ghi đúng Sắp tới / Đang diễn ra / Đã kết thúc, mở sẵn ngày hôm nay; ưu tiên chuyến đang diễn ra có nhiều hoạt động hôm nay nhất | `DashboardController.Index` |
| Mã lỗi và thông báo | Chỉ SUC02.01, 07.01 có mã lỗi | Đủ 98 mã lỗi Bảng 4.250 (`Services/ThongDiep.cs`), câu chữ nguyên văn; thông báo ghi đúng loại và đường dẫn Bảng 4.249 | `ThongDiep`, `ThongBaoLoai` |
| Dữ liệu mẫu | 4 địa điểm, ít dịch vụ | 26 địa điểm, 7 nhà cung cấp, hơn 30 dịch vụ đủ 4 loại, lịch sử yêu cầu đã hoàn thành, nhu cầu đồng hành | `DemoA14Seed`, `DemoFullDataSeed` |

**File mới:** `Services/DestinationFilter.cs`, `Services/ThongDiep.cs`, `Services/ThongBaoLoai.cs`, `Controllers/DesignLinksController.cs`, `Models/ServiceFormHints.cs`, `Data/DemoA14Seed.cs`, `Data/DemoFullDataSeed.cs`, `ViewComponents/DestinationOptionsViewComponent.cs`, `Views/Shared/Components/DestinationOptions/Default.cshtml`, `wwwroot/css/hoan-thien.css`.

**File đã sửa:** các Controller Admin, Community, Dashboard, Home, Journey, Marketplace; `Services/JourneyManager.cs`, `Services/MarketplaceManager.cs`, `Services/MarketplaceBookings.cs` (1 dòng); `Models/CommunityModels.cs`; `Program.cs`; một số View; `wwwroot/js/site.js`.

**Test:** chỉ sửa 1 dòng trong `planning-request-deep-test.mjs` (dòng 34): kịch bản lấy dịch vụ ở Đà Lạt thay vì dịch vụ đầu danh mục. Lý do: dữ liệu mẫu mới có dịch vụ của nhà cung cấp khác đứng đầu danh mục, mà kịch bản lại đăng nhập nhà cung cấp Đà Lạt để phản hồi. Các điều kiện kiểm tra giữ nguyên. Kết quả ngày 11/11/2026: **132/132 test đạt**, gồm 131 test nghiệp vụ và 1 test hệ thống chạy 7 kịch bản trên CSDL riêng (log ở `TestResults/chay-test-11-11-lan2.txt`).

### 1.3. Lịch sử commit

Toàn bộ phần sửa nằm ở nhánh `hoan-thien-sau-a14-v2` trên GitHub (repo `phamminhvu6728/KLTN`), nối tiếp commit `aab5082` của bản A14. Mỗi commit một phần việc và đều build được:

| # | Commit | Nội dung |
|---|---|---|
| 1 | `0190c66` | Bổ sung dữ liệu mẫu đầy đủ cho các điểm đến |
| 2 | `ca214a7` | Form gửi yêu cầu theo từng loại dịch vụ, đơn vị giá, tạm tính lưu trú |
| 3 | `b168c6f` | Lọc địa điểm và dịch vụ theo điểm đến của chuyến đi |
| 4 | `6e63dc2` | Gợi ý điểm đến dùng chung cho khách và nhà cung cấp |
| 5 | `87dadd0` | Trang địa điểm hiện dịch vụ tại chỗ (MT1) |
| 6 | `d4087ec` | Mức độ phù hợp khi tìm người đồng hành (đã gỡ sau đó vì ngoài fulltext) |
| 7 | `b71047b` | Trang Hỗ trợ tại điểm đến (đã gỡ sau đó vì ngoài fulltext) |
| 8 | `27babdc` | Tổng quan hiện đúng trạng thái chuyến đi |
| 9 | `68f37e5` | Kịch bản kiểm thử lập kế hoạch lấy dịch vụ ở Đà Lạt |
| 10 | `5136788` | Tài liệu hoàn thiện sau A14 và kịch bản demo |

Nếu thầy hỏi một phần làm thế nào, mở đúng commit đó trên GitHub để thấy những file đã đổi.

### 1.4. Không đổi

- Cấu trúc CSDL: 25 bảng, không thêm migration.
- Quy tắc lập kế hoạch: kiểm tra trùng giờ, vượt ngân sách, phiên bản (`JourneyManager`).
- Đặt và xác nhận dịch vụ: các trạng thái, mã lượt gửi, phiên bản điều kiện (`MarketplaceBookings`).
- Điều chỉnh, hủy và ghi kết quả (`MarketplaceChangesResults`).
- Kết nối và thỏa thuận đồng hành (`CommunityManager`).
- Xác minh và quyền hoạt động nhà cung cấp, đăng nhập, phân quyền.

Cột chi phí của yêu cầu (`Bookings.Total`, `BookingChanges.Total`) chuyển sang cho phép để trống bằng migration `AllowEstimatedCostNull`, đúng ChiPhiDuKien trong Bảng 4.139.

### 1.5. Đối chiếu với fulltext

Theo yêu cầu của thầy "thiết kế thế nào làm y hệt", nhóm đã gỡ hai chức năng làm thêm (trang Hỗ trợ tại điểm đến, mức độ phù hợp %), gỡ đánh giá dịch vụ, và làm chi phí yêu cầu đúng thiết kế. Phần còn lại là hoàn thiện bên trong các SUC đã có. Riêng lọc địa điểm theo điểm đến là theo góp ý của thầy ở A14.

Bảng đối chiếu đầy đủ từng SUC, 25 bảng CSDL, mã lỗi và kết quả bộ test của tester: `docs/DOI_CHIEU_FULLTEXT.md`.

## 2. Bản đồ xử lý

Mọi chức năng đi theo cùng một đường:

Giao diện Razor → Controller → Manager (Service) → AppDbContext → SQL Server LocalDB.

| Tầng | Vai trò | Ví dụ |
|---|---|---|
| View | Hiển thị và gửi form | `Views/Journey/Incident.cshtml` |
| Controller | Nhận dữ liệu, lấy người dùng từ cookie, gọi Manager | `JourneyController.Plan` |
| Manager | Kiểm tra quyền, trạng thái, quy tắc rồi đọc hoặc ghi; lỗi trả đúng mã Bảng 4.250 | `JourneyManager.Plan` |
| Model | Khai báo dữ liệu | `Trip`, `TravelService` |
| AppDbContext | Ánh xạ bảng và lưu xuống CSDL | `db.SaveChangesAsync()` |

Dữ liệu mẫu nạp ở dòng 103 và 105, chỉ khi bật chế độ demo.

Trong VS Code: Ctrl+P mở file, Ctrl+G nhảy đến dòng, Ctrl+Shift+O tìm tên hàm.

---

## 3. Chuẩn bị trước buổi demo

1. Dừng website cũ đang chạy ngầm, vì tắt terminal không làm website dừng. Trong terminal VS Code chạy:

        Stop-Process -Id (Get-NetTCPConnection -LocalPort 5080 -State Listen).OwningProcess -Force

2. Chạy lại `.\start-demo.cmd`. Script sẽ build bản mới rồi mở website. Bước này bắt buộc để có nhãn ngày mới và trang Tổng quan chọn đúng chuyến.
3. Mở trang bất kỳ, nhấn Ctrl+F5 để trình duyệt tải lại `site.js` mới.
4. Kiểm tra bản mới:
   - Form Thêm hoạt động có 2 ô "1. Địa điểm tại …" và "2. Hoạt động hoặc dịch vụ".
   - Form gửi yêu cầu ghi "Ngày trả phòng", "Ngày kết thúc"…, không còn "(nếu nhiều ngày)".
   - Trang Tổng quan của khách hiện chuyến "Đà Lạt cuối tuần (đang đi)", ghi "Chuyến đi đang diễn ra".
5. Mở sẵn 2 cửa sổ: website và VS Code. Chuyển vai trò bằng các nút Khách du lịch / Nhà cung cấp / Quản trị trên thanh đầu trang.
6. Tự bấm trọn phần 4 một lần. Bộ test tự động đã đạt, nhưng kịch bản demo này chưa được bấm thử liền một mạch.
7. Khi tập lại, chọn dịch vụ khác, vì yêu cầu đã tạo vẫn còn trong CSDL.

Tài khoản demo đều dùng mật khẩu `Demo12345`: `khach@demo.vn` (khách), `ncc@demo.vn` (nhà cung cấp Đà Lạt), `admin@demo.vn`, `lan@demo.vn` (khách có nhu cầu đi Hội An).

---

## 4. Kịch bản demo trọn luồng (khoảng 11 phút)

### Bước 0 (30 giây) — Mở đầu

Nói:

> Ở buổi A14, hội đồng nhận xét sản phẩm chưa chạy được một luồng hoàn chỉnh. Hôm nay em demo một luồng liền mạch: khách tìm thông tin, chọn dịch vụ, lập kế hoạch đúng điểm đến, gửi yêu cầu, nhà cung cấp xác nhận, xử lý tình huống phát sinh, và cuối cùng nhà cung cấp ghi kết quả để khách thấy dịch vụ đã hoàn thành. Dữ liệu đều do thao tác trực tiếp tạo ra.

### Bước 1 (1 phút) — Tra cứu thông tin (MT1, SUC01.01)

Web: chưa đăng nhập, vào **Tra cứu điểm đến**, tìm "Đà Lạt", mở địa điểm Đà Lạt, kéo xuống phần dịch vụ và địa điểm cùng khu vực.

Mở `Controllers/HomeController.cs`, hàm `Place` ở dòng 26. Dòng 33 lấy tên các địa điểm cùng khu vực. Dòng 41–42 ghép dịch vụ:

    var services = published
        .Where(s => Matches(s.Location, place.Name) || regionNames.Any(n => Matches(s.Location, n)))

Nói:

> Hàm Place đọc địa điểm, rồi lấy các dịch vụ đã duyệt tại đúng nơi đó hoặc nơi cùng khu vực. Khách xem được thông tin và dịch vụ trên một trang, không phải tìm lại. Phần này giải quyết IS1.

### Bước 2 (1 phút) — Chọn dịch vụ theo điểm đến (MT2, SUC01.01)

Web: vào **Tìm dịch vụ**, chọn **Điểm đến** "Đà Lạt", **Sắp xếp** "Giá thấp đến cao", bấm Tìm kiếm. Chỉ ra giá ghi theo đơn vị của loại: "/ phòng / đêm", "/ suất", "/ lượt", "/ vé".

Mở `Controllers/MarketplaceController.cs`, hàm `Index` ở dòng 12:

- Dòng 24: lọc theo điểm đến bằng `DestinationFilter`, cùng quy tắc với form lập kế hoạch.
- Dòng 25–27: sắp xếp theo giá.

Đơn vị giá lấy từ `Models/ServiceFormHints.cs`, hàm `Unit` ở dòng 9.

Nói:

> Chọn Đà Lạt thì hệ thống lấy cả dịch vụ ở các địa điểm thuộc Đà Lạt, ví dụ Đồi chè Cầu Đất. Giá ghi rõ đơn vị theo loại để khách so sánh đúng: phòng mỗi đêm, suất ăn, lượt xe, vé tham gia. Phần này giải quyết IS2.

### Bước 3 (1 phút 30) — Lập kế hoạch đúng điểm đến (SUC02.01, góp ý A14)

Web: chọn **Khách du lịch**, vào **Kế hoạch của tôi**, mở chuyến "Đà Lạt cuối tuần (đang đi)", kéo xuống **Thêm hoạt động**.

1. Mở ô **1. Địa điểm tại Đà Lạt**: chỉ có Chợ đêm Đà Lạt, Đà Lạt, Đồi chè Cầu Đất.
2. Chọn "Đồi chè Cầu Đất". Ô **2. Hoạt động hoặc dịch vụ** tự chọn "Tham quan Đồi chè Cầu Đất", ô địa chỉ tự điền.
3. Mở ô 2: chỉ còn hoạt động và dịch vụ của nơi đó, chia theo loại. Chọn một dịch vụ, ô chi phí tự điền theo giá.

Giải thích theo thứ tự xử lý:

**Server lọc theo điểm đến.** Mở `Services/JourneyManager.cs`, hàm `Detail` ở dòng 22. Dòng 49 giữ lại mục mà hoạt động cũ đang dùng. Dòng 51–52:

    var placesHere = DestinationFilter.PlacesAt(allPlaces, t.Destination);
    var servicesHere = DestinationFilter.ServicesAt(allServices, placesHere, t.Destination);

**Quy tắc lọc.** Mở `Services/DestinationFilter.cs`:

| Dòng | Hàm | Việc |
|---|---|---|
| 12 | `PlacesAt` | Tìm địa điểm trùng tên điểm đến để biết khu vực (Đà Lạt thuộc Lâm Đồng), rồi lấy mọi địa điểm cùng khu vực |
| 20 | `ServicesAt` | Lấy dịch vụ phục vụ tại điểm đến hoặc tại một địa điểm ở dòng 12 |
| 28 | `ServesPlace` | Một dịch vụ thuộc một địa điểm nếu phục vụ ngay tại đó, hoặc phục vụ chung cho cả điểm đến |
| 32 | `Same` | Hai tên được coi là cùng nơi khi tên này chứa tên kia, không phân biệt hoa thường |

**View gắn dịch vụ với địa điểm.** Mở `Views/Journey/ActivityFields.cshtml`: dòng 9 `PlacesOf` gắn cho mỗi dịch vụ danh sách mã địa điểm. Dòng 13 chọn sẵn địa điểm khi sửa hoạt động cũ. Dòng 34 là ô 1, dòng 44 là ô 2.

**Trình duyệt ẩn mục không thuộc địa điểm.** Mở `wwwroot/js/site.js`: dòng 131 `filterActivityOptions`, dòng 138 là điều kiện ẩn, dòng 154 `fillActivityCost` điền chi phí.

**Server vẫn kiểm tra khi lưu.** Mở `Services/JourneyManager.cs`, hàm `Activity` ở dòng 156. Dòng 178 và 188 kiểm tra địa điểm, dịch vụ đã chọn còn được công bố.

Nói:

> Ở A14 thầy góp ý chọn Đà Lạt mà vẫn hiện nơi khác. Giờ server lọc trước theo điểm đến của chuyến đi, nên ô địa điểm chỉ có nơi ở Đà Lạt. Khách chọn địa điểm thì ô hoạt động chỉ hiện những gì ở đó. Ô địa điểm chỉ để lọc, không gửi lên server. Server vẫn nhận nguồn đã chọn và kiểm tra lại trong hàm Activity như cũ. Hoạt động cũ đã chọn nơi khác vẫn hiện khi sửa để không mất dữ liệu.

Nếu được hỏi thêm: bấm "Thêm vào lịch trình" ở trang một địa điểm hoặc dịch vụ thì `JourneyController.ChooseSource` (dòng 51) gọi `JourneyManager.TripsForSource` (dòng 76). Trang chọn kế hoạch chỉ hiện các kế hoạch cùng điểm đến.

### Bước 4 (1 phút 30) — Khách gửi yêu cầu (SUC07.01)

Web: mở một dịch vụ ở Đà Lạt. Chỉ ra nhãn form đổi theo loại: lưu trú hỏi "Ngày nhận phòng", "Số phòng"; ăn uống hỏi "Giờ đến quán", "Số suất"; đặt xe hỏi "Giờ và điểm đón". Chọn **ngày hôm nay**, bấm tiếp tục, xem lại rồi gửi.

Phải chọn ngày hôm nay, vì nhà cung cấp chỉ ghi được kết quả khi ngày dùng không sau hôm nay (bước 7).

**Nhãn form.** Mở `Views/Marketplace/_Terms.cshtml` dòng 3, gọi `ServiceFormHints.For` (`Models/ServiceFormHints.cs` dòng 18).

**Chi phí dự kiến.** Ô "Chi phí dự kiến (₫, không bắt buộc)" do khách tự nhập, đúng `chiPhiDuKien: decimal?` trong Bảng 4.203–4.204. Hệ thống không tự tính tiền.

**Xem trước và gửi.** Mở `Services/MarketplaceBookings.cs`:

- Dòng 7, `Preview`: kiểm tra điều kiện và cảnh báo, chưa ghi CSDL. 
- Dòng 23, `Book`: ghi yêu cầu. Dòng 26 chặn gửi trùng bằng mã lượt gửi. Dòng 32 tạo bản ghi ở trạng thái Chờ phản hồi.

Nói:

> Nhãn và ví dụ lấy theo loại dịch vụ, nên đặt xe không còn hỏi giờ nhận phòng. Chi phí dự kiến do khách tự nhập và có thể để trống, đúng thiết kế; giá cuối do hai bên thống nhất. Bước Preview chỉ kiểm tra, bấm gửi mới vào hàm Book. Mỗi lần mở biểu mẫu có một mã lượt gửi nên bấm nhiều lần không tạo trùng. Chờ phản hồi chưa phải là đặt thành công.

### Bước 5 (45 giây) — Nhà cung cấp xác nhận

Web: chọn **Nhà cung cấp**, vào **Yêu cầu của khách**, mở yêu cầu vừa gửi, bấm xác nhận.

Mở `Controllers/ProviderController.cs` dòng 36, hàm `Respond`. Mở `Services/MarketplaceBookings.cs` dòng 55, hàm `Respond`. Dòng 78 đổi trạng thái:

    booking.Status="Đã xác nhận";

Nói:

> Nhà cung cấp chỉ xử lý được yêu cầu của dịch vụ mình sở hữu. Hàm kiểm tra phiên bản điều kiện rồi mới chuyển sang Đã xác nhận, nên không xác nhận nhầm điều kiện cũ.

### Bước 6 (1 phút 30) — Ghi nhận tình huống phát sinh (MT4, SUC05.01)

Web: chọn **Khách du lịch**, vào **Tình huống phát sinh**, chọn chuyến "Đà Lạt cuối tuần (đang đi)", chọn loại Thời tiết, nhập mô tả, lưu. Mở tình huống, ghi phương án, rồi ghi kết quả.

Mở `Services/JourneyManager.cs`: hàm `AddIncident`, `Plan`, `Result` (Ctrl+Shift+O tìm tên hàm). Thứ tự kiểm tra đúng thiết kế:

| Hàm | Kiểm tra theo thứ tự | Mã lỗi |
|---|---|---|
| `AddIncident` | Kế hoạch của mình → loại và mô tả → hoạt động thuộc kế hoạch | 0501-01, 0501-02, 0501-03 |
| `Plan` | Tình huống của mình → đã kết thúc → phiên bản | 0501-01, 0501-04, 0501-05 |
| `Result` | Tình huống của mình → đã kết thúc → phiên bản → mô tả kết quả | 0501-01, 0501-04, 0501-05, 0501-06 |

Nói:

> Tình huống tách riêng phương án và kết quả. Chọn phương án chưa có nghĩa là đã giải quyết, phải ghi kết quả thực tế. Mỗi lỗi trả đúng mã trong Bảng 4.250, ví dụ tình huống đã kết thúc thì trả 0501-04. Phần này giải quyết IS4.

### Bước 7 (1 phút) — Nhà cung cấp ghi kết quả, điểm kết của luồng (SUC09.01)

Web: chọn **Nhà cung cấp**, mở lại yêu cầu, chọn kết quả "Hoàn thành", nhập ghi chú, lưu. Sau đó chọn **Khách du lịch**, vào **Yêu cầu dịch vụ**: yêu cầu hiện "Hoàn thành" kèm kết quả từng hạng mục.

Mở `Controllers/ProviderController.cs` dòng 38, hàm `Results`. Mở `Services/MarketplaceChangesResults.cs`: dòng 83 `RecordResults`, dòng 103 tổng hợp trạng thái:

    booking.Status=AggregateResultStatus(results,items.Count);

Hàm `AggregateResultStatus` ở dòng 75.

Nói:

> Nhà cung cấp ghi kết quả theo từng hạng mục. Trạng thái chung được tổng hợp từ các hạng mục, chỉ Hoàn thành khi mọi hạng mục đều đầy đủ. Khách thấy đúng kết quả thực tế, khép lại luồng từ lúc tìm thông tin đến khi dịch vụ được thực hiện.

### Bước 8 (1 phút) — Phía nhà cung cấp và Admin dùng cùng logic địa điểm (SUC06.01, SUC06.02)

Web: chọn **Nhà cung cấp**, vào **Dịch vụ của tôi**, bấm **+ Thêm dịch vụ**. Bấm vào ô **Nơi phục vụ**: hiện gợi ý tên địa điểm kèm khu vực. Sau đó chọn **Quản trị**: mỗi bản chờ duyệt có dòng cho biết nơi phục vụ khớp địa điểm nào, hoặc cảnh báo nếu không khớp.

Mở `ViewComponents/DestinationOptionsViewComponent.cs` dòng 12, `InvokeAsync`: đọc bảng Places và tạo danh sách gợi ý. Danh sách này dùng chung cho ô Điểm đến khi tạo kế hoạch, đăng nhu cầu đồng hành và ô Nơi phục vụ của nhà cung cấp.

Mở `Controllers/AdminController.cs` dòng 14, hàm `Index`, nạp danh sách địa điểm. `Views/Admin/Index.cshtml` dòng 5 dùng `DestinationFilter.Same` để so khớp.

Nói:

> Khách lọc theo địa điểm thì nhà cung cấp cũng phải ghi nơi phục vụ theo cùng danh sách, nếu không bộ lọc sẽ không khớp. Ô vẫn cho gõ tự do cho nơi chưa có trong hệ thống, nhưng khi duyệt, Admin thấy cảnh báo và có thể yêu cầu sửa.

### Bước 9 (1 phút) — Tìm và kết nối người đồng hành (MT3, SUC03.01, SUC04.01)

Web: vào **Đồng hành**, nhập Điểm đến "Hội An", Từ ngày 22/11/2026, Đến ngày 24/11/2026, bấm Tìm. Mở nhu cầu của Thu Lan, gửi đề nghị kết nối kèm lời giới thiệu. Đăng nhập `lan@demo.vn` để chấp nhận; sau khi chấp nhận, hai bên thấy thông tin liên hệ trong phạm vi chia sẻ.

Mở `Services/CommunityManager.cs`: hàm `Connect` kiểm tra 0401-01 (nhu cầu không còn công bố), 0401-02 (gửi cho chính mình), 0401-03 (đã có đề nghị chờ), 0401-04 (đã kết nối). Hàm `Respond` ghi thông báo đúng loại Bảng 4.249: DeNghiDuocChapNhan, DeNghiBiTuChoi, DeNghiDaRut.

Nói:

> Tìm kiếm lọc theo điểm đến, ngày, ngân sách, hình thức như đặc tả. Kết nối được chấp nhận chỉ cho phép trao đổi, chưa phải thỏa thuận đi cùng. Phần này giải quyết IS3.

### Bước 10 (30 giây, nếu được hỏi) — Trang Tổng quan

Web: chọn **Khách du lịch**, vào **Tổng quan**. Chuyến đang đi ghi "Chuyến đi đang diễn ra" và mở sẵn ngày hôm nay.

Mở `Controllers/DashboardController.cs` dòng 21: ưu tiên chuyến đang diễn ra có nhiều hoạt động hôm nay nhất, rồi đến chuyến sắp tới gần nhất. `Views/Dashboard/Index.cshtml` dòng 3 tính `openDay` và `tripPhase`.

---

## 5. Bảng tra cứu theo hàm

Dùng khi thầy hỏi "hàm này nằm ở đâu, làm gì". Chỉ liệt kê phần mới hoặc đã sửa sau A14.

| File | Dòng | Hàm | Làm gì |
|---|---|---|---|
| `Controllers/HomeController.cs` | 26 | `Place` | Trang địa điểm kèm dịch vụ tại chỗ và địa điểm cùng khu vực |
| `Controllers/MarketplaceController.cs` | 12 | `Index` | Tìm dịch vụ; dòng 24 lọc điểm đến, dòng 25–27 sắp xếp theo giá |
| `Services/DestinationFilter.cs` | 12 | `PlacesAt` | Địa điểm thuộc điểm đến (cùng khu vực) |
| `Services/DestinationFilter.cs` | 20 | `ServicesAt` | Dịch vụ thuộc điểm đến |
| `Services/DestinationFilter.cs` | 28 | `ServesPlace` | Dịch vụ thuộc một địa điểm |
| `Services/DestinationFilter.cs` | 32 | `Same` | So hai tên nơi |
| `Services/JourneyManager.cs` | 22 | `Detail` | Đọc kế hoạch; dòng 51–52 lọc gợi ý theo điểm đến |
| `Services/JourneyManager.cs` | 76 | `TripsForSource` | Kế hoạch cùng điểm đến với mục đang thêm |
| `Controllers/JourneyController.cs` | 51 | `ChooseSource` | Trang chọn kế hoạch khi thêm từ trang tra cứu |
| `Views/Journey/ActivityFields.cshtml` | 9 | `PlacesOf` | Gắn mã địa điểm cho từng dịch vụ ở ô 2 |
| `wwwroot/js/site.js` | 131 | `filterActivityOptions` | Ẩn mục không thuộc địa điểm đã chọn, tự điền địa chỉ |
| `wwwroot/js/site.js` | 154 | `fillActivityCost` | Tự điền chi phí theo giá dịch vụ |
| `Models/ServiceFormHints.cs` | 9 | `Unit` | Đơn vị giá theo loại dịch vụ |
| `Models/ServiceFormHints.cs` | 18 | `For` | Nhãn và ví dụ form theo loại dịch vụ |
| `ViewComponents/DestinationOptionsViewComponent.cs` | 12 | `InvokeAsync` | Danh sách gợi ý địa điểm dùng chung |
| `Controllers/AdminController.cs` | 14 | `Index` | Trang duyệt kèm danh sách địa điểm để so khớp |
| `Controllers/DashboardController.cs` | 21 | trong `Index` | Chọn chuyến đang diễn ra để hiện trên Tổng quan |
| `Data/DemoA14Seed.cs` | 15 | `KeepDemoTripOngoing` | Giữ chuyến demo luôn đang diễn ra |
| `Services/ThongDiep.cs` | — | `Loi`, `Http` | Danh mục 98 mã lỗi Bảng 4.250, thông điệp nguyên văn |
| `Services/ThongBaoLoai.cs` | — | `Them`, `Nhan` | Ghi thông báo đúng loại Bảng 4.249; hiển thị nhãn tiếng Việt |
| `Controllers/DesignLinksController.cs` | — | các route | Đường dẫn thiết kế trong thông báo chuyển sang trang thật |

---

## 6. Câu hỏi thầy có thể hỏi

**Một luồng hoàn chỉnh của nhóm là gì?**

> Từ khách tìm thông tin, lập kế hoạch, gửi yêu cầu, đến khi nhà cung cấp ghi kết quả và khách thấy dịch vụ đã hoàn thành. Luồng đi qua hai vai trò khách và nhà cung cấp, trạng thái yêu cầu đổi từ Chờ phản hồi, Đã xác nhận đến Hoàn thành.

**Lọc theo điểm đến dựa vào đâu, có dùng bản đồ không?**

> Không dùng bản đồ vì Chương 4 đã loại bản đồ ra khỏi phạm vi. Hệ thống dựa vào khu vực của địa điểm trong CSDL: tìm địa điểm trùng tên điểm đến để biết khu vực, rồi lấy các địa điểm cùng khu vực.

**Nếu gửi thẳng một dịch vụ ở nơi khác lên server thì sao?**

> Server vẫn nhận, vì SUC02.01 chỉ quy định chọn từ kết quả tìm kiếm hoặc tự nhập, không giới hạn theo điểm đến. Bộ lọc giúp khách chọn đúng, còn server kiểm tra mục đó còn được công bố.

**Sao form đã ẩn mà server vẫn kiểm tra?**

> Ẩn form chỉ để dễ dùng. Người dùng vẫn có thể gửi yêu cầu trực tiếp, nên quy tắc phải kiểm tra ở máy chủ.

**Nhà cung cấp gõ sai tên nơi phục vụ thì sao?**

> Ô nơi phục vụ gợi ý tên địa điểm có sẵn. Nếu vẫn gõ tên lạ, Admin thấy cảnh báo khi duyệt và có thể yêu cầu sửa, vì dịch vụ đó sẽ không hiện trong form lập kế hoạch.

**Điểm đến chưa có trong hệ thống thì sao?**

> Danh sách gợi ý để trống và form nhắc khách tự nhập hoạt động. Khách vẫn lập được kế hoạch.

**Có thêm bảng nào vào CSDL không?**

> Không. CSDL giữ đúng 25 bảng như Chương 4, phần hoàn thiện chỉ đọc và ghi trên các bảng đã có.

**Sửa nhiều vậy có làm hỏng chức năng cũ không?**

> Các quy tắc cũ giữ nguyên. Nhóm chạy lại toàn bộ 132 test, gồm 131 test nghiệp vụ và test hệ thống chạy 7 kịch bản trên CSDL riêng, tất cả đều đạt.

**Dữ liệu có thật không?**

> Tên cơ sở và giá là dữ liệu minh họa để demo, nạp ở `Data/DemoA14Seed.cs` và `Data/DemoFullDataSeed.cs`, chỉ chạy khi bật chế độ demo.

---

## 7. Giới hạn và việc còn tồn

Nói thẳng nếu được hỏi:

- Không có bản đồ, GPS, thanh toán, AI, đúng phạm vi ở Chương 4.
- Chi phí dự kiến do khách tự nhập; giá cuối do hai bên thống nhất.

Việc nên làm trước buổi bảo vệ:

- CSDL demo còn nhiều bản ghi thử nghiệm ("Demo A1", "Design nullable…", dịch vụ "Demo Đà Lạt"…). Khi demo, chọn đúng chuyến "Đà Lạt cuối tuần (đang đi)" và các dịch vụ thật.
- Trang chủ liệt kê cả 26 địa điểm, trộn thành phố với điểm tham quan nhỏ.
- Yêu cầu #9 có ngày dùng đã qua nhưng vẫn "Chờ phản hồi".
- Kiểm tra lại số dòng trong file này nếu sau đó có sửa code.
