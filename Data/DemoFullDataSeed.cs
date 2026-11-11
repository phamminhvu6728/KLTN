using Microsoft.AspNetCore.Identity;
using TuTuc.Models;

namespace TuTuc.Data;

// Dữ liệu mẫu đầy đủ cho các điểm đến: địa điểm tham quan, nhà cung cấp đã xác minh,
// dịch vụ đủ 4 loại (Lưu trú, Ăn uống, Di chuyển, Trải nghiệm) và nhu cầu đồng hành.
// Chỉ chạy một lần khi bật demo, chỉ thêm mới, không sửa/xóa dữ liệu cũ.
// Tên cơ sở và giá là dữ liệu minh họa.
public static class DemoFullDataSeed
{
    const string Marker = "du-lieu-day-du-v1";

    record ServiceData(string Name, string Category, decimal Price, string Hours, string Description);
    record Destination(string Location, string Region, string Image, string ProviderEmail, string ProviderName, string Phone,
        (string Name, string Category, string Description, decimal Price)[] Places, ServiceData[] Services);

    static readonly Destination[] Destinations =
    [
        new("Ninh Bình", "Miền Bắc", "/images/ninhbinh.jpg", "ninhbinh@demo.vn", "Ninh Bình Xanh", "0900000101",
            [("Tam Cốc – Bích Động", "Thiên nhiên", "Đi thuyền dọc sông Ngô Đồng giữa núi đá vôi, đẹp nhất mùa lúa chín tháng 5–6.", 400000),
             ("Hang Múa", "Thiên nhiên", "Leo khoảng 500 bậc đá để ngắm toàn cảnh Tam Cốc, nên đi lúc hoàng hôn.", 150000),
             ("Chùa Bái Đính", "Văn hóa", "Quần thể chùa lớn với hành lang La Hán, nên dành nửa ngày tham quan.", 200000)],
            [new("Homestay ven núi Tam Cốc", "Lưu trú", 550000, "Nhận phòng 14:00, trả phòng 12:00", "Phòng đôi nhìn ra cánh đồng, có xe đạp miễn phí cho khách."),
             new("Cơm dê núi và cơm cháy", "Ăn uống", 250000, "10:00 – 21:00", "Suất đặc sản Ninh Bình cho 2 người, có món chay theo yêu cầu."),
             new("Thuê xe đạp và xe máy Tam Cốc", "Di chuyển", 100000, "6:00 – 19:00", "Xe đạp và xe máy theo ngày, giao xe tận homestay trong khu Tam Cốc."),
             new("Thuyền Tràng An tuyến 1", "Trải nghiệm", 250000, "7:00 – 16:00", "Vé thuyền qua 9 hang động, khoảng 3 giờ. Đặt trước để có lái đò hướng dẫn.")]),

        new("Hội An", "Quảng Nam", "/images/vietnam.jpg", "hoian@demo.vn", "Hội An Phố Cổ Travel", "0900000102",
            [("Phố cổ Hội An", "Văn hóa", "Nhà cổ, hội quán và chùa Cầu. Buổi tối thả đèn hoa đăng trên sông Hoài.", 300000),
             ("Rừng dừa Bảy Mẫu", "Thiên nhiên", "Đi thúng chai trong rừng dừa nước, cách phố cổ khoảng 5 km.", 200000),
             ("Biển An Bàng", "Biển đảo", "Bãi biển yên tĩnh, nhiều quán ven biển, phù hợp tắm biển buổi chiều.", 100000)],
            [new("Nhà vườn gần phố cổ", "Lưu trú", 700000, "Nhận phòng 14:00, trả phòng 12:00", "Phòng đôi trong nhà vườn, đi bộ 10 phút ra phố cổ, có bữa sáng."),
             new("Cao lầu và mì Quảng", "Ăn uống", 120000, "7:00 – 21:00", "Suất cao lầu, mì Quảng và nước chanh muối cho 2 người."),
             new("Xe đưa đón Đà Nẵng – Hội An", "Di chuyển", 300000, "Theo giờ khách hẹn", "Xe 4–7 chỗ đón tại sân bay hoặc khách sạn ở Đà Nẵng."),
             new("Lớp nấu ăn và đi chợ quê", "Trải nghiệm", 450000, "8:00 – 12:00", "Đi chợ cùng người địa phương, học nấu 3 món, ăn trưa tại chỗ.")]),

        new("Hạ Long", "Quảng Ninh", "/images/vietnam.jpg", "halong@demo.vn", "Hạ Long Bay Tour", "0900000103",
            [("Đảo Ti Tốp", "Biển đảo", "Leo lên đỉnh đảo ngắm vịnh, có bãi tắm nhỏ dưới chân đảo.", 150000),
             ("Hang Sửng Sốt", "Thiên nhiên", "Hang động lớn nhất vịnh, thường nằm trong tuyến thuyền 1 ngày.", 250000)],
            [new("Khách sạn view vịnh Bãi Cháy", "Lưu trú", 900000, "Nhận phòng 14:00, trả phòng 12:00", "Phòng đôi nhìn ra vịnh, gần cầu Bãi Cháy và chợ đêm."),
             new("Hải sản chả mực Hạ Long", "Ăn uống", 350000, "10:00 – 22:00", "Chả mực giã tay, hàu nướng, cá song hấp cho 2 người."),
             new("Xe limousine Hà Nội – Hạ Long", "Di chuyển", 280000, "Mỗi giờ từ 6:00 – 18:00", "Xe 9 chỗ đón tận nơi trong phố cổ Hà Nội, khoảng 2 giờ 30 phút."),
             new("Du thuyền ngày ngắm vịnh", "Trải nghiệm", 850000, "8:00 – 16:00", "Tham quan hang Sửng Sốt, đảo Ti Tốp, chèo kayak, kèm bữa trưa trên thuyền.")]),

        new("Sa Pa", "Lào Cai", "/images/vietnam.jpg", "sapa@demo.vn", "Sa Pa Trekking", "0900000104",
            [("Bản Cát Cát", "Văn hóa", "Bản người H'Mông gần trung tâm, có thác nước và nghề dệt thổ cẩm.", 150000),
             ("Đỉnh Fansipan", "Thiên nhiên", "Đi cáp treo lên nóc nhà Đông Dương, mang áo ấm vì nhiệt độ thấp.", 800000),
             ("Thung lũng Mường Hoa", "Thiên nhiên", "Ruộng bậc thang và bãi đá cổ, phù hợp đi bộ nửa ngày.", 100000)],
            [new("Homestay bản Tả Van", "Lưu trú", 450000, "Nhận phòng 13:00, trả phòng 11:00", "Phòng trên gác nhà người Giáy, có bữa tối cùng gia đình."),
             new("Lẩu cá hồi và thắng cố", "Ăn uống", 300000, "10:00 – 22:00", "Lẩu cá hồi Sa Pa cho 2 người, có rau rừng theo mùa."),
             new("Xe giường nằm Hà Nội – Sa Pa", "Di chuyển", 350000, "Khởi hành 7:00 và 22:00", "Xe giường nằm khoảng 5 giờ 30 phút, trả khách tại trung tâm Sa Pa."),
             new("Đi bộ Lao Chải – Tả Van cùng người bản địa", "Trải nghiệm", 400000, "8:30 – 15:00", "Hướng dẫn viên người H'Mông, khoảng 12 km, kèm bữa trưa.")]),

        new("Phú Quốc", "Kiên Giang", "/images/vietnam.jpg", "phuquoc@demo.vn", "Đảo Ngọc Phú Quốc", "0900000105",
            [("Bãi Sao", "Biển đảo", "Bãi cát trắng, nước trong, ít sóng, phù hợp gia đình.", 100000),
             ("Chợ đêm Phú Quốc", "Ẩm thực", "Hải sản tươi, gỏi cá trích, kem cuộn ở trung tâm Dương Đông.", 300000)],
            [new("Bungalow sát biển Ông Lang", "Lưu trú", 1100000, "Nhận phòng 14:00, trả phòng 12:00", "Bungalow riêng sát biển, có hồ bơi chung và bữa sáng."),
             new("Gỏi cá trích và bún quậy", "Ăn uống", 150000, "7:00 – 21:00", "Đặc sản Phú Quốc cho 2 người, gần chợ Dương Đông."),
             new("Thuê xe máy giao tận nơi", "Di chuyển", 150000, "7:00 – 21:00", "Xe ga đời mới, kèm mũ và áo mưa, giao xe tại khách sạn."),
             new("Tour 4 đảo và lặn ngắm san hô", "Trải nghiệm", 750000, "8:00 – 16:30", "Cano đi 4 đảo phía nam, dụng cụ lặn, bữa trưa hải sản.")]),

        new("Huế", "Thừa Thiên Huế", "/images/vietnam.jpg", "hue@demo.vn", "Cố Đô Huế Travel", "0900000106",
            [("Đại Nội Huế", "Văn hóa", "Kinh thành triều Nguyễn, nên dành nửa ngày và đi vào buổi sáng.", 200000),
             ("Lăng Khải Định", "Văn hóa", "Lăng kết hợp kiến trúc Á – Âu, nội thất khảm sành sứ.", 150000),
             ("Chùa Thiên Mụ", "Văn hóa", "Ngôi chùa cổ bên bờ sông Hương, có thể đi thuyền rồng tới.", 0)],
            [new("Nhà rường bên sông Hương", "Lưu trú", 650000, "Nhận phòng 14:00, trả phòng 12:00", "Phòng trong nhà rường cổ, sân vườn yên tĩnh, có bữa sáng."),
             new("Bún bò và bánh bèo Huế", "Ăn uống", 100000, "6:00 – 21:00", "Suất bún bò, bánh bèo, nậm, lọc cho 2 người."),
             new("Xích lô dạo phố Huế", "Di chuyển", 150000, "8:00 – 21:00", "Xích lô 1 giờ quanh kinh thành và cầu Trường Tiền."),
             new("Thuyền rồng nghe ca Huế", "Trải nghiệm", 200000, "19:00 – 21:00", "Thuyền trên sông Hương, nghe ca Huế khoảng 1 giờ.")]),
    ];

