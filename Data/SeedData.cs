using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Data;
public static class SeedData
{
    public static void Initialize(AppDbContext db, bool demoEnabled)
    {
        if (!demoEnabled) return;
        var hasher = new PasswordHasher<Account>();
        Account User(string email, string name, string role)
        {
            var account = db.Accounts.SingleOrDefault(x => x.Email == email);
            if (account != null) return account;
            account = new Account { Email = email, Name = name, Role = role, ProviderVerified = role == "Provider" };
            account.PasswordHash = hasher.HashPassword(account, "Demo12345");
            db.Accounts.Add(account); db.SaveChanges(); return account;
        }
        var traveler = User("khach@demo.vn", "Minh Anh", "Traveler");
        var provider = User("ncc@demo.vn", "Đà Lạt Local", "Provider");
        var friend = User("anh@demo.vn", "Hoàng Nam", "Traveler");
        User("admin@demo.vn", "Quản trị viên", "Admin");
        if (db.DemoSeedMarkers.Any(x => x.Key == "complete-demo-v2")) return;
        provider.ProviderVerified = true;
        if (!db.Places.Any())
            db.Places.AddRange(new Place { Name="Đà Lạt",Region="Lâm Đồng",Category="Núi rừng",Description="Cao nguyên mờ sương, đồi thông và những quán cà phê yên bình. Gợi ý: hồ Xuân Hương, đồi chè Cầu Đất và làng hoa.",Image="/images/dalat.jpg",Price=1500000 },
                new Place { Name="Hội An",Region="Quảng Nam",Category="Văn hóa",Description="Khám phá phố cổ, ẩm thực miền Trung và những đêm đèn lồng bên sông Hoài.",Image="/images/vietnam.jpg",Price=2000000 },
                new Place { Name="Vịnh Hạ Long",Region="Quảng Ninh",Category="Biển đảo",Description="Tham quan vịnh biển, đảo đá vôi và khám phá văn hóa làng chài.",Image="/images/vietnam.jpg",Price=2500000 },
                new Place { Name="Ninh Bình",Region="Miền Bắc",Category="Thiên nhiên",Description="Trải nghiệm Tràng An, Tam Cốc và những cung đường bình yên giữa non nước.",Image="/images/ninhbinh.jpg",Price=1200000 });
        foreach (var p in db.Places) if (!p.Image.StartsWith('/')) p.Image = p.Name == "Đà Lạt" ? "/images/dalat.jpg" : p.Name == "Ninh Bình" ? "/images/ninhbinh.jpg" : "/images/vietnam.jpg";
        if (!db.Services.Any())
            db.Services.AddRange(new TravelService {OwnerId=provider.Id,Name="Khám phá Đà Lạt cùng người bản địa",Description="Hướng dẫn viên địa phương, một ngày khám phá thành phố và trải nghiệm ẩm thực. Gửi yêu cầu để thống nhất lịch trình.",Price=450000},new TravelService {OwnerId=provider.Id,Name="Homestay giữa đồi thông",Description="Phòng đôi yên tĩnh, gần trung tâm. Giá tính theo phòng mỗi đêm, liên hệ xác nhận tình trạng phòng.",Price=650000,Category="Lưu trú"});
        foreach (var service in db.Services.Where(x => x.OwnerId == provider.Id))
        {
            service.Image="/images/dalat.jpg";
            if(service.Name.Contains("Homestay"))service.Category="Lưu trú";
        }
        db.SaveChanges();
        var start = DateTime.Today.AddDays(14);
        var trip = new Trip { OwnerId=traveler.Id,Name="Đà Lạt · Đi tìm bình yên",Destination="Đà Lạt",Start=start,End=start.AddDays(2),Budget=3500000,
            Activities = [new TripActivity{Title="Săn mây Cầu Đất",Location="Đồi chè Cầu Đất",Start=start.AddHours(5),End=start.AddHours(8),Cost=200000,Notes="Chuẩn bị áo ấm và kiểm tra thời tiết trước khi đi."},new TripActivity{Title="Dạo hồ Xuân Hương & cà phê",Location="Trung tâm Đà Lạt",Start=start.AddHours(9),End=start.AddHours(11),Cost=150000},new TripActivity{Title="Nhận phòng homestay",Location="Phường Lâm Viên",Start=start.AddHours(14),End=start.AddHours(15),Cost=1300000}] };
        db.Trips.Add(trip);
        db.Companions.AddRange(new Companion { OwnerId=friend.Id,Name=friend.Name,Destination="Đà Lạt",Date=start,People=3,Budget=2500000,Description="Mình tìm hai bạn cùng săn mây, đi cà phê và chụp ảnh. Di chuyển tự túc, chia sẻ chi phí rõ ràng trước chuyến đi." },new Companion { OwnerId=provider.Id,Name="Đà Lạt Local",Destination="Ninh Bình",Date=start.AddDays(7),People=4,Budget=2000000,Description="Nhóm thích thiên nhiên, lịch trình chậm rãi tham quan Tràng An và Hang Múa. Cùng trao đổi kế hoạch trước khi đi." });
        var firstService=db.Services.First(x=>x.OwnerId==provider.Id);
        db.Bookings.AddRange(new Booking {OwnerId=traveler.Id,TravelServiceId=firstService.Id,OperationId=Guid.NewGuid(),SnapshotName=firstService.Name,Date=start,People=2,Total=firstService.Price*2,Note="Muốn khởi hành lúc 8h, tư vấn giúp mình lịch trình phù hợp."},new Booking {OwnerId=traveler.Id,TravelServiceId=firstService.Id,OperationId=Guid.NewGuid(),SnapshotName=firstService.Name,Date=DateTime.Today.AddDays(-3),People=1,Total=firstService.Price,Status="Đã xác nhận",Note="Yêu cầu mẫu để trình diễn ghi nhận kết quả dịch vụ."});
        var pendingService=new TravelService {OwnerId=provider.Id,Name="Cà phê & câu chuyện cao nguyên",Description="Khám phá vườn cà phê, tìm hiểu quy trình và thử hương vị địa phương cùng người bản địa.",Category="Trải nghiệm",Location="Đà Lạt",Image="/images/dalat.jpg",Price=280000,ApprovalStatus="Chờ duyệt"};
        db.Services.Add(pendingService);db.SaveChanges();
        db.ServiceRevisions.Add(new ServiceRevision {TravelServiceId=pendingService.Id,Name=pendingService.Name,Description=pendingService.Description,Category=pendingService.Category,Location=pendingService.Location,Image=pendingService.Image,Price=pendingService.Price});
        db.Incidents.Add(new Incident {OwnerId=traveler.Id,TripId=trip.Id,Category="Thời tiết",Description="Dự báo có mưa trong buổi săn mây.",Plan="Theo dõi thời tiết trước 24 giờ, chuẩn bị lịch tham quan trong nhà.",Status="Đang xử lý"});
        db.Notifications.AddRange(new UserNotification{OwnerId=traveler.Id,Title="Chào Minh Anh, chuyến đi đã sẵn sàng!",Description="Bạn có một kế hoạch mẫu để thử chỉnh sửa lịch trình.",Url="/Journey"},new UserNotification{OwnerId=provider.Id,Title="Có yêu cầu dịch vụ đang chờ phản hồi",Description="Kiểm tra ngày phục vụ và điều kiện khách đề nghị trước khi xác nhận.",Url="/Provider/Requests"});
        db.DemoSeedMarkers.Add(new DemoSeedMarker {Key="complete-demo-v2"}); db.SaveChanges();
    }
}
