using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;

namespace TuTuc.Services;

public class JourneyManager(AppDbContext db, IDongHo? clock = null)
{
    readonly IDongHo Clock = clock ?? new DongHoHeThong();
    public static readonly string[] IncidentCategories = ["Điểm đến không phục vụ", "Thời tiết", "Dịch vụ có vấn đề", "Khác"];
    public static readonly string[] IncidentOutcomes = ["Đã giải quyết", "Giải quyết một phần", "Chưa giải quyết", "Dừng xử lý"];
    #region 1. Đọc kế hoạch — Bảng 4.177 và 4.180

    public Task<List<Trip>> List(int user) => db.Trips
        .AsNoTracking()
        .Include(x => x.Activities)
        .Where(x => x.OwnerId == user)
        .OrderByDescending(x => x.Start)
        .ThenByDescending(x => x.Id)
        .ToListAsync();

    public async Task<JourneyDetails?> Detail(int user, int id)
    {
        var t = await db.Trips
            .AsNoTracking()
            .Include(x => x.Activities)
            .SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == user);
        if (t == null)
            return null;

        // Cảnh báo được tính khi đọc; không lưu thành trạng thái trong DB.
        var warnings = new List<string>();
        var items = t.Activities.Where(x => x.HasTime).OrderBy(x => x.Start).ToList();
        for (var i = 0; i < items.Count; i++)
            for (var j = i + 1; j < items.Count; j++)
                if (items[i].Start.Date == items[j].Start.Date &&
                    items[i].Start < items[j].End &&
                    items[j].Start < items[i].End)
                    warnings.Add($"Trùng giờ: {items[i].Title} và {items[j].Title} ({items[i].Start:dd/MM}).");

        var total = t.Activities.Sum(x => x.Cost ?? 0);
        if (t.Budget.HasValue && total > t.Budget.Value)
            warnings.Add($"Tổng dự kiến vượt ngân sách {(total - t.Budget):N0} ₫.");

        // Chỉ gợi ý địa điểm, dịch vụ thuộc điểm đến của chuyến đi (DestinationFilter).
        // Mục mà hoạt động cũ đang dùng vẫn được giữ trong danh sách để khi sửa không bị mất nguồn.
        var allPlaces = await db.Places.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        var allServices = await PublishedServices();
        var usedPlaces = t.Activities.Where(a => a.PlaceId.HasValue).Select(a => a.PlaceId!.Value).ToHashSet();
        var usedServices = t.Activities.Where(a => a.TravelServiceId.HasValue).Select(a => a.TravelServiceId!.Value).ToHashSet();
        var placesHere = DestinationFilter.PlacesAt(allPlaces, t.Destination);
        var servicesHere = DestinationFilter.ServicesAt(allServices, placesHere, t.Destination);

