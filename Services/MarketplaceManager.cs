using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Services;
public partial class MarketplaceManager(AppDbContext db, IDataProtectionProvider protection, IDongHo? clock=null, IConfiguration? configuration=null)
{
    readonly IDongHo Clock=clock??new DongHoHeThong();
    public int FeedbackWindowDays => Math.Clamp(configuration?.GetValue<int?>("Business:HanPhanAnhNgay")??30,1,365);
    readonly ITimeLimitedDataProtector previewProtection = protection.CreateProtector("TuTuc.BookingPreview.v1").ToTimeLimitedDataProtector();
    public static DateTime Today => DateTime.UtcNow.AddHours(7).Date;
    public static readonly string[] PendingBookingStates = ["Chờ phản hồi", "Chờ xác nhận", "Đề xuất khác", "Chờ NCC xác nhận cuối"];
    public static readonly string[] ActiveChangeStates = ["Chờ phản hồi", "Đề xuất khác"];
    public static readonly string[] Outcomes = ["Hoàn thành", "Thực hiện một phần", "Không thực hiện", "Khách không đến", "Chưa xác định"];
    static string Clean(string? value) => value?.Trim() ?? "";
    static bool VersionMatches<T>(AppDbContext context, T entity, string? version) where T : class
    {
        try { var bytes = Convert.FromBase64String(version ?? ""); if (bytes.Length != 8) return false; context.Entry(entity).Property("Version").OriginalValue = bytes; return true; }
        catch (FormatException) { return false; }
    }
    static async Task SafeRollback(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction) { try { await transaction.RollbackAsync(); } catch (InvalidOperationException) { } }
    async Task<string> Atomic(Func<Task<string>> operation,string? conflictCode=null)
    {
        for(var attempt=1;;attempt++)
        {
            await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try{var message=await operation();await transaction.CommitAsync();return message;}
            catch(Exception error) when(error.GetBaseException() is Microsoft.Data.SqlClient.SqlException sql && (sql.Number==1205||sql.Number==-2) && attempt<3)
            {await SafeRollback(transaction);db.ChangeTracker.Clear();await Task.Delay(Random.Shared.Next(200,800));}
            catch(DbUpdateConcurrencyException){await SafeRollback(transaction);if(conflictCode!=null)throw new BusinessFault(conflictCode,409,"Dữ liệu đã thay đổi ở phiên khác. Vui lòng tải lại và xem đúng phiên bản mới nhất.");return "Dữ liệu đã thay đổi ở phiên khác. Vui lòng tải lại và xem đúng phiên bản mới nhất.";}
            catch(DbUpdateException){await SafeRollback(transaction);if(conflictCode!=null)throw new BusinessFault(conflictCode,409,"Không thể ghi thay đổi do xung đột dữ liệu. Hãy tải lại phiên bản mới nhất.");return "Không thể ghi thay đổi hoặc đã có đề nghị đang chờ. Dữ liệu trước đó được giữ nguyên; vui lòng tải lại.";}
            catch(Microsoft.Data.SqlClient.SqlException){await SafeRollback(transaction);if(conflictCode!=null)throw new BusinessFault(conflictCode,409,"Giao dịch đang xung đột. Dữ liệu chưa được thay đổi; hãy tải lại và thử lại.");return "Giao dịch đang xung đột với thao tác khác. Dữ liệu chưa được thay đổi; hãy tải lại và thử lại.";}
        }
    }
    async Task<string> Save(string message) { await db.SaveChangesAsync(); return message; }
    void Notify(int owner,string title,string description,string url) => db.Notifications.Add(new UserNotification { OwnerId=owner,Title=title,Description=description,Url=url });
    public static string Snapshot(Booking booking) => JsonSerializer.Serialize(new { booking.SnapshotName, booking.Date, booking.EndDate, booking.Guests, Quantity=booking.People, booking.ArrivalDeparture, booking.Options, booking.Total, booking.Note, booking.ConditionsVersion });
    public static string DescribeSnapshot(string snapshot)
    {
        try { using var doc=JsonDocument.Parse(snapshot); var r=doc.RootElement; string S(string key)=>r.TryGetProperty(key,out var v)?v.ToString():""; string D(string key)=>DateTime.TryParse(S(key),out var d)?d.ToString("dd/MM/yyyy"):""; return $"{S("SnapshotName")}; ngày {D("Date")}–{D("EndDate")}; {S("Guests")} người; {S("Quantity")} đơn vị; tổng {S("Total")} ₫; {S("ArrivalDeparture")}; hạng mục: {S("Options")}; ghi chú: {S("Note")}"; }
        catch(JsonException){return snapshot;}
    }
    void Capture(Booking booking,int actor,string reason) => db.BookingConditionVersions.Add(new BookingConditionVersion { Booking=booking,VersionNumber=booking.ConditionsVersion,Snapshot=Snapshot(booking),ActorId=actor,Reason=reason });
    async Task EnsureSnapshot(Booking booking,int actor)
    {
        if (!await db.BookingConditionVersions.AnyAsync(x=>x.BookingId==booking.Id&&x.VersionNumber==booking.ConditionsVersion)) Capture(booking,actor,"Điều kiện trước thao tác");
    }
    public static List<(string Key,string Title)> Items(Booking booking)
    {
        var result = new List<(string,string)> { ("main", $"{booking.SnapshotName} × {booking.People}") };
        var lines = Clean(booking.Options).Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        for(var i=0;i<lines.Length;i++)result.Add(($"option-{i+1}",lines[i]));
        return result;
    }
    public static string ValidateTerms(BookingTermsInput terms) => ValidateTerms(terms,Today);
    public static string ValidateTerms(BookingTermsInput terms,DateTime today)
    {
        if(terms.Date.Date<today||terms.Date==default||terms.EndDate?.Date<terms.Date.Date)return "Ngày bắt đầu/kết thúc chưa hợp lệ (ngày kết thúc phải từ ngày bắt đầu).";
        if(terms.People<1||terms.People>100||terms.Guests<1||terms.Guests>100||terms.Total<0||terms.Total>1000000000)return "Số người, số lượng phải từ 1–100 và chi phí phải từ 0–1 tỷ đồng.";
        var options=Clean(terms.Options).Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if(options.Length>10||options.Any(x=>x.Length>150)||options.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=options.Length||Clean(terms.Options).Length>1000)return "Hạng mục chọn thêm: tối đa 10 dòng riêng biệt, mỗi dòng không quá 150 ký tự, không trùng nhau.";
        if(Clean(terms.Note).Length>1000||Clean(terms.ArrivalDeparture).Length>100)return "Ghi chú hoặc thời gian đến/đi vượt giới hạn ký tự.";
        return "";
    }
    public static string ValidateService(ServiceEditVm input)
    {
        if(string.IsNullOrWhiteSpace(input.Name)||string.IsNullOrWhiteSpace(input.Location)||!new[]{"Trải nghiệm","Lưu trú","Di chuyển","Ăn uống"}.Contains(input.Category)||input.Price<0||input.Price>1000000000)return "Tên, loại dịch vụ, địa điểm hoặc đơn giá chưa hợp lệ.";
        if(Clean(input.Image).Length>0&&SafeImage(input.Image).Length==0)return "Ảnh phải là đường dẫn HTTPS hợp lệ hoặc ảnh nội bộ /images/.";
        var hasClosure=input.ClosureStart.HasValue||input.ClosureEnd.HasValue||!string.IsNullOrWhiteSpace(input.ClosureReason)||!string.IsNullOrWhiteSpace(input.ClosureScope);
        if(hasClosure&&(!input.ClosureStart.HasValue||string.IsNullOrWhiteSpace(input.ClosureReason)||string.IsNullOrWhiteSpace(input.ClosureScope)||input.ClosureEnd?.Date<input.ClosureStart?.Date||input.ClosureEnd?.Date<Today))return "Thông báo tạm ngừng cần ngày bắt đầu, lý do và phạm vi; ngày kết thúc không trước ngày bắt đầu/hôm nay.";
        return "";
    }
    static string SafeImage(string? image)
    {
        image=Clean(image);if(image.StartsWith("/images/",StringComparison.Ordinal)&&!image.Contains(".."))return image;
        return Uri.TryCreate(image,UriKind.Absolute,out var uri)&&uri.Scheme=="https"?image:"";
    }
    static void CopyContent(TravelService service,ServiceEditVm input)
    {
        service.Name=Clean(input.Name);service.Description=Clean(input.Description);service.Category=input.Category;service.Location=Clean(input.Location);service.Image=SafeImage(input.Image);service.Price=input.Price;service.ServingHours=Clean(input.ServingHours);service.Conditions=Clean(input.Conditions);service.ClosureStart=input.ClosureStart?.Date;service.ClosureEnd=input.ClosureEnd?.Date;service.ClosureReason=Clean(input.ClosureReason);service.ClosureScope=Clean(input.ClosureScope);service.UpdatedAt=DateTime.UtcNow;
    }
    static string ContentKey(TravelService service)=>JsonSerializer.Serialize(new {service.Name,service.Description,service.Category,service.Location,service.Price,service.Image,service.ServingHours,service.Conditions,service.ClosureStart,service.ClosureEnd,service.ClosureReason,service.ClosureScope});
    public Task<string> SubmitService(int owner,ServiceEditVm input)=>Atomic(async()=>
    {
        var user=await db.Accounts.FindAsync(owner);if(!ProviderRules.CanPublish(user))return "Chưa có quyền đăng nội dung. Hãy xem hồ sơ xác minh hoặc quyết định quyền hoạt động.";
        var invalid=ValidateService(input);if(invalid.Length>0)return invalid;
        var isDraft=input.Operation=="SaveDraft";if(!isDraft&&input.Operation!="Submit")return "Thao tác không hợp lệ.";
        TravelService? service=null;
        if(input.Id>0)
        {
            service=await db.Services.SingleOrDefaultAsync(x=>x.Id==input.Id&&x.OwnerId==owner);if(service==null)return "Không tìm thấy dịch vụ của bạn.";
            if(!VersionMatches(db,service,input.Version))return "Phiên bản dịch vụ không hợp lệ. Hãy tải lại.";
            if(await db.ServiceRevisions.AnyAsync(x=>x.TravelServiceId==service.Id&&x.Status=="Chờ duyệt"))return "Đã có một phiên bản chờ duyệt, chưa thể gửi thêm.";
            if(isDraft&&service.ApprovalStatus!="Nháp")return "Chỉ lưu nháp trực tiếp cho dịch vụ chưa gửi đăng; nội dung đã gửi cần tạo bản cập nhật.";
        }
        if(service==null){service=new TravelService{OwnerId=owner,ApprovalStatus=isDraft?"Nháp":"Chờ duyệt"};CopyContent(service,input);db.Services.Add(service);}
        else if(service.ApprovalStatus=="Nháp")CopyContent(service,input);
        else
        {
            var compare=new TravelService();CopyContent(compare,input);
            if(ContentKey(service)==ContentKey(compare))return "Nội dung chưa có thay đổi so với bản hiện tại.";
        }
        if(isDraft)return await Save("Đã lưu nháp. Dịch vụ chưa chuyển xét duyệt và chưa công bố.");
        var number=service.Id==0?1:(await db.ServiceRevisions.Where(x=>x.TravelServiceId==service.Id).MaxAsync(x=>(int?)x.RevisionNumber)??0)+1;
        db.ServiceRevisions.Add(new ServiceRevision{TravelService=service,RevisionNumber=number,Name=Clean(input.Name),Description=Clean(input.Description),Category=input.Category,Location=Clean(input.Location),Price=input.Price,Image=SafeImage(input.Image),ServingHours=Clean(input.ServingHours),Conditions=Clean(input.Conditions),ClosureStart=input.ClosureStart?.Date,ClosureEnd=input.ClosureEnd?.Date,ClosureReason=Clean(input.ClosureReason),ClosureScope=Clean(input.ClosureScope)});
        if(service.PublishedRevisionNumber==null&&service.ApprovalStatus!="Đã duyệt"){CopyContent(service,input);service.ApprovalStatus="Chờ duyệt";}
        service.UpdatedAt=DateTime.UtcNow;
        foreach(var admin in await db.Accounts.Where(x=>x.Role=="Admin"&&x.IsActive).Select(x=>x.Id).ToListAsync())Notify(admin,"Có phiên bản dịch vụ chờ duyệt",input.Name,"/Admin");
        return await Save("Đã gửi phiên bản dịch vụ để xét duyệt. Nội dung đang công bố được giữ nguyên.");
    });
    public Task<string> Review(int admin,int id,string decision,string? note,string version)=>Atomic(async()=>
    {
        if(!await db.Accounts.AnyAsync(x=>x.Id==admin&&x.Role=="Admin"&&x.IsActive))return "Không có quyền quản trị.";
        var revision=await db.ServiceRevisions.Include(x=>x.TravelService).SingleOrDefaultAsync(x=>x.Id==id);
        if(revision==null||revision.Status!="Chờ duyệt"||await db.ServiceRevisions.AnyAsync(x=>x.TravelServiceId==revision.TravelServiceId&&x.RevisionNumber>revision.RevisionNumber))return "Phiên bản đã được xử lý hoặc có phiên bản mới hơn.";
        if(!VersionMatches(db,revision,version))return "Phiên bản không hợp lệ.";
        if(Clean(note).Length>500)return "Nhận xét xét duyệt không quá 500 ký tự.";
        if(!new[]{"Đã duyệt","Từ chối","Cần sửa"}.Contains(decision))return "Quyết định không hợp lệ.";
        if(decision!="Đã duyệt"&&string.IsNullOrWhiteSpace(note))return "Phải ghi lý do từ chối hoặc nội dung cần sửa.";
        var service=revision.TravelService!;var owner=await db.Accounts.FindAsync(service.OwnerId);
        if(decision=="Đã duyệt"&&!ProviderRules.CanPublish(owner))return "Nhà cung cấp không đủ quyền hoạt động; chỉ có thể từ chối hoặc yêu cầu sửa.";
        revision.Status=decision;revision.ReviewNote=Clean(note);revision.ReviewedById=admin;revision.ReviewedAt=DateTime.UtcNow;
        if(decision=="Đã duyệt")
        {
            CopyContent(service,new ServiceEditVm{Name=revision.Name,Description=revision.Description,Category=revision.Category,Location=revision.Location,Image=revision.Image,Price=revision.Price,ServingHours=revision.ServingHours,Conditions=revision.Conditions,ClosureStart=revision.ClosureStart,ClosureEnd=revision.ClosureEnd,ClosureReason=revision.ClosureReason,ClosureScope=revision.ClosureScope});
            service.PublishedRevisionNumber=revision.RevisionNumber;service.ApprovalStatus="Đã duyệt";service.IsPublished=true;
            if(!service.IsAccepting&&service.BlockedByDecisionId.HasValue&&owner?.CurrentGovernanceDecisionId==null){service.IsAccepting=true;service.BlockedByDecisionId=null;}
        }
        else if(service.PublishedRevisionNumber==null&&service.ApprovalStatus!="Đã duyệt")service.ApprovalStatus=decision;
        Notify(service.OwnerId,"Phiên bản dịch vụ: "+decision,revision.Name+". "+revision.ReviewNote,"/Provider");
        return await Save("Đã ghi quyết định cho đúng phiên bản nội dung.");
    });
}





