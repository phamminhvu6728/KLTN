using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Services;
public partial class MarketplaceManager
{
    async Task<bool> Receives(TravelService service) { var provider=await db.Accounts.FindAsync(service.OwnerId); return provider!=null&&ProviderRules.CanReceiveNew(service,provider); }
    public async Task<BookingPreviewOutcome> Preview(int owner,BookingTermsInput terms)
    {
        if(!await db.Accounts.AnyAsync(x=>x.Id==owner&&x.IsActive&&x.Role=="Traveler"))return new(null,"Chỉ khách du lịch được gửi yêu cầu dịch vụ.",403,"0701-02");
        var invalid=ValidateTerms(terms,Clock.Today);if(invalid.Length>0)return new(null,invalid);
        var service=await db.Services.FindAsync(terms.Service);
        if(service==null||!service.IsPublished||service.ApprovalStatus!="Đã duyệt")return new(null,"Dịch vụ không còn được công bố, không thể gửi yêu cầu mới.",422,"0701-01");
        if(!await Receives(service))return new(null,"Dịch vụ hiện không tiếp nhận yêu cầu mới.",422,"0701-02");
        terms.Version=Convert.ToBase64String(service.Version);
        if(terms.Operation==Guid.Empty)terms.Operation=Guid.NewGuid();
        terms.PreviewToken=CreatePreviewToken(owner,terms);
        var warnings=new List<string>{"Gửi yêu cầu không bảo đảm còn chỗ. Chỉ thỏa thuận cùng điều kiện được xác nhận mới có hiệu lực."};
        if(service.ClosureStart.HasValue&&service.ClosureStart.Value.Date<=(terms.EndDate??terms.Date).Date&&(!service.ClosureEnd.HasValue||service.ClosureEnd.Value.Date>=terms.Date.Date))warnings.Add($"Tạm ngừng đã công bố: {service.ClosureStart:dd/MM/yyyy}–{service.ClosureEnd:dd/MM/yyyy}. {service.ClosureReason}. Phạm vi: {service.ClosureScope}. Hãy xem lại ngày yêu cầu.");
        if(await db.ServiceRevisions.AnyAsync(x=>x.TravelServiceId==service.Id&&x.Status=="Chờ duyệt"))warnings.Add("Dịch vụ có phiên bản đang chờ xét duyệt. Nội dung mới chưa có hiệu lực và chưa bảo đảm phản ánh khả năng phục vụ hiện tại.");
        return new(new BookingPreviewVm{Service=service,Terms=terms,Warnings=warnings},"");
    }
    public Task<string> Book(int owner,BookingTermsInput terms)=>Atomic(async()=>
    {
        if(terms.Operation==Guid.Empty)throw new BusinessFault("0701-03",400,"Lượt gửi không hợp lệ. Vui lòng kiểm tra lại yêu cầu.");
        if(await db.Bookings.AnyAsync(x=>x.OperationId==terms.Operation&&x.OwnerId==owner))return "Đã ghi nhận lượt gửi này trước đó; không tạo yêu cầu trùng.";
        var reviewed=ReadPreview(owner,terms.PreviewToken);
        if(reviewed==null||PreviewFingerprint(reviewed)!=PreviewFingerprint(terms))throw new BusinessFault("0701-05",409,"Nội dung xác nhận không hợp lệ, đã thay đổi hoặc hết hạn. Hãy xem trước lại yêu cầu.");
        var expectedVersion=terms.Version;var checkedTerms=await Preview(owner,terms);if(checkedTerms.Preview==null)throw new BusinessFault(checkedTerms.Code,checkedTerms.StatusCode,checkedTerms.Error);
        var service=checkedTerms.Preview.Service;if(expectedVersion!=Convert.ToBase64String(service.Version))throw new BusinessFault("0701-05",409,"Thông tin dịch vụ hoặc giá đã thay đổi. Hãy kiểm tra lại trước khi xác nhận gửi.");
        if(service.OwnerId==owner)throw new BusinessFault("0701-02",422,"Không thể gửi yêu cầu cho dịch vụ của chính mình.");
        var booking=new Booking{OwnerId=owner,TravelServiceId=service.Id,SnapshotName=service.Name,Date=terms.Date.Date,EndDate=terms.EndDate?.Date,People=terms.People,Guests=terms.Guests,ArrivalDeparture=Clean(terms.ArrivalDeparture),Options=Clean(terms.Options),Total=service.Price*terms.People,Note=Clean(terms.Note),OperationId=terms.Operation,Status="Chờ phản hồi",ConditionsVersion=1,CustomerAcceptedVersion=1,CreatedAt=Clock.UtcNow};
        db.Bookings.Add(booking);Capture(booking,owner,"Khách xác nhận gửi yêu cầu phiên bản 1");
        return await Save("Đã gửi yêu cầu, chờ nhà cung cấp phản hồi. Chưa hình thành thỏa thuận dịch vụ.");
    },"0701-05");
    public async Task<RequestListVm> Requests(int owner,bool provider,int? requestId=null,int page=1,string? status=null)
    {
        var query=db.Bookings.AsNoTracking().Include(x=>x.TravelService).Where(x=>provider?x.TravelService!.OwnerId==owner:x.OwnerId==owner);
        if(requestId.HasValue)query=query.Where(x=>x.Id==requestId.Value);
        if(!string.IsNullOrWhiteSpace(status))query=query.Where(x=>x.Status==status);
        var totalCount=await query.CountAsync();
        var ordered=query.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id);
        var skip=(long)(Math.Max(1,page)-1)*20;
        var bookings=!requestId.HasValue&&skip>int.MaxValue?new List<Booking>():await (requestId.HasValue?ordered.AsQueryable():ordered.Skip((int)skip).Take(20)).ToListAsync();
        var ids=bookings.Select(x=>x.Id).ToArray();var results=await db.BookingItemResults.Where(x=>ids.Contains(x.BookingId)).ToListAsync();var resultIds=results.Select(x=>x.Id).ToArray();
        var people=bookings.Select(x=>x.OwnerId).Concat(bookings.Select(x=>x.TravelService!.OwnerId)).Distinct().ToArray();
        var contactIds=provider?bookings.Where(x=>x.Status=="Đã xác nhận").Select(x=>x.OwnerId).Distinct().ToArray():Array.Empty<int>();
        var contacts=await db.Accounts.AsNoTracking().Where(x=>contactIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Phone+" · "+x.Email);
        return new RequestListVm{CustomerContacts=contacts,Page=page,TotalCount=totalCount,StatusFilter=status,FeedbackWindowDays=FeedbackWindowDays,Bookings=bookings,Changes=await db.BookingChanges.Where(x=>ids.Contains(x.BookingId)).OrderByDescending(x=>x.Id).ToListAsync(),History=await db.BookingConditionVersions.Where(x=>ids.Contains(x.BookingId)).OrderByDescending(x=>x.VersionNumber).ToListAsync(),Results=results,ResultHistory=await db.BookingResultHistories.Where(x=>resultIds.Contains(x.BookingItemResultId)).OrderByDescending(x=>x.Id).ToListAsync(),PeopleNames=await db.Accounts.Where(x=>people.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Name),IsProvider=provider};
    }
    static void ApplyTerms(Booking booking,BookingTermsInput terms)
    {
        booking.Date=terms.Date.Date;booking.EndDate=terms.EndDate?.Date;booking.People=terms.People;booking.Guests=terms.Guests;booking.ArrivalDeparture=Clean(terms.ArrivalDeparture);booking.Options=Clean(terms.Options);booking.Total=terms.Total;booking.Note=Clean(terms.Note);
    }
    public Task<string> Respond(int owner,int id,string decision,string version,int conditionsVersion,string? reason,BookingTermsInput? offer=null)=>Atomic(async()=>
    {
        var booking=await db.Bookings.Include(x=>x.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.TravelService!.OwnerId==owner);
        if(booking==null)throw new BusinessFault("0701-04",404,"Không tìm thấy yêu cầu thuộc nhà cung cấp của bạn.");
        if(!new[]{"Chờ phản hồi","Chờ xác nhận","Chờ NCC xác nhận cuối"}.Contains(booking.Status)||booking.ConditionsVersion!=conditionsVersion)throw new BusinessFault("0701-05",409,"Yêu cầu đã thay đổi hoặc không chờ phản hồi trên phiên bản này.");
        if(!VersionMatches(db,booking,version))throw new BusinessFault("0701-05",409,"Phiên bản không hợp lệ.");
        if(!new[]{"Đã xác nhận","Từ chối","Đề xuất khác"}.Contains(decision))throw new BusinessFault("0701-03",400,"Phản hồi không hợp lệ.");
        if(decision!="Từ chối"&&!await Receives(booking.TravelService!))throw new BusinessFault("0701-02",422,"Dịch vụ không còn tiếp nhận yêu cầu mới. Không thể xác nhận hoặc đề xuất thỏa thuận mới.");
        if(Clean(reason).Length>(decision=="Đề xuất khác"?2000:500))throw new BusinessFault("0701-03",400,"Lý do phản hồi vượt quá giới hạn ký tự.");
        if(decision=="Từ chối"&&string.IsNullOrWhiteSpace(reason))throw new BusinessFault("0701-06",400,"Vui lòng ghi lý do từ chối.");
        if(decision=="Đề xuất khác")
        {
            if(offer==null||string.IsNullOrWhiteSpace(reason))throw new BusinessFault("0701-03",400,"Phải nhập điều kiện đề xuất và lời giải thích.");
            var invalid=ValidateTerms(offer,Clock.Today);if(invalid.Length>0)throw new BusinessFault("0701-03",400,invalid);
            var previous=Snapshot(booking);await EnsureSnapshot(booking,owner);
            var compare=new Booking{SnapshotName=booking.SnapshotName,Date=offer.Date.Date,EndDate=offer.EndDate?.Date,People=offer.People,Guests=offer.Guests,ArrivalDeparture=Clean(offer.ArrivalDeparture),Options=Clean(offer.Options),Total=offer.Total,Note=Clean(offer.Note),ConditionsVersion=booking.ConditionsVersion};
            if(previous==Snapshot(compare))throw new BusinessFault("0701-07",422,"Điều kiện đề xuất phải khác điều kiện hiện tại.");
            ApplyTerms(booking,offer);booking.ConditionsVersion++;booking.ProviderAcceptedVersion=booking.ConditionsVersion;booking.CustomerAcceptedVersion=0;booking.ProviderOfferReason=Clean(reason);booking.Status="Đề xuất khác";Capture(booking,owner,"Nhà cung cấp đề xuất điều kiện khác: "+Clean(reason));
        }
        else if(decision=="Đã xác nhận")
        {
            if(booking.CustomerAcceptedVersion!=booking.ConditionsVersion)throw new BusinessFault("0701-05",409,"Khách chưa chấp thuận đúng phiên bản điều kiện hiện tại.");
            if(booking.Status=="Chờ NCC xác nhận cuối"&&booking.ProviderAcceptedVersion!=booking.ConditionsVersion)throw new BusinessFault("0701-05",409,"Nhà cung cấp chưa xác lập đề xuất đúng phiên bản hiện tại.");
            await EnsureSnapshot(booking,owner);booking.ProviderAcceptedVersion=booking.ConditionsVersion;booking.Status="Đã xác nhận";
        }
        else booking.Status="Từ chối";
        booking.ResponseReason=Clean(reason);booking.RespondedAt=Clock.UtcNow;
        return await Save(decision=="Đề xuất khác"?"Đã gửi điều kiện mới cho khách; chưa tạo thỏa thuận.":"Đã ghi phản hồi yêu cầu dịch vụ.");
    },"0701-05");
    public Task<string> ReplyOffer(int owner,int id,bool accept,string version,int conditionsVersion)=>Atomic(async()=>
    {
        var booking=await db.Bookings.Include(x=>x.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);
        if(booking==null)throw new BusinessFault("0701-04",404,"Không tìm thấy yêu cầu.");
        if(booking.Status!="Đề xuất khác"||booking.ConditionsVersion!=conditionsVersion||!VersionMatches(db,booking,version))throw new BusinessFault("0701-05",409,"Đề xuất đã thay đổi. Hãy mở lại đúng phiên bản mới nhất.");
        if(accept&&!await Receives(booking.TravelService!)){booking.Status="Không tiếp tục";booking.ResponseReason="Nhà cung cấp không còn tiếp nhận yêu cầu mới.";return await Save("Không thể tiếp tục vì dịch vụ đã ngừng tiếp nhận.");}
        booking.CustomerAcceptedVersion=accept?booking.ConditionsVersion:0;booking.Status=accept?"Đã xác nhận":"Không tiếp tục";
        return await Save(accept?"Đã xác nhận thỏa thuận đúng phiên bản hai bên đã chấp thuận.":"Đã kết thúc yêu cầu. Không hình thành thỏa thuận.");
    },"0701-05");
    public Task<string> EditPending(int owner,int id,BookingTermsInput terms)=>Atomic(async()=>
    {
        var booking=await db.Bookings.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);
        if(booking==null)throw new BusinessFault("0801-01",404,"Không tìm thấy yêu cầu.");
        if(!new[]{"Chờ phản hồi","Chờ xác nhận"}.Contains(booking.Status))throw new BusinessFault("0801-02",422,ThongDiep.DanhMuc["0801-02"].NoiDung);
        if(booking.ConditionsVersion!=terms.ConditionsVersion||!VersionMatches(db,booking,terms.Version))throw new BusinessFault("0801-03",409,"Yêu cầu vừa được cập nhật. Hãy tải lại.");
        var invalid=ValidateTerms(terms,Clock.Today);if(invalid.Length>0)throw new BusinessFault("0701-03",400,invalid);
        var service=await db.Services.FindAsync(booking.TravelServiceId);
        if(service==null||!await Receives(service))throw new BusinessFault("0701-02",422,"Dịch vụ không còn tiếp nhận yêu cầu mới. Không thể gửi lại điều kiện mới.");
        await EnsureSnapshot(booking,owner);ApplyTerms(booking,terms);booking.ConditionsVersion++;booking.CustomerAcceptedVersion=booking.ConditionsVersion;booking.ProviderAcceptedVersion=0;booking.Status="Chờ phản hồi";Capture(booking,owner,"Khách sửa yêu cầu chưa xác nhận");
        return await Save("Đã sửa yêu cầu và gửi lại phiên bản mới. Phản hồi trên phiên bản cũ không còn hợp lệ.");
    },"0701-05");
    public Task<string> Withdraw(int owner,int id,string version)=>Atomic(async()=>
    {
        var booking=await db.Bookings.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);
        if(booking==null)throw new BusinessFault("0801-01",404,"Không tìm thấy yêu cầu.");
        if(!PendingBookingStates.Contains(booking.Status))throw new BusinessFault("0801-02",422,"Yêu cầu không còn ở trạng thái được rút trực tiếp.");
        if(!VersionMatches(db,booking,version))throw new BusinessFault("0701-05",409,"Phiên bản không hợp lệ.");booking.Status="Không tiếp tục";
        return await Save("Đã rút yêu cầu. Không hình thành thỏa thuận; lịch sử được giữ nguyên.");
    },"0701-05");
}


