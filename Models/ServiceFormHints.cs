namespace TuTuc.Models;

// Nhãn và ví dụ của form gửi yêu cầu, đổi theo loại dịch vụ
// (đặt xe thì hỏi giờ đón, ăn uống thì hỏi giờ đến quán, không hỏi giờ nhận phòng).
// Ô ngày kết thúc luôn không bắt buộc nên nhãn để ngắn, tránh xuống dòng làm lệch hai ô ngày.
public record ServiceFormHints(string DateLabel, string EndDateLabel, string QuantityLabel, string TimeLabel, string TimeExample, string OptionsExample)
{
    // Đơn vị của giá tham khảo, khớp với ô số lượng của form (Lưu trú: giá mỗi phòng mỗi đêm).
    public static string Unit(string? category) => category switch
    {
        "Lưu trú" => "phòng / đêm",
        "Ăn uống" => "suất",
        "Di chuyển" => "lượt",
        "Trải nghiệm" => "vé",
        _ => "đơn vị",
    };

    public static ServiceFormHints For(string? category) => category switch
    {
        "Lưu trú" => new("Ngày nhận phòng", "Ngày trả phòng", "Số phòng",
            "Giờ nhận / trả phòng mong muốn", "Ví dụ: nhận phòng 14:00, trả phòng 11:00", "Ví dụ: thêm giường phụ\nBữa sáng cho 2 người"),
        "Ăn uống" => new("Ngày dùng bữa", "Đến ngày", "Số suất",
            "Giờ đến quán", "Ví dụ: 18:30", "Ví dụ: bàn gần cửa sổ\nThêm 1 suất trẻ em"),
        "Di chuyển" => new("Ngày đi", "Ngày về / trả xe", "Số lượt (xe hoặc vé)",
            "Giờ và điểm đón", "Ví dụ: đón 7:00 tại khách sạn, trả ở sân bay", "Ví dụ: thêm 1 mũ bảo hiểm\nGhế cho trẻ em"),
        "Trải nghiệm" => new("Ngày tham gia", "Ngày kết thúc", "Số vé",
            "Giờ bắt đầu mong muốn", "Ví dụ: bắt đầu 8:00, gặp tại sảnh khách sạn", "Ví dụ: hướng dẫn viên nói tiếng Anh\nĐưa đón tại khách sạn"),
        _ => new("Ngày bắt đầu", "Ngày kết thúc", "Số lượng (đơn vị dịch vụ)",
            "Thời gian mong muốn", "Ví dụ: 8:00", "Mỗi dòng một hạng mục"),
    };
}
