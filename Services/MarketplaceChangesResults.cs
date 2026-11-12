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
        if(booking==null)return ThongDiep.Loi("0801-01");
        if(!await CanChange(booking))return ThongDiep.Loi("0801-02");
        if(!VersionMatches(db,booking,terms.Version)||booking.ConditionsVersion!=terms.ConditionsVersion)return ThongDiep.Loi("0801-03");
        if(await db.BookingChanges.AnyAsync(x=>x.BookingId==id&&ActiveChangeStates.Contains(x.Status)))return ThongDiep.Loi("0801-04");
        if(kind!="Điều chỉnh"&&kind!="Hủy")return "Loại đề nghị không hợp lệ.";
        if(Clean(terms.Reason).Length>500)return "Lý do đề nghị tối đa 500 ký tự.";
        if(string.IsNullOrWhiteSpace(terms.Reason))return "Phải nhập lý do thay đổi hoặc hủy.";
        if(kind=="Điều chỉnh")
        {
            var invalid=ValidateTerms(terms);if(invalid.Length>0)return ThongDiep.Loi("0801-05")+" "+invalid;
            if(booking.Date.Date==terms.Date.Date&&booking.EndDate?.Date==terms.EndDate?.Date&&booking.People==terms.People&&booking.Guests==terms.Guests&&booking.Total==terms.Total&&booking.ArrivalDeparture==Clean(terms.ArrivalDeparture)&&booking.Options==Clean(terms.Options))return ThongDiep.Loi("0801-05")+" Cần ít nhất một điều kiện khác thỏa thuận hiện tại.";
        }
        await EnsureSnapshot(booking,owner);
        var proposal=new BookingChange{BookingId=id,RequesterId=owner,Kind=kind,BaseConditionsVersion=booking.ConditionsVersion,OldConditions=Snapshot(booking),Reason=Clean(terms.Reason)};
        CopyChange(proposal,kind=="Hủy"?new BookingTermsInput{Date=booking.Date,EndDate=booking.EndDate,People=booking.People,Guests=booking.Guests,Total=booking.Total,Options=booking.Options,ArrivalDeparture=booking.ArrivalDeparture}:terms);
        db.BookingChanges.Add(proposal);
        // Force parent rowversion change so a concurrent service-result write cannot slip past the eligibility read.
        booking.RespondedAt=Clock.UtcNow;
        var providerId=await db.Services.Where(x=>x.Id==booking.TravelServiceId).Select(x=>x.OwnerId).SingleAsync();ThongBaoLoai.Them(db,providerId,"DeNghiDieuChinhMoi",proposal.Reason,ThongBaoLoai.YeuCauNhaCungCap(id));
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
        if(change?.Booking==null)return ThongDiep.Loi("0801-01");
        var booking=change.Booking;
        if(!ProviderRules.CanFulfillExisting(await db.Accounts.FindAsync(provider)))return "Tài khoản không được xử lý thỏa thuận này.";
        if(change.Status!="Chờ phản hồi"||change.LogicalVersion!=logicalVersion||booking.ConditionsVersion!=change.BaseConditionsVersion||!await CanChange(booking)||!VersionMatches(db,change,version))return ThongDiep.Loi("0801-06");
        if(!new[]{"Chấp nhận","Từ chối","Đề xuất khác"}.Contains(decision))return "Phản hồi không hợp lệ.";
        if(Clean(reason).Length>(decision=="Đề xuất khác"?2000:500))return "Lý do phản hồi vượt quá giới hạn ký tự.";
        if(decision=="Từ chối"&&string.IsNullOrWhiteSpace(reason))return ThongDiep.Loi("0801-07");
        if(decision=="Đề xuất khác")
        {
            if(change.Kind!="Điều chỉnh"||alternative==null||string.IsNullOrWhiteSpace(reason))return "Chỉ đề xuất điều kiện khác cho đề nghị thay đổi, kèm lời giải thích.";
            var invalid=ValidateTerms(alternative);if(invalid.Length>0)return ThongDiep.Loi("0801-05")+" "+invalid;
            CopyChange(change,alternative);change.ProviderOfferReason=Clean(reason);change.Status="Đề xuất khác";change.LogicalVersion++;change.RespondedAt=Clock.UtcNow;
        }
        else if(decision=="Chấp nhận")await ApplyChange(change,booking,provider);
        else{change.Status="Từ chối";change.RespondedAt=Clock.UtcNow;}
        change.DecisionReason=Clean(reason);
        ThongBaoLoai.Them(db,booking.OwnerId,decision switch{"Chấp nhận"=>"DeNghiDieuChinhDuocChapNhan","Từ chối"=>"DeNghiDieuChinhBiTuChoi",_=>"DeNghiDieuChinhCoDeXuatKhac"},Clean(reason),ThongBaoLoai.YeuCauKhach(booking.Id));
        return await Save(decision=="Đề xuất khác"?"Đã gửi điều kiện thay thế cho khách. Thỏa thuận cũ giữ nguyên.":"Đã ghi phản hồi; lịch sử điều kiện cũ được bảo toàn.");
    });
    public Task<string> ReplyChangeOffer(int owner,int id,bool accept,string version,int logicalVersion)=>Atomic(async()=>
    {
        var change=await db.BookingChanges.Include(x=>x.Booking).SingleOrDefaultAsync(x=>x.Id==id&&x.Booking!.OwnerId==owner);
        if(change?.Booking==null)return ThongDiep.Loi("0801-01");
        if(change.LogicalVersion!=logicalVersion||!VersionMatches(db,change,version)||!ActiveChangeStates.Contains(change.Status))return ThongDiep.Loi("0801-06");
        if(accept)
        {
            if(change.Status!="Đề xuất khác"||change.Booking.ConditionsVersion!=change.BaseConditionsVersion||!await CanChange(change.Booking))return ThongDiep.Loi("0801-06");
            await ApplyChange(change,change.Booking,owner);
        }
        else{change.Status="Đã rút";change.RespondedAt??=Clock.UtcNow;}
        var provider=await db.Services.Where(x=>x.Id==change.Booking.TravelServiceId).Select(x=>x.OwnerId).SingleAsync();ThongBaoLoai.Them(db,provider,accept?"DeNghiDieuChinhDuocChapNhan":"YeuCauDaRut",change.Reason,ThongBaoLoai.YeuCauNhaCungCap(change.BookingId));
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
        if(booking==null||!ProviderRules.CanFulfillExisting(await db.Accounts.FindAsync(provider)))return ThongDiep.Loi("0901-01");
        if(!new[]{"Đã xác nhận","Chờ kiểm tra","Chưa chốt"}.Contains(booking.Status))return ThongDiep.Loi("0901-02");if(booking.Date.Date>Clock.Today)return ThongDiep.Loi("0901-03");
        if(!VersionMatches(db,booking,version))return ThongDiep.Loi("0901-04");
        if(await db.BookingChanges.AnyAsync(x=>x.BookingId==id&&ActiveChangeStates.Contains(x.Status)))return "Cần xử lý đề nghị điều chỉnh trước khi ghi kết quả.";
        var items=Items(booking);var results=await db.BookingItemResults.Where(x=>x.BookingId==id).ToListAsync();
        var editable=items.Where(x=>!results.Any(r=>r.ItemKey==x.Key&&r.Outcome!="Chưa xác định"&&r.VerificationStatus!="Chờ kiểm tra")).ToList();
        if(inputs.Any(x=>items.Any(i=>i.Key==x.ItemKey)&&!editable.Any(i=>i.Key==x.ItemKey)))return ThongDiep.Loi("0901-06");
        if(inputs.Count!=editable.Count||inputs.Select(x=>x.ItemKey).Distinct().Count()!=editable.Count||inputs.Any(x=>!editable.Any(i=>i.Key==x.ItemKey)||!Outcomes.Contains(x.Outcome)))return ThongDiep.Loi("0901-05");
        if(inputs.Any(x=>string.IsNullOrWhiteSpace(x.Note)||Clean(x.Note).Length>1000))return ThongDiep.Loi("0901-05")+" Mỗi kết quả cần ghi chú/căn cứ, tối đa 1000 ký tự.";
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
        ThongBaoLoai.Them(db,booking.OwnerId,"KetQuaDaGhiNhan","Kết quả: "+booking.Status,"/yeu-cau/"+booking.Id+"/ket-qua/xem-ket-qua");
        return await Save("Đã lưu kết quả từng hạng mục. Việc khách chưa phản hồi không đồng nghĩa đã xác nhận kết quả.");
    });
    public Task<string> FeedbackItem(int owner,int id,string? feedback,string version)=>Atomic(async()=>
    {
        var result=await db.BookingItemResults.Include(x=>x.Booking).ThenInclude(x=>x!.TravelService).SingleOrDefaultAsync(x=>x.Id==id&&x.Booking!.OwnerId==owner);
        if(result?.Booking==null)return ThongDiep.Loi("0901-01");
        if(!string.IsNullOrEmpty(result.Feedback))return ThongDiep.Loi("0901-07");
        // 4.9.14: thirty calendar days by default, configurable through HanPhanAnhNgay.
        if(result.RecordedAt.AddHours(7).Date.AddDays(FeedbackWindowDays)<Clock.Today)return ThongDiep.Loi("0901-07");
        if(!VersionMatches(db,result,version))return ThongDiep.Loi("0901-04");if(string.IsNullOrWhiteSpace(feedback)||Clean(feedback).Length>2000)return "Nội dung phản ánh không hợp lệ (tối đa 2000 ký tự).";
        result.Feedback=Clean(feedback);result.FeedbackAt=Clock.UtcNow;result.VerificationStatus="Chờ kiểm tra";result.CheckedAt=null;
        var booking=result.Booking;booking.Status="Chờ kiểm tra";booking.Result="Kết quả theo hạng mục: Chờ kiểm tra";booking.Feedback="Có phản ánh theo hạng mục cần đối chiếu.";
        ThongBaoLoai.Them(db,booking.TravelService!.OwnerId,"PhanAnhKetQua",result.Feedback,"/yeu-cau/"+booking.Id+"/ket-qua/mo-ghi-nhan");
        return await Save("Đã lưu phản ánh riêng và chuyển hạng mục sang Chờ kiểm tra. Kết quả thực hiện ban đầu được giữ nguyên.");
    });
}

