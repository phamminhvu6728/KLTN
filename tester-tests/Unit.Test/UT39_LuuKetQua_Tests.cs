using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TuTuc.Models;
using Xunit;

namespace TuTuc.Tests.Unit;

public sealed class UT39_LuuKetQua_Tests
{
    [Fact(DisplayName = "UT-39a | KT1 | RowVersion cu")]
    public Task UT39a_Test() => UT383940.Check("UT-39a", "KT1", "Phiên bản thỏa thuận đã cũ",
        "Version=INVALID", "Lỗi 0901-04; không ghi", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId, "INVALID", UT383940.Inputs());
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={r}; ResultRows={count}");
            Assert.Equal(0, count); Assert.Contains("0901-04", r);
        }
    });

    [Fact(DisplayName = "UT-39b | KT2 | Thieu hang muc chua chot")]
    public Task UT39b_Test() => UT383940.Check("UT-39b", "KT2", "Thiếu một hạng mục phải ghi",
        "Inputs=chỉ main, thiếu option-1", "Lỗi 0901-05; không ghi", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var inputs = UT383940.Inputs().Take(1).ToList();
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId, UT232425.Version, inputs);
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={r}; ResultRows={count}");
            Assert.Equal(0, count); Assert.Contains("0901-05", r);
        }
    });

    [Fact(DisplayName = "UT-39c | KT2 | Hang muc khong thuoc yeu cau")]
    public Task UT39c_Test() => UT383940.Check("UT-39c", "KT2", "Hạng mục lạ không thuộc yêu cầu",
        "main; option-1; option-99", "Lỗi 0901-05; không ghi", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var inputs = UT383940.Inputs();
            inputs.Add(new BookingResultInput { ItemKey = "option-99", Outcome = "Hoàn thành", Note = "Hạng mục ngoài yêu cầu" });
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId, UT232425.Version, inputs);
            var count = await db.BookingItemResults.CountAsync();
            observe($"Return={r}; ResultRows={count}");
            Assert.Equal(0, count); Assert.Contains("0901-05", r);
        }
    });

    [Fact(DisplayName = "UT-39d | KT3 | Cam sua ket qua da chot")]
    public Task UT39d_Test() => UT383940.Check("UT-39d", "KT3", "Không được sửa hạng mục đã chốt",
        "main=Hoàn thành/Đã ghi nhận; gửi lại main=Thực hiện một phần", "Lỗi 0901-06; giữ nguyên kết quả", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId, UT232425.Version,
                UT383940.Inputs("Thực hiện một phần", "Hoàn thành"));
            var saved = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Main);
            observe($"Return={r}; main.Outcome={saved.Outcome}");
            Assert.Equal("Hoàn thành", saved.Outcome); Assert.Contains("0901-06", r);
        }
    });

    [Fact(DisplayName = "UT-39e | XL1 | Moi: ChuaXacDinh")]
    public Task UT39e_Test() => UT383940.Check("UT-39e", "XL1", "Hạng mục mới chưa xác định",
        "main=Hoàn thành; option-1=Chưa xác định", "option-1.TrangThaiKiemTra=ChoKiemTra", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs());
            var row = await db.BookingItemResults.SingleOrDefaultAsync(x => x.ItemKey == UT383940.Extra);
            observe($"Return={r}; Outcome={row?.Outcome}; VerificationStatus={row?.VerificationStatus}");
            Assert.NotNull(row); Assert.Equal("Chưa xác định", row.Outcome); Assert.Equal("Chờ kiểm tra", row.VerificationStatus);
        }
    });

    [Fact(DisplayName = "UT-39f | XL1 | Moi: DayDu")]
    public Task UT39f_Test() => UT383940.Check("UT-39f", "XL1", "Hạng mục mới đầy đủ",
        "main=Hoàn thành; option-1=Hoàn thành", "option-1.TrangThaiKiemTra=DaGhiNhan", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs("Hoàn thành", "Hoàn thành"));
            var row = await db.BookingItemResults.SingleOrDefaultAsync(x => x.ItemKey == UT383940.Extra);
            observe($"Return={r}; Outcome={row?.Outcome}; VerificationStatus={row?.VerificationStatus}");
            Assert.NotNull(row); Assert.Equal("Hoàn thành", row.Outcome); Assert.Equal("Đã ghi nhận", row.VerificationStatus);
        }
    });

    [Fact(DisplayName = "UT-39g | XL2 | Co phan anh: doi sang MotPhan")]
    public Task UT39g_Test() => UT383940.Check("UT-39g", "XL2", "Đổi kết quả trên dòng có phản ánh",
        "option-1=Chưa xác định/Chờ kiểm tra; Feedback=Khác thực tế", "Đã kiểm tra; CheckedAt=BayGioUtc", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            await UT383940.AddResult(db, UT383940.Extra, "Chưa xác định", "Chờ kiểm tra", "Khác thực tế");
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, new() { new BookingResultInput {
                    ItemKey = UT383940.Extra, Outcome = "Thực hiện một phần", Note = "Đối chiếu nội dung phản ánh" } });
            var row = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Extra);
            observe($"Return={r}; Outcome={row.Outcome}; Status={row.VerificationStatus}; CheckedAt={row.CheckedAt:O}");
            Assert.Equal("Thực hiện một phần", row.Outcome);
            Assert.Equal("Đã kiểm tra", row.VerificationStatus); Assert.NotNull(row.CheckedAt);
            Assert.Equal(new DateTime(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc), row.CheckedAt.Value);
        }
    });

    [Fact(DisplayName = "UT-39h | XL2 | Khong phan anh: doi sang DayDu")]
    public Task UT39h_Test() => UT383940.Check("UT-39h", "XL2", "Đổi dòng chưa xác định không có phản ánh",
        "option-1=Chưa xác định/Chờ kiểm tra; Feedback=rỗng", "Đã ghi nhận; CheckedAt=NULL", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            await UT383940.AddResult(db, UT383940.Main, "Hoàn thành");
            await UT383940.AddResult(db, UT383940.Extra, "Chưa xác định", "Chờ kiểm tra");
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, new() { new BookingResultInput {
                    ItemKey = UT383940.Extra, Outcome = "Hoàn thành", Note = "Đã hoàn thành" } });
            var row = await db.BookingItemResults.SingleAsync(x => x.ItemKey == UT383940.Extra);
            observe($"Return={r}; Outcome={row.Outcome}; Status={row.VerificationStatus}; CheckedAt={row.CheckedAt:O}");
            Assert.Equal("Hoàn thành", row.Outcome);
            Assert.Equal("Đã ghi nhận", row.VerificationStatus); Assert.Null(row.CheckedAt);
        }
    });

    [Fact(DisplayName = "UT-39i | XL3 XL4 | Tong hop va thong bao")]
    public Task UT39i_Test() => UT383940.Check("UT-39i", "XL3, XL4", "Cập nhật tổng hợp và thông báo cho khách",
        "Hai hạng mục hoàn thành", "TrangThai=TongHop (theo UT-64); thông báo KetQuaDaGhiNhan cho KH01", async observe =>
    {
        var (db, m) = await UT383940.Setup(); await using (db)
        {
            var r = await m.RecordResults(UT293031.NhaCungCap, UT383940.BookingId,
                UT232425.Version, UT383940.Inputs("Hoàn thành", "Hoàn thành"));
            var booking = await db.Bookings.SingleAsync(x => x.Id == UT383940.BookingId);
            var notices = await db.Notifications.Where(x => x.OwnerId == UT293031.Khach).ToListAsync();
            observe($"Return={r}; Status={booking.Status}; Notices={string.Join(" | ", notices.Select(x => x.Title))}");
            Assert.Equal("Hoàn thành", booking.Status);
            Assert.Contains(notices, x => x.Title.Contains("KetQuaDaGhiNhan", StringComparison.OrdinalIgnoreCase));
        }
    });
}
