
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Integration.Test;

public class IntegrationTestFactory
    : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } =
        "TuTuc_IT_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        var root = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../"
            )
        );

        builder.UseContentRoot(root);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] =
                        $"Server=(localdb)\\MSSQLLocalDB;" +
                        $"Database={DatabaseName};" +
                        "Trusted_Connection=True;" +
                        "TrustServerCertificate=True",

                    ["Demo:Enabled"] = "true",

                    ["Storage:UploadDirectory"] =
                        Path.Combine(
                            root,
                            "TestResults",
                            DatabaseName,
                            "UploadFiles"
                        )
                }
            );
        });
    }
}
