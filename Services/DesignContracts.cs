namespace TuTuc.Services;

public interface IDongHo
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}

public sealed class DongHoHeThong : IDongHo
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => UtcNow.AddHours(7).Date;
}

public sealed class BusinessFault(string code, int statusCode, string message) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public record BookingPreviewOutcome(TuTuc.Models.BookingPreviewVm? Preview, string Error, int StatusCode = 400, string Code = "0701-03");
