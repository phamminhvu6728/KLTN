using Microsoft.EntityFrameworkCore;
using TuTuc.Models;

namespace TuTuc.Data;

// Dữ liệu mẫu bổ sung sau buổi A14 để demo được trọn luồng của 4 mục tiêu.
// Chỉ chạy một lần (đánh dấu bằng DemoSeedMarker) và chỉ khi bật chế độ demo.
// Không xóa hay sửa dữ liệu cũ, chỉ thêm mới.
public static class DemoA14Seed
{
    const string Marker = "a14-bo-sung-v1";

    // Giữ chuyến đi mẫu luôn "đang diễn ra" mỗi lần khởi động: dời cả chuyến và hoạt động
    // sao cho ngày bắt đầu là hôm qua, nhờ đó demo trang Hỗ trợ tại điểm đến vào ngày nào cũng được.
    static void KeepDemoTripOngoing(AppDbContext db)
    {
        var trip = db.Trips.Include(x => x.Activities).FirstOrDefault(x => x.Name == "Đà Lạt cuối tuần (đang đi)");
        if (trip == null) return;
        var shift = DateTime.UtcNow.AddHours(7).Date.AddDays(-1) - trip.Start.Date;
        if (shift == TimeSpan.Zero) return;
        trip.Start += shift;
        trip.End += shift;
        foreach (var a in trip.Activities) { a.Start += shift; a.End += shift; }
        db.SaveChanges();
    }