    // Những dịch vụ đã có khách dùng xong (để có lịch sử yêu cầu "Hoàn thành").
    static readonly string[] CompletedServices =
    [
        "Thuyền Tràng An tuyến 1",
        "Homestay ven núi Tam Cốc",
        "Cơm dê núi và cơm cháy",
        "Lớp nấu ăn và đi chợ quê",
        "Nhà vườn gần phố cổ",
        "Du thuyền ngày ngắm vịnh",
        "Xe limousine Hà Nội – Hạ Long",
        "Đi bộ Lao Chải – Tả Van cùng người bản địa",
        "Homestay bản Tả Van",
        "Tour 4 đảo và lặn ngắm san hô",
        "Bún bò và bánh bèo Huế",
        "Thuyền rồng nghe ca Huế",
    ];

    // Trang chính của từng điểm đến (thêm nếu chưa có, chạy mỗi lần khởi động nhưng không tạo trùng).
    static readonly (string Name, string Region, string Category, string Description, decimal Price)[] MainPlaces =
    [
        ("Sa Pa", "Lào Cai", "Núi rừng", "Thị trấn vùng cao mát mẻ quanh năm: ruộng bậc thang, bản làng H'Mông, Dao và đỉnh Fansipan.", 2500000),
        ("Huế", "Thừa Thiên Huế", "Văn hóa", "Cố đô với kinh thành, lăng tẩm triều Nguyễn, sông Hương và ẩm thực cung đình.", 2000000),
        ("Phú Quốc", "Kiên Giang", "Biển đảo", "Đảo ngọc với bãi cát trắng, lặn ngắm san hô và hải sản tươi ở chợ đêm.", 4500000),
    ];