        return new()
        {
            Trip = t,
            Warnings = warnings,
            Incidents = await db.Incidents
                .AsNoTracking()
                .Where(x => x.TripId == id && x.OwnerId == user)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(),
            Places = allPlaces.Where(p => placesHere.Contains(p) || usedPlaces.Contains(p.Id)).ToList(),
            Services = allServices.Where(s => servicesHere.Contains(s) || usedServices.Contains(s.Id)).ToList()
        };
    }

    Task<List<TravelService>> PublishedServices() => db.Services
        .AsNoTracking()
        .Where(x => x.ApprovalStatus == "Đã duyệt" && x.IsPublished &&
            db.Accounts.Any(a => a.Id == x.OwnerId && a.ProviderStatus != "Đã thu hồi"))
        .OrderBy(x => x.Name)
        .ToListAsync();

    // Thêm địa điểm/dịch vụ từ trang tra cứu vào kế hoạch: chỉ đưa ra các kế hoạch cùng điểm đến với mục đó.
    public async Task<(List<Trip> Trips, string SourceName)> TripsForSource(int user, string sourceType, int sourceId)
    {
        var places = await db.Places.AsNoTracking().ToListAsync();
        var place = sourceType == "Place" ? places.FirstOrDefault(x => x.Id == sourceId) : null;
        var service = sourceType == "Service" ? (await PublishedServices()).FirstOrDefault(x => x.Id == sourceId) : null;
        if (place == null && service == null)
            return ([], "");

        var trips = (await List(user)).Where(t =>
        {
            var here = DestinationFilter.PlacesAt(places, t.Destination);
            return place != null
                ? here.Any(p => p.Id == place.Id)
                : DestinationFilter.ServicesAt([service!], here, t.Destination).Count > 0;
        }).ToList();
        return (trips, place?.Name ?? service!.Name);
    }

    #endregion

    #region 2. Tạo và cập nhật kế hoạch — Bảng 4.179 và 4.184

    public async Task<JourneyOutcome> Save(int user, TripInput input)
    {
        if (!await CanWrite(user))
            return Failure("Tài khoản không có quyền lập kế hoạch.", 403);

        var invalid = JourneyRules.ValidateTrip(input);
        if (invalid.Length > 0)
            return Failure(invalid);

        Trip t;
        if (input.Id == 0)
        {
            if (input.Start.Date < Clock.Today)
                return Failure("Ngày bắt đầu kế hoạch mới không được trước hôm nay.");
            t = new()
            {
                OwnerId = user
            };
            db.Trips.Add(t);
        }
        else
        {
            t = (await db.Trips
                .Include(x => x.Activities)
                .SingleOrDefaultAsync(x => x.Id == input.Id && x.OwnerId == user))!;
            if (t == null)
                return Failure("Không tìm thấy kế hoạch.", 404);
            if (input.Start.Date < Clock.Today && input.Start.Date != t.Start.Date)
                return Failure("Chỉ được giữ ngày bắt đầu cũ đã qua, không đổi sang ngày quá khứ khác.", 400, t.Id);
            if (!Version(t, input.Version))
                return Failure("Phiên bản không hợp lệ. Hãy tải lại.", 409, t.Id);
            if (t.Activities.Any(x => x.Start.Date < input.Start.Date || x.End.Date > input.End.Date))
                return Failure("Khoảng ngày mới không chứa tất cả hoạt động. Sửa hoạt động trước.", 422, t.Id, "0201-07");
        }

        t.Name = input.Name.Trim();
        t.Destination = input.Destination.Trim();
        t.Start = input.Start.Date;
        t.End = input.End.Date;
        t.Budget = input.Budget;
        t.Status = "Nháp";
        t.UpdatedAt = Clock.UtcNow;

        try
        {
            await db.SaveChangesAsync();
            return new(true, input.Id == 0 ? "Đã tạo kế hoạch nháp." : "Đã lưu thông tin. Kế hoạch ở trạng thái Nháp.", t.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("Kế hoạch vừa được thay đổi. Nội dung đang nhập được giữ lại để bạn xem và thử lại.", 409, input.Id);
        }
    }

    #endregion

    #region 3. Thêm, sửa và bỏ hoạt động — Bảng 4.181 đến 4.183

    public async Task<JourneyOutcome> Activity(int user, ActivityInput input)
    {
        // 1. Kiểm tra quyền ghi và đọc đúng kế hoạch của khách hiện tại.
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền sửa kế hoạch.", 403, input.TripId);

        var t = await db.Trips
            .Include(x => x.Activities)
            .SingleOrDefaultAsync(x => x.Id == input.TripId && x.OwnerId == user);
        if (t == null)
            return Failure("Không tìm thấy kế hoạch.", 404, input.TripId);

        // 2. Kiểm tra ngày, giờ, nội dung, chi phí và hoạt động cần sửa.
        var invalid = JourneyRules.ValidateActivity(input, t);
        if (invalid.Length > 0)
            return Failure(invalid, 422, t.Id, "0201-03");
        if (input.Id > 0 && !t.Activities.Any(x => x.Id == input.Id))
            return Failure("Không tìm thấy hoạt động.", 404, t.Id);

        // 3. Kiểm tra nguồn tham khảo; tiêu đề trống thì dùng tên nguồn.
        string title = (input.Title ?? "").Trim();
        int? placeId = null, serviceId = null;
        if (input.SourceType == "Place")
        {
            var p = await db.Places.FindAsync(input.SourceId);
            if (p == null)
                return Failure("Địa điểm đã chọn không còn được công bố.", 422, t.Id, "0201-04");
            placeId = p.Id;
            if (title.Length == 0)
                title = p.Name;
        }

        if (input.SourceType == "Service")
        {
            var s = await db.Services.SingleOrDefaultAsync(x =>
                x.Id == input.SourceId &&
                x.ApprovalStatus == "Đã duyệt" &&
                x.IsPublished &&
                db.Accounts.Any(a => a.Id == x.OwnerId && a.ProviderStatus != "Đã thu hồi"));
            if (s == null)
                return Failure("Dịch vụ đã chọn không còn được công bố.", 422, t.Id, "0201-04");
            serviceId = s.Id;
            if (title.Length == 0)
                title = s.Name;
        }

        if (title.Length == 0 || title.Length > 300)
            return Failure("Nhập nội dung hoạt động hoặc chọn một mục tham khảo.", 400, t.Id);

        // 4. Chặn yêu cầu gửi từ phiên bản kế hoạch đã cũ.
        if (!Version(t, input.Version))
            return Failure("Phiên bản kế hoạch không hợp lệ.", 409, t.Id);

        // 5. Id = 0: thêm mới; Id > 0: sửa hoạt động thuộc kế hoạch này.
        TripActivity a;
        if (input.Id == 0)
        {
            a = new();
            t.Activities.Add(a);
        }
        else
        {
            a = t.Activities.SingleOrDefault(x => x.Id == input.Id)!;
            if (a == null)
                return Failure("Không tìm thấy hoạt động.", 404, t.Id);
        }

        a.Title = title;
        a.Start = input.HasTime ? input.Start : input.Start.Date;
        a.End = input.HasTime ? input.End : input.Start.Date.AddTicks(TimeSpan.TicksPerDay - 1);
        a.HasTime = input.HasTime;
        a.Location = (input.Location ?? "").Trim();
        a.Notes = (input.Notes ?? "").Trim();
        a.Cost = input.Cost;
        a.SortOrder = input.Id == 0
            ? t.Activities
                .Where(x => x != a && x.Start.Date == input.Start.Date)
                .Select(x => x.SortOrder)
                .DefaultIfEmpty(0)
                .Max() + 1
            : input.SortOrder;
        a.PlaceId = placeId;
        a.TravelServiceId = serviceId;

        // 6. Cập nhật cha để đổi rowversion; giao dịch ghi nằm ở DbContext.
        t.Status = "Nháp";
        t.UpdatedAt = Clock.UtcNow;

        try
        {
            await db.SaveChangesAsync();
            return new(true, "Đã lưu hoạt động. Trùng giờ và vượt ngân sách chỉ là cảnh báo để bạn cân nhắc.", t.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Có thể xuất hiện thay đổi sau bước 4 và trước lúc UPDATE.
            return Failure("Kế hoạch vừa thay đổi. Nội dung đang nhập được giữ lại; tải phiên bản mới trước khi gửi lại.", 409, t.Id);
        }
    }

    public async Task<JourneyOutcome> RemoveActivity(int user, int tripId, int id, string version)
    {
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền sửa kế hoạch.", 403, tripId);
        var t = await db.Trips
            .Include(x => x.Activities)
            .SingleOrDefaultAsync(x => x.Id == tripId && x.OwnerId == user);
        if (t == null)
            return Failure("Không tìm thấy kế hoạch.", 404, tripId);
        var a = t.Activities.SingleOrDefault(x => x.Id == id);
        if (a == null)
            return Failure("Không tìm thấy hoạt động.", 404, tripId);
        if (await db.IncidentActivities.AnyAsync(x => x.ActivityId == id))
            return Failure("Hoạt động đã gắn với tình huống phát sinh nên không thể bỏ.", 422, tripId, "0201-06");
        if (!Version(t, version))
            return Failure("Phiên bản không hợp lệ.", 409, tripId);
        db.Activities.Remove(a);
        t.Status = "Nháp";
        t.UpdatedAt = Clock.UtcNow;
        try
        {
            await db.SaveChangesAsync();
            return new(true, "Đã bỏ hoạt động. Kế hoạch trở về Nháp.", tripId);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("Kế hoạch vừa thay đổi. Hãy tải lại.", 409, tripId);
        }
        catch (DbUpdateException)
        {
            return Failure("Hoạt động đang được tham chiếu, không thể bỏ. Dữ liệu được giữ nguyên.", 422, tripId);
        }
    }

    #endregion

    #region 4. Chấp nhận và lưu nháp — Bảng 4.185 và 4.186

    public Task<JourneyOutcome> Confirm(int user, int id, string version) =>
        ChangeStatus(user, id, version, "Đã chấp nhận");

    public Task<JourneyOutcome> Draft(int user, int id, string version) =>
        ChangeStatus(user, id, version, "Nháp");

    async Task<JourneyOutcome> ChangeStatus(int user, int id, string version, string status)
    {
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền lập kế hoạch.", 403, id);
        var t = await db.Trips
            .Include(x => x.Activities)
            .SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == user);
        if (t == null)
            return Failure("Không tìm thấy kế hoạch.", 404, id);
        if (!Version(t, version))
            return Failure("Phiên bản không hợp lệ.", 409, id);
        if (t.End < t.Start ||
            string.IsNullOrWhiteSpace(t.Name) ||
            string.IsNullOrWhiteSpace(t.Destination) ||
            t.Activities.Any(x => x.Start.Date < t.Start.Date || x.End.Date > t.End.Date))
            return Failure("Dữ liệu bắt buộc hoặc ngày hoạt động không hợp lệ.", 422, id);

        t.Status = status;
        t.UpdatedAt = Clock.UtcNow;
        try
        {
            await db.SaveChangesAsync();
            return new(true, status == "Nháp"
                ? "Đã lưu nháp."
                : "Đã lưu kế hoạch ở trạng thái Đã chấp nhận. Các cảnh báo và yêu cầu dịch vụ vẫn được xử lý riêng.", id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("Kế hoạch đã thay đổi. Hãy xem phiên bản mới trước khi chấp nhận.", 409, id);
        }
    }

    #endregion

    #region 5. Tình huống phát sinh

    public async Task<JourneyIncidents> Incidents(int user, int? tripId = null)
    {
        var trips = await List(user);
        return new()
        {
            Trips = trips,
            SelectedTripId = tripId,
            Draft = new()
            {
                TripId = tripId ?? trips.FirstOrDefault()?.Id ?? 0
            },
            Incidents = await db.Incidents
                .AsNoTracking()
                .Where(x => x.OwnerId == user && (!tripId.HasValue || x.TripId == tripId))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync()
        };
    }

    public async Task<IncidentDetails?> Incident(int user, int id)
    {
        var i = await db.Incidents.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerId == user && x.Id == id);
        if (i == null || !i.TripId.HasValue)
            return null;
        var t = await db.Trips.AsNoTracking().SingleOrDefaultAsync(x => x.Id == i.TripId && x.OwnerId == user);
        if (t == null)
            return null;
        return new()
        {
            Incident = i,
            Trip = t,
            Activities = await db.IncidentActivities.Where(x => x.IncidentId == id).Select(x => x.Activity).AsNoTracking().ToListAsync(),
            Plans = await db.IncidentPlans.AsNoTracking().Where(x => x.IncidentId == id).OrderByDescending(x => x.ChosenAt).ToListAsync()
        };
    }

    public async Task<JourneyOutcome> AddIncident(int user, IncidentInput input)
    {
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền ghi nhận tình huống.", 403);
        if (!IncidentCategories.Contains(input.Category) || string.IsNullOrWhiteSpace(input.Description) || input.Description.Length > 2000)
            return Failure("Chọn loại và nhập mô tả tình huống tối đa 2000 ký tự.");
        if (!await db.Trips.AnyAsync(x => x.Id == input.TripId && x.OwnerId == user))
            return Failure("Phải chọn một chuyến đi của bạn.", 404);
        var ids = input.ActivityIds.Distinct().ToList();
        if (await db.Activities.CountAsync(x => ids.Contains(x.Id) && x.TripId == input.TripId) != ids.Count)
            return Failure("Có hoạt động được chọn không thuộc chuyến đi này.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var incident = new Incident
        {
            OwnerId = user,
            TripId = input.TripId,
            Category = input.Category,
            Description = input.Description.Trim(),
            Location = (input.Location ?? "").Trim(),
            Status = "Chưa giải quyết"
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();
        foreach (var activity in ids)
            db.IncidentActivities.Add(new() { IncidentId = incident.Id, ActivityId = activity });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, "Đã ghi nhận tình huống Chưa giải quyết.", incident.Id);
    }

    public async Task<JourneyOutcome> Plan(int user, IncidentPlanInput input)
    {
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền cập nhật tình huống.", 403, input.Id);
        if (string.IsNullOrWhiteSpace(input.Content) || input.Content.Length > 1000)
            return Failure("Nhập phương án tối đa 1000 ký tự.", 400, input.Id);
        await using var tx = await db.Database.BeginTransactionAsync();
        var i = await OwnIncident(user, input.Id);
        if (i == null)
            return Failure("Không tìm thấy tình huống.", 404, input.Id);
        if (IsClosed(i))
            return Failure("Tình huống đã kết thúc, chỉ có thể xem lịch sử.", 422, input.Id);
        if (!Version(i, input.Version))
            return Failure("Tình huống vừa thay đổi, hãy tải lại.", 409, input.Id);
        var old = await db.IncidentPlans.Where(x => x.IncidentId == i.Id && x.IsCurrent).ToListAsync();
        foreach (var p in old)
            p.IsCurrent = false;
        await db.SaveChangesAsync();
        db.IncidentPlans.Add(new() { IncidentId = i.Id, Content = input.Content.Trim() });
        i.Plan = input.Content.Trim();
        i.Status = "Đã chọn phương án";
        i.UpdatedAt = Clock.UtcNow;
        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(true, "Đã chọn phương án. Kết quả thực tế chưa được xác nhận.", i.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("Tình huống vừa được cập nhật. Lịch sử trước đó được giữ nguyên.", 409, i.Id);
        }
    }

    public async Task<JourneyOutcome> Result(int user, IncidentResultInput input)
    {
        if (!await CanWrite(user))
            return Failure("Bạn không có quyền cập nhật tình huống.", 403, input.Id);
        if (!IncidentOutcomes.Contains(input.Outcome) || (input.Resolution ?? "").Length > 2000)
            return Failure("Chọn kết quả hợp lệ và mô tả tối đa 2000 ký tự.", 400, input.Id);
        if (input.Outcome is "Đã giải quyết" or "Giải quyết một phần" && string.IsNullOrWhiteSpace(input.Resolution))
            return Failure("Phải mô tả kết quả thực tế khi đã giải quyết hoặc giải quyết một phần.", 400, input.Id);
        var i = await OwnIncident(user, input.Id);
        if (i == null)
            return Failure("Không tìm thấy tình huống.", 404, input.Id);
        if (IsClosed(i))
            return Failure("Tình huống đã kết thúc, chỉ có thể xem.", 422, input.Id);
        if (!Version(i, input.Version))
            return Failure("Phiên bản tình huống không hợp lệ.", 409, input.Id);
        i.Status = input.Outcome == "Chưa giải quyết" && !string.IsNullOrWhiteSpace(i.Plan) ? "Đã chọn phương án" : input.Outcome;
        i.Resolution = (input.Resolution ?? "").Trim();
        i.UpdatedAt = Clock.UtcNow;
        try
        {
            await db.SaveChangesAsync();
            return new(true, "Đã lưu đúng kết quả thực tế.", i.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure("Tình huống vừa thay đổi. Kết quả đang nhập được giữ để bạn kiểm tra.", 409, i.Id);
        }
    }

    #endregion

    #region 6. Hàm chung — quyền ghi, phiên bản và kết quả lỗi

    Task<Incident?> OwnIncident(int user, int id) => db.Incidents.SingleOrDefaultAsync(x =>
        x.Id == id && x.OwnerId == user &&
        db.Trips.Any(t => t.Id == x.TripId && t.OwnerId == user));

    static bool IsClosed(Incident i) => i.Status is "Đã giải quyết" or "Dừng xử lý";

    Task<bool> CanWrite(int user) => db.Accounts.AnyAsync(x =>
        x.Id == user && x.Role == "Traveler" && x.IsActive);

    bool Version<T>(T entity, string? version)
        where T : class
    {
        try
        {
            // 1. Đọc rowversion 8 byte mà form gửi về và so với bản vừa đọc.
            var bytes = Convert.FromBase64String(version ?? "");
            if (bytes.Length != 8 || !db.Entry(entity).Property<byte[]>("Version").CurrentValue.SequenceEqual(bytes))
                return false;

            // 2. EF kiểm tra phiên bản trong WHERE của UPDATE ngay lúc ghi.
            db.Entry(entity).Property("Version").OriginalValue = bytes;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static JourneyOutcome Failure(string message, int status = 400, int id = 0, string? code = null) =>
        new(false, message, id,
            code ?? (status == 404 ? "0201-02" : status == 409 ? "0201-05" : "0201-01"), status);

    #endregion
}
