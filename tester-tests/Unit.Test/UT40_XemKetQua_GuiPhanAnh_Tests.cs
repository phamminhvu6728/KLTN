using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT40_XemKetQua_GuiPhanAnh_Tests
{
    [Fact(DisplayName = "UT-40a | KT1 | Ket qua khach khac")]
    public Task UT40a_Test() => UT383940.Check("UT-40a", "KT1", "Khách khác không được phản ánh",
        "KH02 phản ánh kết quả của KH01", "Lỗi 0901-01; không có phản ánh", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            var result = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            var r = await m.FeedbackItem(UT293031.KhachKhac, result.Id, "Không đúng", UT232425.Version);
            var after = await db.BookingItemResults.SingleAsync(x => x.Id == result.Id);
            observe($"Return={r}; Feedback={after.Feedback}");
            Assert.True(string.IsNullOrEmpty(after.Feedback)); Assert.Contains("0901-01", r);
        }
    });

    [Fact(DisplayName = "UT-40b | KT2 bien | 04/10 con han")]
    public Task UT40b_Test() => UT383940.Check("UT-40b", "KT2 (biên)", "Ngày 04/10 còn hạn phản ánh",
        "HomNay=03/11; RecordedAt=04/10/2026 giờ Việt Nam", "Còn hạn; nút Phản ánh hiển thị", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành", recordedAt:
                new DateTime(2026, 10, 3, 17, 0, 0, DateTimeKind.Utc));
            var result = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            var r = await m.FeedbackItem(UT293031.Khach, result.Id, "Dịch vụ không đúng mô tả", UT232425.Version);
            var after = await db.BookingItemResults.SingleAsync(x => x.Id == result.Id);
            observe($"Return={r}; Feedback={after.Feedback}; WindowDays={m.FeedbackWindowDays}; ViewButton=chưa kiểm tra");
            Assert.Equal("Dịch vụ không đúng mô tả", after.Feedback);
            Assert.Fail("CHƯA XÁC MINH ĐỦ: Đã kiểm tra cho phép phản ánh ở biên 04/10, nhưng chưa kiểm tra nút Phản ánh trên Razor View.");
        }
    });

    [Fact(DisplayName = "UT-40c | KT2 bien | 03/10 het han")]
    public Task UT40c_Test() => UT383940.Check("UT-40c", "KT2 (biên)", "Ngày 03/10 hết hạn phản ánh",
        "HomNay=03/11; RecordedAt=03/10/2026 giờ Việt Nam", "Không có nút; gửi trả 0901-07", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành", recordedAt:
                new DateTime(2026, 10, 2, 17, 0, 0, DateTimeKind.Utc));
            var result = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            var r = await m.FeedbackItem(UT293031.Khach, result.Id, "Gửi quá hạn", UT232425.Version);
            var after = await db.BookingItemResults.SingleAsync(x => x.Id == result.Id);
            observe($"Return={r}; Feedback={after.Feedback}; ViewButton=chưa kiểm tra");
            Assert.True(string.IsNullOrEmpty(after.Feedback));
            Assert.Contains("0901-07", r);
            Assert.Fail("CHƯA XÁC MINH ĐỦ: Chưa kiểm tra nút Phản ánh bị ẩn trong Razor View.");
        }
    });

    [Fact(DisplayName = "UT-40d | KT2 | Da co phan anh")]
    public Task UT40d_Test() => UT383940.Check("UT-40d", "KT2", "Không được ghi đè phản ánh đã có",
        "Feedback=Phản ánh cũ; gửi phản ánh mới", "Lỗi 0901-07; nội dung cũ giữ nguyên", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành", feedback: "Phản ánh cũ");
            var result = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            var r = await m.FeedbackItem(UT293031.Khach, result.Id, "Phản ánh mới", UT232425.Version);
            var after = await db.BookingItemResults.SingleAsync(x => x.Id == result.Id);
            observe($"Return={r}; Feedback={after.Feedback}");
            Assert.Equal("Phản ánh cũ", after.Feedback); Assert.Contains("0901-07", r);
        }
    });

    [Fact(DisplayName = "UT-40e | KT3 | Phien ban ket qua cu")]
    public Task UT40e_Test() => UT383940.Check("UT-40e", "KT3", "Phiên bản kết quả cũ",
        "Version=INVALID", "Lỗi 0901-04; không ghi phản ánh", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            var result = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            var r = await m.FeedbackItem(UT293031.Khach, result.Id, "Sai dịch vụ", "INVALID");
            var after = await db.BookingItemResults.SingleAsync(x => x.Id == result.Id);
            observe($"Return={r}; Feedback={after.Feedback}");
            Assert.True(string.IsNullOrEmpty(after.Feedback)); Assert.Contains("0901-04", r);
        }
    });

    [Fact(DisplayName = "UT-40f | XL1 XL3 | Phan anh hang muc DayDu")]
    public Task UT40f_Test() => UT383940.Check("UT-40f", "XL1–XL3", "Phản ánh hạng mục đã ghi đầy đủ",
        "main=Hoàn thành; chưa phản ánh; KH01 gửi nội dung", "Lưu nội dung + giờ; ChoKiemTra; giữ DayDu; booking ChoKiemTra; thông báo NCC", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            var row = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            row.CheckedAt = new DateTime(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
            var r = await m.FeedbackItem(UT293031.Khach, row.Id, "Phòng thực tế khác mô tả", UT232425.Version);
            var updated = await db.BookingItemResults.SingleAsync(x => x.Id == row.Id);
            var booking = await db.Bookings.SingleAsync(x => x.Id == UT383940.BookingId);
            var notices = await db.Notifications.Where(x => x.OwnerId == UT293031.NhaCungCap).ToListAsync();
            observe($"Return={r}; Feedback={updated.Feedback}; FeedbackAt={updated.FeedbackAt:O}; " +
                $"Status={updated.VerificationStatus}; CheckedAt={updated.CheckedAt:O}; Outcome={updated.Outcome}; " +
                $"BookingStatus={booking.Status}; Notifications={string.Join("|", notices.Select(x => x.Title))}");
            Assert.Equal("Phòng thực tế khác mô tả", updated.Feedback);
            Assert.NotNull(updated.FeedbackAt);
            Assert.Equal(new DateTime(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc), updated.FeedbackAt.Value);
            Assert.Equal("Chờ kiểm tra", updated.VerificationStatus);
            Assert.Null(updated.CheckedAt); Assert.Equal("Hoàn thành", updated.Outcome);
            Assert.Equal("Chờ kiểm tra", booking.Status);
            Assert.Contains(notices, x => x.Title.Contains("PhanAnhKetQua", StringComparison.OrdinalIgnoreCase));
        }
    });
}
