# Hoàn thiện sản phẩm sau buổi báo cáo A14

## Nhận xét của hội đồng và hướng xử lý

Hội đồng đánh giá phần tài liệu ổn, nhưng sản phẩm mới là khung giao diện. Khi demo chưa đi được trọn một luồng từ lúc bắt đầu thao tác đến khi có kết quả.

Hướng xử lý: giữ nguyên các chức năng, giao diện và 25 bảng CSDL đã có, chỉ hoàn thiện những phần còn thiếu để mỗi mục tiêu ở Chương 1 có một luồng demo hoàn chỉnh. Phần bổ sung không đổi cấu trúc cũ (Controller, Service, Model, View) và dùng lại giao diện đang có.

## Đối chiếu 4 mục tiêu với chức năng

| Mục tiêu | Vấn đề | Đã có trước A14 | Bổ sung sau A14 |
|---|---|---|---|
| MT1: tiếp cận đủ thông tin | IS1 | Tra cứu điểm đến/dịch vụ, lọc theo khu vực, loại, giá | Trang địa điểm gom thêm dịch vụ tại nơi đó và các địa điểm cùng khu vực; thêm dữ liệu địa điểm, dịch vụ ăn uống và di chuyển |
| MT2: chọn phương án phù hợp | IS2 | Lọc theo giá, lập kế hoạch có kiểm tra trùng giờ và ngân sách | Lọc dịch vụ theo điểm đến, sắp xếp theo giá; lập kế hoạch chỉ gợi ý địa điểm, dịch vụ tại điểm đến; nhãn form và tạm tính theo loại dịch vụ |
| MT3: tìm người đồng hành tương đồng | IS3 | Tìm nhu cầu theo điểm đến, ngày, ngân sách, hình thức; kết nối và thỏa thuận | **Mức độ phù hợp (%)** kèm lý do; xếp nhu cầu phù hợp nhất lên đầu; tìm nhanh theo chuyến đi sắp tới của mình |
| MT4: giải quyết nhu cầu phát sinh | IS4 | Ghi tình huống, phương án, kết quả | **Trang "Hỗ trợ tại điểm đến"**: lịch hôm nay, chọn nhu cầu (ăn uống, di chuyển, lưu trú, trải nghiệm), gửi yêu cầu hoặc thêm vào lịch ngay, lối tắt ghi tình huống |

## Giải thích từng chức năng bổ sung

### 1. Mức độ phù hợp khi tìm đồng hành (MT3)

Khi khách nhập tiêu chí, mỗi nhu cầu được chấm điểm theo bảng sau:

| Tiêu chí | Điểm |
|---|---|
| Cùng điểm đến | 35 |
| Khoảng ngày giao nhau | 25 |
| Ngân sách trong khả năng | 15 |
| Cùng hình thức đi | 10 |
| Có sở thích chung (so với hồ sơ của mình) | 15 |

- **Cách quy ra phần trăm:** chỉ tính những tiêu chí khách đã nhập, rồi lấy điểm đạt chia tổng điểm có thể đạt. Ví dụ khách chỉ nhập điểm đến và ngày thì tổng là 60 điểm.
- **Thứ tự hiển thị:** nhu cầu phù hợp nhất lên đầu, kèm lý do như "Cùng điểm đến Đà Lạt", "Trùng 1 ngày đi".
- **Quyền riêng tư:** sở thích chỉ được so khi người đăng cho phép xem hồ sơ.
- **Tìm nhanh:** nút "Tìm theo chuyến đi của bạn" tự điền điểm đến, ngày và ngân sách từ kế hoạch sắp tới.
- **File:** `Services/CompanionSuggestion.cs`, `Controllers/CommunityController.cs` (action `Index`), `Views/Community/_DemandCard.cshtml`.

### 2. Hỗ trợ tại điểm đến (MT4)

- **Đường dẫn:** `/Journey/OnTrip`, mục "Hỗ trợ tại điểm đến" ở menu trái.
- **Chọn chuyến đi:** hệ thống tự chọn chuyến đang diễn ra hôm nay. Không có thì lấy chuyến gần nhất.
- **Nội dung trang:**
  - Lịch hoạt động hôm nay.
  - Các nút nhu cầu: Ăn uống / Di chuyển / Lưu trú / Trải nghiệm. Hệ thống lọc dịch vụ đã duyệt tại đúng điểm đến của chuyến đi, ưu tiên dịch vụ còn tiếp nhận, rồi giá thấp.
  - Mỗi dịch vụ có nút "Gửi yêu cầu" (ngày dùng là hôm nay) và nút "Thêm vào lịch trình".
  - Khung "Có vấn đề phát sinh?" dẫn sang ghi nhận tình huống của đúng chuyến đi.
