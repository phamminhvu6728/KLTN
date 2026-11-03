namespace TuTuc.Models;

public static class MoneyDisplay
{
    public static string ToString(this decimal? amount, string format) => amount?.ToString(format) ?? "Chưa xác định";
    public static string ToString(this decimal? amount, string format, IFormatProvider provider) => amount?.ToString(format, provider) ?? "";
}