    public static void Apply(AppDbContext db, bool demoEnabled)
    {
        if (!demoEnabled) return;
        KeepDemoTripOngoing(db);
        if (db.DemoSeedMarkers.Any(x => x.Key == Marker)) return;

        var traveler = db.Accounts.SingleOrDefault(x => x.Email == "khach@demo.vn");
        var friend = db.Accounts.SingleOrDefault(x => x.Email == "anh@demo.vn");
        var provider = db.Accounts.SingleOrDefault(x => x.Email == "ncc@demo.vn");
        if (traveler == null || friend == null || provider == null) return;
        var today = DateTime.UtcNow.AddHours(7).Date;

        // Mục tiêu 1: thêm địa điểm để tra cứu có nội dung.
        void Place(string name, string region, string category, string description, string image, decimal price)
        {
            if (!db.Places.Any(x => x.Name == name))
                db.Places.Add(new Place { Name = name, Region = region, Category = category, Description = description, Image = image, Price = price });
        }
        Place("Đồi chè Cầu Đất", "Lâm Đồng", "Thiên nhiên", "Đồi chè trên cao, điểm săn mây buổi sáng sớm cách trung tâm Đà Lạt khoảng 25 km.", "/images/dalat.jpg", 300000);
        Place("Chợ đêm Đà Lạt", "Lâm Đồng", "Ẩm thực", "Khu ăn uống buổi tối gần hồ Xuân Hương: bánh tráng nướng, sữa đậu nành, đồ len.", "/images/dalat.jpg", 200000);
        Place("Tràng An", "Miền Bắc", "Thiên nhiên", "Đi thuyền qua hang động và núi đá vôi, nên đi buổi sáng để tránh nắng.", "/images/ninhbinh.jpg", 500000);

        // Mục tiêu 4: dịch vụ ăn uống và di chuyển để khách tìm khi có nhu cầu tại điểm đến.
        TravelService Service(string name, string category, decimal price, string hours, string description)
        {
            var s = db.Services.FirstOrDefault(x => x.Name == name);
            if (s != null) return s;
            s = new TravelService { OwnerId = provider.Id, Name = name, Category = category, Location = "Đà Lạt", Price = price, ServingHours = hours, Description = description, Image = "/images/dalat.jpg", ApprovalStatus = "Đã duyệt", PublishedRevisionNumber = 1 };
            db.Services.Add(s);
            return s;
        }
        Service("Lẩu gà lá é bên đồi thông", "Ăn uống", 180000, "10:00 – 22:00", "Suất lẩu cho 2 người, đặt trước để giữ bàn vào buổi tối.");
        Service("Bữa sáng bánh căn và sữa đậu nành", "Ăn uống", 60000, "6:00 – 10:00", "Bánh căn nóng, nước chấm xíu mại, sữa đậu nành. Phù hợp đi sớm trước khi tham quan.");
        Service("Thuê xe máy theo ngày", "Di chuyển", 120000, "7:00 – 20:00", "Xe số hoặc xe ga, kèm 2 mũ bảo hiểm. Giao xe tận nơi trong trung tâm.");
        Service("Xe đưa đón sân bay Liên Khương", "Di chuyển", 250000, "Theo giờ chuyến bay", "Xe 4–7 chỗ, đón tại sân bay về trung tâm Đà Lạt.");
        db.SaveChanges();

        // Mục tiêu 3: sở thích trong hồ sơ để so mức độ phù hợp khi tìm đồng hành.
        foreach (var (id, interests) in new[] { (traveler.Id, "Thiên nhiên, Nhiếp ảnh, Cà phê"), (friend.Id, "Nhiếp ảnh, Thiên nhiên, Ẩm thực") })
        {
            var profile = db.UserProfiles.SingleOrDefault(x => x.OwnerId == id);
            if (profile == null) { profile = new UserProfile { OwnerId = id }; db.UserProfiles.Add(profile); }
            if (string.IsNullOrWhiteSpace(profile.Interests)) profile.Interests = interests;
            if (id == friend.Id && profile.SharingScope == "Private") profile.SharingScope = "Registered";
        }

        // Các lượt dùng dịch vụ đã hoàn thành (lịch sử yêu cầu).
        var tour = db.Services.First(x => x.OwnerId == provider.Id && x.Name.StartsWith("Khám phá Đà Lạt"));
        var homestay = db.Services.First(x => x.OwnerId == provider.Id && x.Name.StartsWith("Homestay"));
        var hotpot = db.Services.First(x => x.Name == "Lẩu gà lá é bên đồi thông");
        Booking Done(Account owner, TravelService s, int daysAgo)
        {
            var b = new Booking { OwnerId = owner.Id, TravelServiceId = s.Id, OperationId = Guid.NewGuid(), SnapshotName = s.Name, Date = today.AddDays(-daysAgo), People = 2, Total = s.Price * 2, Status = "Hoàn thành", Result = "Kết quả theo hạng mục: Hoàn thành", Note = "Dữ liệu mẫu: dịch vụ đã sử dụng.", CustomerAcceptedVersion = 1, ProviderAcceptedVersion = 1 };
            db.Bookings.Add(b);
            db.SaveChanges();
            db.BookingItemResults.Add(new BookingItemResult { BookingId = b.Id, ItemKey = "main", ItemTitle = s.Name, Outcome = "Hoàn thành", Note = "Đã phục vụ đầy đủ.", VerificationStatus = "Đã ghi nhận", RecordedAt = DateTime.UtcNow.AddDays(-daysAgo) });
            return b;
        }
        Done(friend, tour, 12);
        Done(friend, homestay, 20);
        Done(friend, hotpot, 8);
        // Lượt dùng của khach@demo.vn đã hoàn thành.
        Done(traveler, homestay, 5);

        // Mục tiêu 4: một chuyến đi đang diễn ra hôm nay để demo trang Hỗ trợ tại điểm đến.
        db.Trips.Add(new Trip
        {
            OwnerId = traveler.Id, Name = "Đà Lạt cuối tuần (đang đi)", Destination = "Đà Lạt", Start = today.AddDays(-1), End = today.AddDays(1), Budget = 2500000, Status = "Đã chấp nhận",
            Activities =
            [
                new TripActivity { Title = "Săn mây đồi chè Cầu Đất", Location = "Cầu Đất", Start = today.AddHours(5), End = today.AddHours(8), Cost = 200000 },
                new TripActivity { Title = "Cà phê ven hồ Xuân Hương", Location = "Trung tâm Đà Lạt", Start = today.AddHours(9), End = today.AddHours(10), Cost = 80000 },
                new TripActivity { Title = "Ăn tối chợ đêm", Location = "Chợ đêm Đà Lạt", Start = today.AddHours(18), End = today.AddHours(20), Cost = 150000 },
            ],
        });

        db.DemoSeedMarkers.Add(new DemoSeedMarker { Key = Marker });
        db.SaveChanges();
    }
}