- **File:** `Services/OnTripManager.cs`, `Controllers/JourneyController.cs` (action `OnTrip`), `Views/Journey/OnTrip.cshtml`.

### 3. Trang địa điểm đầy đủ hơn (MT1)

- **Bổ sung:** trang chi tiết địa điểm hiện các dịch vụ đã duyệt tại nơi đó và các địa điểm cùng khu vực.
- **Vì sao đáp ứng MT1:** khách không phải tìm lại ở trang khác.
- **File:** `Controllers/HomeController.cs` (action `Place`), `Views/Home/Place.cshtml`.

### 4. Lập kế hoạch lọc theo điểm đến

- **Góp ý:** kế hoạch đi Đà Lạt nhưng danh sách thêm hoạt động vẫn hiện địa điểm, dịch vụ ở nơi khác.
- **Cách lọc:** tìm địa điểm trùng tên điểm đến để biết khu vực (Đà Lạt thuộc Lâm Đồng). Sau đó chỉ gợi ý địa điểm cùng khu vực và dịch vụ phục vụ tại những nơi đó. Form chia 2 ô: chọn địa điểm trước, ô thứ hai chỉ hiện hoạt động và dịch vụ của địa điểm đó (hàm `DestinationFilter.ServesPlace`, lọc trên trình duyệt bằng `filterActivityOptions` trong `wwwroot/js/site.js`). Chọn địa điểm thì tự điền địa chỉ, chọn dịch vụ thì tự điền chi phí.
- **Giữ dữ liệu cũ:** hoạt động đã chọn nơi khác trước đây vẫn còn trong danh sách để khi sửa không bị mất nguồn.
- **Thêm từ trang tra cứu:** bấm "Thêm vào lịch trình" ở một địa điểm hoặc dịch vụ thì chỉ hiện kế hoạch cùng điểm đến.
- **File:** `Services/DestinationFilter.cs`, `Services/JourneyManager.cs` (`Detail`, `TripsForSource`), `Controllers/JourneyController.cs` (`ChooseSource`), `Views/Journey/ActivityFields.cshtml`, `Views/Journey/ChooseSource.cshtml`.

### 5. Form gửi yêu cầu theo loại dịch vụ

- **Góp ý:** dịch vụ đặt xe hay ăn uống vẫn ví dụ "nhận phòng 14:00".
- **Sửa:** nhãn và ví dụ đổi theo loại. Lưu trú hỏi ngày và giờ nhận phòng, ăn uống hỏi giờ đến quán và số suất, di chuyển hỏi giờ và điểm đón, trải nghiệm hỏi giờ bắt đầu và số vé.
- **File:** `Models/ServiceFormHints.cs`, `Views/Marketplace/_Terms.cshtml`, `Views/Marketplace/Requests.cshtml`.

### 6. Rà soát sau khi lọc theo điểm đến

| Chỗ sửa | Lý do | File |
|---|---|---|
| Ô "Điểm đến" (kế hoạch, nhu cầu đồng hành) và "Nơi phục vụ" (nhà cung cấp) gợi ý từ cùng danh sách địa điểm | Hai phía dùng cùng tên thì bộ lọc mới khớp | `ViewComponents/DestinationOptionsViewComponent.cs` |
| Admin duyệt dịch vụ thấy nơi phục vụ có khớp địa điểm nào không | Bắt lỗi gõ tên lạ trước khi công bố (SUC06.02) | `AdminController.Index`, `Admin/Index.cshtml` |
| Tìm dịch vụ có ô chọn Điểm đến | Lọc dịch vụ theo nơi đến như phía kế hoạch | `MarketplaceController.Index` |
| Giá ghi đơn vị theo loại: phòng/đêm, suất, lượt, vé | "/ đơn vị" không rõ khách trả cho cái gì | `ServiceFormHints.Unit` |
| Tạm tính lưu trú nhân số đêm | Trước đây đặt 3 đêm vẫn tính như 1 đêm | `MarketplaceManager.EstimateTotal`, `site.js` |
| Tổng quan ghi đúng trạng thái chuyến và mở ngày hôm nay | Chuyến đang đi bị ghi "đang chuẩn bị" | `DashboardController`, `Dashboard/Index.cshtml` |

