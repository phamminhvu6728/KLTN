
using System.Net;
using Xunit;
namespace Integration.Test;

public class IT00_KiemTraMoiTruong_Tests
{
    [Fact]
    public async Task IT00_Web_KetNoi_SQL_ThanhCong()
    {
        // Khởi chạy website với database kiểm thử riêng
        using var factory = new IntegrationTestFactory();

        // Tạo HTTP Client để gửi yêu cầu
        using var client = factory.CreateClient();

        // Gọi endpoint kiểm tra trạng thái
        var response = await client.GetAsync("/health");

        // Xác nhận ứng dụng trả HTTP 200
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Kiểm tra nội dung phản hồi
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("ready", content);

        // Chỉ hiển thị khi chạy chi tiết
        Console.WriteLine(
            $"[PASS] IT00 - Website ket noi SQL thanh cong"
        );

        Console.WriteLine(
            $"Database: {factory.DatabaseName}"
        );
    }
}