    public static void Apply(AppDbContext db, bool demoEnabled)
    {
        if (!demoEnabled) return;
        foreach (var p in MainPlaces)
            if (!db.Places.Any(x => x.Name == p.Name))
                db.Places.Add(new Place { Name = p.Name, Region = p.Region, Category = p.Category, Description = p.Description, Image = "/images/vietnam.jpg", Price = p.Price });
        db.SaveChanges();
        if (db.DemoSeedMarkers.Any(x => x.Key == Marker)) return;
        var hasher = new PasswordHasher<Account>();
        var today = DateTime.UtcNow.AddHours(7).Date;

        Account Ensure(string email, string name, string role, string phone, string bio)
        {
            var a = db.Accounts.SingleOrDefault(x => x.Email == email);
            if (a != null) return a;
            a = new Account { Email = email, Name = name, Role = role, Phone = phone, Bio = bio };
            if (role == "Provider")
            {
                a.ProviderVerified = true;
                a.VerificationStatus = "Đã xác minh";
                a.ProviderStatus = "Đang hoạt động";
            }
            a.PasswordHash = hasher.HashPassword(a, "Demo12345");
            db.Accounts.Add(a);
            db.SaveChanges();
            return a;
        }

        // Thêm vài khách du lịch để có lịch sử dùng dịch vụ và nhu cầu đồng hành đa dạng.
        var lan = Ensure("lan@demo.vn", "Thu Lan", "Traveler", "0900000201", "Thích đi chậm, chụp ảnh và ăn món địa phương.");
        var tuan = Ensure("tuan@demo.vn", "Minh Tuấn", "Traveler", "0900000202", "Mê trekking và biển, thường đi cuối tuần.");
        var friend = db.Accounts.SingleOrDefault(x => x.Email == "anh@demo.vn");
        var reviewers = new[] { lan, tuan, friend }.Where(x => x != null).Cast<Account>().ToArray();
        foreach (var (who, interests) in new[] { (lan, "Nhiếp ảnh, Ẩm thực, Văn hóa"), (tuan, "Trekking, Biển, Thiên nhiên") })
            if (!db.UserProfiles.Any(x => x.OwnerId == who.Id))
                db.UserProfiles.Add(new UserProfile { OwnerId = who.Id, Interests = interests, Residence = "Hà Nội", SharingScope = "Registered" });

        foreach (var d in Destinations)
        {
            // Địa điểm tham quan.
            foreach (var p in d.Places)
                if (!db.Places.Any(x => x.Name == p.Name))
                    db.Places.Add(new Place { Name = p.Name, Region = d.Region, Category = p.Category, Description = p.Description, Image = d.Image, Price = p.Price });

            // Nhà cung cấp đã xác minh và hồ sơ cơ sở.
            var provider = Ensure(d.ProviderEmail, d.ProviderName, "Provider", d.Phone, $"Đơn vị cung cấp dịch vụ du lịch tại {d.Location}.");
            if (!db.ProviderProfiles.Any(x => x.OwnerId == provider.Id))
                db.ProviderProfiles.Add(new ProviderProfile { OwnerId = provider.Id, CompanyName = d.ProviderName, CompanyType = "Hộ kinh doanh du lịch", ContactName = d.ProviderName, Address = d.Location, ContactPhone = d.Phone, ContactEmail = d.ProviderEmail });

            // Dịch vụ đã duyệt, đủ 4 loại.
            foreach (var s in d.Services)
                if (!db.Services.Any(x => x.Name == s.Name))
                    db.Services.Add(new TravelService
                    {
                        OwnerId = provider.Id, Name = s.Name, Category = s.Category, Location = d.Location, Price = s.Price,
                        ServingHours = s.Hours, Description = s.Description, Image = d.Image, ApprovalStatus = "Đã duyệt", PublishedRevisionNumber = 1,
                    });
        }
        db.SaveChanges();

        // Lượt dùng dịch vụ đã hoàn thành.
        var i = 0;
        foreach (var name in CompletedServices)
        {
            var service = db.Services.SingleOrDefault(x => x.Name == name);
            if (service == null || reviewers.Length == 0) continue;
            var author = reviewers[i++ % reviewers.Length];
            var booking = new Booking
            {
                OwnerId = author.Id, TravelServiceId = service.Id, OperationId = Guid.NewGuid(), SnapshotName = service.Name,
                Date = today.AddDays(-(10 + i * 3)), People = 2, Total = service.Price * 2, Status = "Hoàn thành",
                Result = "Kết quả theo hạng mục: Hoàn thành", Note = "Dữ liệu mẫu: dịch vụ đã sử dụng.", CustomerAcceptedVersion = 1, ProviderAcceptedVersion = 1,
            };
            db.Bookings.Add(booking);
            db.SaveChanges();
            db.BookingItemResults.Add(new BookingItemResult { BookingId = booking.Id, ItemKey = "main", ItemTitle = service.Name, Outcome = "Hoàn thành", Note = "Đã phục vụ đầy đủ.", VerificationStatus = "Đã ghi nhận", RecordedAt = DateTime.UtcNow.AddDays(-(10 + i * 3)) });
        }

        // Nhu cầu tìm người đồng hành ở nhiều điểm đến.
        void Demand(Account owner, string destination, int fromDay, int days, decimal budget, string style, string description, string wishes) =>
            db.Companions.Add(new Companion
            {
                OwnerId = owner.Id, Name = owner.Name, Destination = destination, Date = today.AddDays(fromDay), EndDate = today.AddDays(fromDay + days - 1),
                Budget = budget, People = 2, TravelStyle = style, Description = description, Wishes = wishes, SharingScope = "Public",
            });
        Demand(lan, "Hội An", 12, 3, 3000000, "Khám phá văn hóa", "Mình đi Hội An 3 ngày, muốn tìm 1 bạn cùng dạo phố cổ, học nấu ăn và chia tiền xe từ Đà Nẵng.", "Thích chụp ảnh, không ngại dậy sớm.");
        Demand(tuan, "Sa Pa", 20, 3, 2500000, "Phượt", "Trekking Lao Chải – Tả Van, ngủ homestay. Cần thêm 1–2 bạn để chia tiền hướng dẫn viên.", "Có sức khỏe đi bộ 12 km/ngày.");
        Demand(tuan, "Phú Quốc", 35, 4, 5000000, "Nghỉ dưỡng", "Đi Phú Quốc nghỉ dưỡng, lặn ngắm san hô và ăn hải sản. Tìm bạn chia phòng bungalow.", "Ưu tiên người biết bơi.");
        Demand(lan, "Ninh Bình", 9, 2, 2000000, "Thiên nhiên", "Đi Tràng An, Hang Múa 2 ngày cuối tuần, đi xe đạp quanh Tam Cốc.", "Thích đi chậm, ngắm cảnh.");

        db.DemoSeedMarkers.Add(new DemoSeedMarker { Key = Marker });
        db.SaveChanges();
    }
}