## Dữ liệu mẫu bổ sung

`Data/DemoA14Seed.cs` chỉ chạy một lần, chỉ thêm mới, không sửa hay xóa dữ liệu cũ. Nội dung thêm vào:

- 3 địa điểm.
- 4 dịch vụ ăn uống và di chuyển ở Đà Lạt.
- Sở thích trong hồ sơ hai khách mẫu.
- 4 lượt dùng dịch vụ đã hoàn thành (lịch sử yêu cầu), trong đó 1 lượt của `khach@demo.vn`.
- Chuyến đi "Đà Lạt cuối tuần (đang đi)" diễn ra quanh ngày hiện tại.

### Dữ liệu đầy đủ cho các điểm đến

`Data/DemoFullDataSeed.cs` cũng chỉ thêm mới. Nội dung:

| Nhóm dữ liệu | Chi tiết |
|---|---|
| Điểm đến | Ninh Bình, Hội An, Hạ Long, Sa Pa, Phú Quốc, Huế. Mỗi nơi có trang chính và 2–3 địa điểm tham quan, ví dụ Tam Cốc, Hang Múa, Bản Cát Cát, Fansipan, Đại Nội |
| Nhà cung cấp | Mỗi điểm đến có một nhà cung cấp riêng đã xác minh (`ninhbinh@demo.vn`, `hoian@demo.vn`, `halong@demo.vn`, `sapa@demo.vn`, `phuquoc@demo.vn`, `hue@demo.vn`), mật khẩu `Demo12345` |
| Dịch vụ | Mỗi điểm đến đủ 4 loại: Lưu trú, Ăn uống, Di chuyển, Trải nghiệm. Hơn 30 dịch vụ đã duyệt |
| Lịch sử | 12 lượt dùng dịch vụ đã hoàn thành |
| Khách thêm | `lan@demo.vn`, `tuan@demo.vn`, có nhu cầu đồng hành đi Hội An, Ninh Bình, Sa Pa, Phú Quốc |

Trang địa điểm hiện dịch vụ của chính nơi đó trước, sau đó đến dịch vụ của các nơi cùng khu vực. Ví dụ trang Bản Cát Cát vẫn hiện dịch vụ ở Sa Pa. Tên cơ sở và giá đều là dữ liệu minh họa.

## Kịch bản demo rút gọn (chi tiết ở file 07 trong `docs/Huong dan demo`)

1. **MT1:** chưa đăng nhập, vào Tra cứu, tìm "Đà Lạt", mở địa điểm Đà Lạt. Kéo xuống thấy dịch vụ tại chỗ và địa điểm cùng khu vực.
2. **MT2:** vào Tìm dịch vụ, chọn Điểm đến "Đà Lạt", sắp xếp theo giá. Đăng nhập Khách du lịch, mở kế hoạch "Đà Lạt cuối tuần (đang đi)", thêm hoạt động bằng 2 ô: chọn địa điểm rồi chọn hoạt động.
3. **MT3:** vào Đồng hành, nhập điểm đến "Hội An", từ 22/11/2026 đến 24/11/2026. Thấy nhu cầu của Thu Lan "Phù hợp 100%" kèm lý do, rồi gửi đề nghị kết nối như luồng cũ. Xem kịch bản chi tiết ở `docs/Huong dan demo/07 Kich ban demo tron luong va phan sua sau A14.md`.
4. **MT4:** vào "Hỗ trợ tại điểm đến": lịch hôm nay của chuyến Đà Lạt đang đi. Bấm "Ăn uống", chọn "Lẩu gà lá é", bấm "Thêm vào lịch trình" hoặc "Gửi yêu cầu". Thử "Ghi nhận tình huống" khi trời mưa.

## Phần chưa làm (giữ đúng phạm vi đã báo cáo)

- Không tích hợp bản đồ, định vị GPS, thanh toán hay AI. Chương 4 xác định các phần này nằm ngoài phạm vi. "Gần đây" được hiểu là cùng điểm đến của chuyến đi.
- Mức độ phù hợp là quy tắc chấm điểm đơn giản, giải thích được, không phải thuật toán học máy.
