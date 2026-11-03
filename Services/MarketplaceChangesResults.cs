using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
namespace TuTuc.Services;
public partial class MarketplaceManager
{
    static BookingTermsInput ChangeTerms(BookingChange change)=>new(){Date=change.Date,EndDate=change.EndDate,People=change.People,Guests=change.Guests,ArrivalDeparture=change.ArrivalDeparture,Options=change.Options,Total=change.Total};
    static void CopyChange(BookingChange change,BookingTermsInput terms){change.Date=terms.Date.Date;change.EndDate=terms.EndDate?.Date;change.People=terms.People;change.Guests=terms.Guests;change.ArrivalDeparture=Clean(terms.ArrivalDeparture);change.Options=Clean(terms.Options);change.Total=terms.Total;}
    async Task<bool> CanChange(Booking booking)=>booking.Status=="Đã xác nhận"&&booking.Date.Date>Clock.Today&&!await db.BookingItemResults.AnyAsync(x=>x.BookingId==booking.Id)&&string.IsNullOrEmpty(booking.Result);
    public Task<string> ProposeChange(int owner,int id,string kind,BookingTermsInput terms)=>Atomic(async()=>
    {
        var booking=await db.Bookings.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);
        if(booking==null)return "Không tìm thấy thỏa thuận của bạn.";
        if(!await CanChange(booking))return "Chỉ gửi đổi/hủy trước ngày phục vụ, khi thỏa thuận đã xác nhận và chưa có kết quả thực hiện.";
        if(!VersionMatches(db,booking,terms.Version)||booking.ConditionsVersion!=terms.ConditionsVersion)return "Điều kiện đã thay đổi. Vui lòng tải lại.";
        if(await db.BookingChanges.AnyAsync(x=>x.BookingId==id&&ActiveChangeStates.Contains(x.Status)))return "Đã có đề nghị chờ phản hồi hoặc điều kiện khác. Hãy xử lý đề nghị đó trước.";
        if(kind!="Điều chỉnh"&&kind!="Hủy")return "Loại đề nghị không hợp lệ.";
        if(Clean(terms.Reason).Length>500)return "Lý do đề nghị tối đa 500 ký tự.";
        if(string.IsNullOrWhiteSpace(terms.Reason))return "Phải nhập lý do thay đổi hoặc hủy.";
        if(kind=="Điều chỉnh")
        {
            var invalid=ValidateTerms(terms);if(invalid.Length>0)return invalid;
            if(booking.Date.Date==terms.Date.Date&&booking.EndDate?.Date==terms.EndDate?.Date&&booking.People==terms.People&&booking.Guests==terms.Guests&&booking.Total==terms.Total&&booking.ArrivalDeparture==Clean(terms.ArrivalDeparture)&&booking.Options==Clean(terms.Options))return "Đề nghị phải có ít nhất một điều kiện mới khác với thỏa thuận hiện tại.";
        }
        await EnsureSnapshot(booking,owner);
        var proposal=new BookingChange{BookingId=id,RequesterId=owner,Kind=kind,BaseConditionsVersion=booking.ConditionsVersion,OldConditions=Snapshot(booking),Reason=Clean(terms.Reason)};
        CopyChange(proposal,kind=="Hủy"?new BookingTermsInput{Date=booking.Date,EndDate=booking.EndDate,People=booking.People,Guests=booking.Guests,Total=booking.Total,Options=booking.Options,ArrivalDeparture=booking.ArrivalDeparture}:terms);
        db.BookingChanges.Add(proposal);
        // Force parent rowversion change so a concurrent service-result write cannot slip past the eligibility read.
        booking.RespondedAt=Clock.UtcNow;
        var providerId=await db.Services.Where(x=>x.Id==booking.TravelServiceId).Select(x=>x.OwnerId).SingleAsync();Notify(providerId,"Đề nghị "+kind.ToLower()+" thỏa thuận #"+id,proposal.Reason,"/Provider/Requests");
        return await Save("Đã gửi đề nghị. Thỏa thuận cũ tiếp tục có hiệu lực đến khi có quyết định hợp lệ.");
    });
    async Task ApplyChange(BookingChange change,Booking booking,int actor)
    {
        await EnsureSnapshot(booking,actor);
        if(change.Kind=="Hủy")booking.Status="Đã hủy";
        else{var note=booking.Note;ApplyTerms(booking,ChangeTerms(change));booking.Note=note;booking.ConditionsVersion++;booking.CustomerAcceptedVersion=booking.ConditionsVersion;booking.ProviderAcceptedVersion=booking.ConditionsVersion;Capture(booking,actor,"Áp dụng đề nghị điều chỉnh #"+change.Id);}
        change.Status="Chấp nhận";change.RespondedAt=Clock.UtcNow;booking.RespondedAt=Clock.UtcNow;
    }
    public Task<string> ReplyChange(int provider,int id,string decision,string version,int logicalVersion,string? reason,BookingTermsInput? alternative=null)=>Atomic(async()=>
    {
        var change=await db.BookingChanges.Include(x=>x.Booking).ThenInclude(x=>x!.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.Booking!.TravelService!.OwnerId==provider);
        if(change?.Booking==null)return "Không tìm thấy đề nghị thuộc nhà cung cấp của bạn.";
        var booking=change.Booking;
        if(!ProviderRules.CanFulfillExisting(await db.Accounts.FindAsync(provider)))return "Tài khoản không được xử lý thỏa thuận này.";
        if(change.Status!="Chờ phản hồi"||change.LogicalVersion!=logicalVersion||booking.ConditionsVersion!=change.BaseConditionsVersion||!await CanChange(booking)||!VersionMatches(db,change,version))return "Đề nghị hoặc thỏa thuận đã thay đổi/không còn trong điều kiện đổi hủy.";
        if(!new[]{"Chấp nhận","Từ chối","Đề xuất khác"}.Contains(decision))return "Phản hồi không hợp lệ.";
        if(Clean(reason).Length>(decision=="Đề xuất khác"?2000:500))return "Lý do phản hồi vượt quá giới hạn ký tự.";
        if(decision=="Từ chối"&&string.IsNullOrWhiteSpace(reason))return "Phải nhập lý do từ chối đề nghị.";
        if(decision=="Đề xuất khác")
        {
            if(change.Kind!="Điều chỉnh"||alternative==null||string.IsNullOrWhiteSpace(reason))return "Chỉ đề xuất điều kiện khác cho đề nghị thay đổi, kèm lời giải thích.";
            var invalid=ValidateTerms(alternative);if(invalid.Length>0)return invalid;
            CopyChange(change,alternative);change.ProviderOfferReason=Clean(reason);change.Status="Đề xuất khác";change.LogicalVersion++;change.RespondedAt=Clock.UtcNow;
        }
        else if(decision=="Chấp nhận")await ApplyChange(change,booking,provider);
        else{change.Status="Từ chối";change.DecisionReason=Clean(reason);change.RespondedAt=Clock.UtcNow;}
        Notify(booking.OwnerId,"Phản hồi đề nghị #"+change.Id+": "+decision,Clean(reason),"/Marketplace/Requests");
        return await Save(decision=="Đề xuất khác"?"Đã gửi điều kiện thay thế cho khách. Thỏa thuận cũ giữ nguyên.":"Đã ghi phản hồi; lịch sử điều kiện cũ được bảo toàn.");
    });
    public Task<string> ReplyChangeOffer(int owner,int id,bool accept,string version,int logicalVersion)=>Atomic(async()=>
    {
        var change=await db.BookingChanges.Include(x=>x.Booking).SingleOrDefaultAsync(x=>x.Id==id&&x.Booking!.OwnerId==owner);
        if(change?.Booking==null)return "Không tìm thấy đề nghị của bạn.";
        if(change.LogicalVersion!=logicalVersion||!VersionMatches(db,change,version)||!ActiveChangeStates.Contains(change.Status))return "Phiên bản đề nghị đã thay đổi hoặc đã được xử lý.";
        if(accept)
        {
            if(change.Status!="Đề xuất khác"||change.Booking.ConditionsVersion!=change.BaseConditionsVersion||!await CanChange(change.Booking))return "Không thể chấp thuận điều kiện thay thế không còn hiệu lực.";
            await ApplyChange(change,change.Booking,owner);
        }
        else{change.Status="Đã rút";change.RespondedAt=Clock.UtcNow;}
        var provider=await db.Services.Where(x=>x.Id==change.Booking.TravelServiceId).Select(x=>x.OwnerId).SingleAsync();Notify(provider,accept?"Khách đã chấp thuận đề xuất điều chỉnh":"Khách đã rút đề nghị điều chỉnh",change.Reason,"/Provider/Requests");
        return await Save(accept?"Đã áp dụng đúng điều kiện hai bên thống nhất.":"Đã rút đề nghị. Thỏa thuận trước đó giữ nguyên hiệu lực.");
    });
    public static string AggregateResultStatus(IReadOnlyCollection<BookingItemResult> results,int count)
    {
        if(results.Count<count||results.Any(x=>x.Outcome=="Chưa xác định"||x.VerificationStatus=="Chờ kiểm tra"))return "Chờ kiểm tra";
        
        if(results.All(x=>x.Outcome=="Hoàn thành"))return "Hoàn thành";
        if(results.All(x=>x.Outcome is "Không thực hiện" or "Khách không đến"))return "Không thực hiện";
        return "Thực hiện một phần";
    }
    public Task<string> RecordResults(int provider,int id,string version,List<BookingResultInput> inputs)=>Atomic(async()=>
    {
        var booking=await db.Bookings.Include(x=>x.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.TravelService!.OwnerId==provider);
        if(booking==null||!ProviderRules.CanFulfillExisting(await db.Accounts.FindAsync(provider)))return "Không có quyền ghi kết quả thỏa thuận.";
        if(!new[]{"Đã xác nhận","Chờ kiểm tra","Chưa chốt"}.Contains(booking.Status)||booking.Date.Date>Clock.Today)return "Chưa đến ngày phục vụ hoặc thỏa thuận không còn hợp lệ để ghi kết quả.";
        if(!VersionMatches(db,booking,version))return "Phiên bản thỏa thuận không hợp lệ.";
        if(await db.BookingChanges.AnyAsync(x=>x.BookingId==id&&ActiveChangeStates.Contains(x.Status)))return "Cần xử lý đề nghị điều chỉnh trước khi ghi kết quả.";
        var items=Items(booking);var results=await db.BookingItemResults.Where(x=>x.BookingId==id).ToListAsync();
        var editable=items.Where(x=>!results.Any(r=>r.ItemKey==x.Key&&r.Outcome!="Chưa xác định"&&r.VerificationStatus!="Chờ kiểm tra")).ToList();
        if(inputs.Count!=editable.Count||inputs.Select(x=>x.ItemKey).Distinct().Count()!=editable.Count||inputs.Any(x=>!editable.Any(i=>i.Key==x.ItemKey)||!Outcomes.Contains(x.Outcome)))return "Phải ghi đúng một kết quả hợp lệ cho mỗi hạng mục chưa chốt; không sửa hạng mục đã chốt.";
        if(inputs.Any(x=>string.IsNullOrWhiteSpace(x.Note)||Clean(x.Note).Length>1000))return "Mỗi kết quả cần ghi chú/căn cứ (tối đa 1000 ký tự), kể cả khi chưa xác định.";
        foreach(var input in inputs)
        {
            var item=items.Single(x=>x.Key==input.ItemKey);var result=results.SingleOrDefault(x=>x.ItemKey==input.ItemKey);
            if(result==null){result=new BookingItemResult{BookingId=id,ItemKey=item.Key,ItemTitle=item.Title,RecordedAt=Clock.UtcNow};db.BookingItemResults.Add(result);results.Add(result);}
            else db.BookingResultHistories.Add(new BookingResultHistory{BookingItemResult=result,PreviousOutcome=result.Outcome,PreviousNote=result.Note,NewOutcome=input.Outcome,Evidence=Clean(input.Note),ActorId=provider});
            result.Outcome=input.Outcome;result.Note=Clean(input.Note);
            result.VerificationStatus=input.Outcome=="Chưa xác định"?"Chờ kiểm tra":string.IsNullOrEmpty(result.Feedback)?"Đã ghi nhận":"Đã kiểm tra";
            result.CheckedAt=result.VerificationStatus=="Đã kiểm tra"?Clock.UtcNow:null;
        }
        booking.Status=AggregateResultStatus(results,items.Count);booking.Result="Kết quả theo hạng mục: "+booking.Status;booking.RespondedAt=Clock.UtcNow;
        Notify(booking.OwnerId,"Kết quả dịch vụ #"+booking.Id+": "+booking.Status,"Xem kết quả từng hạng mục và phản ánh nếu có sai khác.","/Marketplace/Requests");
        return await Save("Đã lưu kết quả từng hạng mục. Việc khách chưa phản hồi không đồng nghĩa đã xác nhận kết quả.");
    });
    public Task<string> FeedbackItem(int owner,int id,string? feedback,string version)=>Atomic(async()=>
    {
        var result=await db.BookingItemResults.Include(x=>x.Booking).ThenInclude(x=>x!.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.Booking!.OwnerId==owner);
        if(result?.Booking==null)return "Không tìm thấy hạng mục thuộc thỏa thuận của bạn.";
        if(!string.IsNullOrEmpty(result.Feedback))return "Hạng mục đã có phản ánh; không ghi đè phản ánh cũ.";
        // 4.9.14: thirty calendar days by default, configurable through HanPhanAnhNgay.
        if(result.RecordedAt.AddHours(7).Date.AddDays(FeedbackWindowDays)<Clock.Today)return $"Đã hết hạn phản ánh {FeedbackWindowDays} ngày kể từ lần ghi nhận kết quả.";
        if(string.IsNullOrWhiteSpace(feedback)||Clean(feedback).Length>2000||!VersionMatches(db,result,version))return "Nội dung hoặc phiên bản phản ánh không hợp lệ.";
        result.Feedback=Clean(feedback);result.FeedbackAt=Clock.UtcNow;result.VerificationStatus="Chờ kiểm tra";result.CheckedAt=null;
        var booking=result.Booking;booking.Status="Chờ kiểm tra";booking.Result="Kết quả theo hạng mục: Chờ kiểm tra";booking.Feedback="Có phản ánh theo hạng mục cần đối chiếu.";
        Notify(booking.TravelService!.OwnerId,"Phản ánh hạng mục của yêu cầu #"+booking.Id,result.Feedback,"/Provider/Requests");
        return await Save("Đã lưu phản ánh riêng và chuyển hạng mục sang Chờ kiểm tra. Kết quả thực hiện ban đầu được giữ nguyên.");
    });
}

