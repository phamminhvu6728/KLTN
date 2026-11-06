using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration.Test;

internal static class IT_Common
{
    public static HttpClient Client(IntegrationTestFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    public static string Token(string html)
    {
        var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!m.Success) m = Regex.Match(html, "value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");
        if (!m.Success) throw new InvalidOperationException("Khong tim thay antiforgery token tren HTML");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }
    public static async Task<HttpResponseMessage> Login(HttpClient client, string email)
    {
        var html = await client.GetStringAsync("/Account/Login");
        return await client.PostAsync("/Account/Login", Form(new(){["Email"]=email,["Password"]="Demo12345",["__RequestVerificationToken"]=Token(html)}));
    }
    public static FormUrlEncodedContent Form(Dictionary<string,string> data) => new(data);
    public static void Check(List<string> facts,List<string> errors,bool good,string what,string observed)
    {
        facts.Add($"{what}={observed}");
        if (!good) errors.Add($"{what}: {observed}");
    }
    public static void Finish(string id,string title,string pre,string input,string expected,List<string> facts,List<string> errors)
    {
        IntegrationTestReporter.Save(id,title,pre,input,expected,string.Join("; ",facts),string.Join(" | ",errors));
        Xunit.Assert.True(errors.Count==0,string.Join(Environment.NewLine,errors));
    }
}
