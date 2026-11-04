
using Xunit.Abstractions;

namespace TuTuc.Tests.Unit;

public static class TestReporter
{
    public static void Print(
        ITestOutputHelper output,
        string ma,
        string ten,
        string buoc,
        string dauVao,
        string kyVong,
        string thucTe,
        bool dat)
    {
        output.WriteLine(
            $"[{(dat ? "PASS" : "FAIL")}] {ma} - {ten}");

        output.WriteLine($"  Buoc: {buoc}");
        output.WriteLine($"  Dau vao: {dauVao}");
        output.WriteLine($"  Ky vong: {kyVong}");
        output.WriteLine($"  Thuc te: {thucTe}");
    }
}

