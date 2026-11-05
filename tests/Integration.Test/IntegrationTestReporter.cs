using System.Text.Json;



namespace Integration.Test;

public static class IntegrationTestReporter
{
    private static readonly object Sync = new();

    public static void Save(string id, string title, string precondition,
        string input, string expected, string actual, string problem)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "TestResults");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "IntegrationTest_Details.jsonl");
        var record = new { id, title, precondition, input, expected, actual, problem };
        lock (Sync)
            File.AppendAllText(path, JsonSerializer.Serialize(record) + Environment.NewLine);
    }
}
