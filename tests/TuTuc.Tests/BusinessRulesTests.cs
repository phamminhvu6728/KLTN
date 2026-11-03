using TuTuc.Models;
using TuTuc.Services;

namespace TuTuc.Tests;

public class BusinessRulesTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData(" 090-123-4567 ", "0901234567")]
    [InlineData("+84 (90) 123 4567", "0901234567")]
    [InlineData("0084901234567", "0901234567")]
    [InlineData("0901234567", "0901234567")]
    public void PhoneNormalization(string? raw, string expected) => Assert.Equal(expected, AccountManager.NormalizePhone(raw));

    public static IEnumerable<object[]> ProviderCases()
    {
        foreach (var active in new[] { true, false })
        foreach (var role in new[] { "Provider", "Traveler", "Admin" })
        foreach (var verification in new[] { "Đã xác minh", "Chờ xác minh", "Đã thu hồi" })
        foreach (var operation in new[] { "Đang hoạt động", "Tạm đình chỉ", "Đã thu hồi" })
            yield return new object[] { active, role, verification, operation };
    }
    [Theory, MemberData(nameof(ProviderCases))]
    public void ProviderPermissions(bool active, string role, string verification, string operation)
    {
        var p = new Account { IsActive = active, Role = role, VerificationStatus = verification, ProviderStatus = operation };
        Assert.Equal(active && role == "Provider" && verification == "Đã xác minh" && operation == "Đang hoạt động", ProviderRules.CanPublish(p));
        Assert.Equal(active && role == "Provider", ProviderRules.CanFulfillExisting(p));
        foreach (var approved in new[] { true, false })
        foreach (var published in new[] { true, false })
        foreach (var accepting in new[] { true, false })
        {
            var s = new TravelService { ApprovalStatus = approved ? "Đã duyệt" : "Chờ duyệt", IsPublished = published, IsAccepting = accepting };
            Assert.Equal(approved && published && operation != "Đã thu hồi", ProviderRules.IsPublic(s, p));
            Assert.Equal(approved && published && accepting && active && role == "Provider" && verification == "Đã xác minh" && operation == "Đang hoạt động", ProviderRules.CanReceiveNew(s, p));
        }
    }
    [Fact] public void NullProviderDenied() { Assert.False(ProviderRules.CanPublish(null)); Assert.False(ProviderRules.CanFulfillExisting(null)); }

    [Theory]
    [InlineData("past")][InlineData("end")][InlineData("quantity-low")][InlineData("quantity-high")]
    [InlineData("guests-low")][InlineData("guests-high")][InlineData("price-low")][InlineData("price-high")]
    [InlineData("duplicate")][InlineData("too-many")][InlineData("option-long")][InlineData("options-long")]
    [InlineData("note-long")][InlineData("arrival-long")]
    public void InvalidBookingTerms(string kind)
    {
        var x = new BookingTermsInput { Date = MarketplaceManager.Today.AddDays(1) };
        switch (kind)
        {
            case "past": x.Date = MarketplaceManager.Today.AddDays(-1); break;
            case "end": x.EndDate = x.Date.AddDays(-1); break;
            case "quantity-low": x.People = 0; break;
            case "quantity-high": x.People = 101; break;
            case "guests-low": x.Guests = 0; break;
            case "guests-high": x.Guests = 101; break;
            case "price-low": x.Total = -1; break;
            case "price-high": x.Total = 1000000001; break;
            case "duplicate": x.Options = "Breakfast\nbreakfast"; break;
            case "too-many": x.Options = string.Join('\n', Enumerable.Range(0,11)); break;
            case "option-long": x.Options = new string('x',151); break;
            case "options-long": x.Options = string.Join('\n',Enumerable.Range(0,10).Select(i=>i+new string('x',110))); break;
            case "note-long": x.Note = new string('x',1001); break;
            case "arrival-long": x.ArrivalDeparture = new string('x',101); break;
        }
        Assert.NotEmpty(MarketplaceManager.ValidateTerms(x));
    }
    [Theory][InlineData(1,0)][InlineData(100,1000000000)]
    public void BookingBoundaryValuesAccepted(int quantity, decimal total) => Assert.Empty(MarketplaceManager.ValidateTerms(new() { Date=MarketplaceManager.Today, People=quantity, Guests=quantity, Total=total, Options="Breakfast\nTransport" }));

    [Theory]
    [InlineData("name")][InlineData("location")][InlineData("category")][InlineData("price")]
    [InlineData("image-http")][InlineData("image-traversal")][InlineData("closure-missing")][InlineData("closure-end")]
    public void ServiceValidation(string kind)
    {
        var x=new ServiceEditVm {Name="Tour",Location="Đà Lạt",Price=10};
        switch(kind) {
            case "name": x.Name=" ";break;
            case "location":x.Location="";break;
            case "category":x.Category="Unknown";break;
            case "price":x.Price=-1;break;
            case "image-http":x.Image="http://example.com/a.png";break;
            case "image-traversal":x.Image="/images/../a.png";break;
            case "closure-missing":x.ClosureReason="Closed";break;
            case "closure-end":x.ClosureStart=MarketplaceManager.Today.AddDays(2);x.ClosureEnd=MarketplaceManager.Today;x.ClosureReason="Closed";x.ClosureScope="All";break;
        }
        Assert.NotEmpty(MarketplaceManager.ValidateService(x));
    }
    [Theory][InlineData("")][InlineData("/images/tour.jpg")][InlineData("https://example.com/tour.jpg")]
    public void ValidServiceImages(string image) => Assert.Empty(MarketplaceManager.ValidateService(new() {Name="Tour",Location="Đà Lạt",Image=image}));
    [Fact] public void ValidClosure() => Assert.Empty(MarketplaceManager.ValidateService(new() {Name="Tour",Location="Đà Lạt",ClosureStart=MarketplaceManager.Today,ClosureReason="Weather",ClosureScope="All"}));
    [Fact] public void SnapshotAndItemKeysPreserveAgreement()
    {
        var b=new Booking {SnapshotName="Tour",Date=MarketplaceManager.Today,People=2,Guests=3,Total=600000,Options=" Breakfast\n\nTransport ",Note="Original agreement"};
        var snapshot=MarketplaceManager.Snapshot(b);
        Assert.Contains("600000",snapshot);
        Assert.Contains("Tour",MarketplaceManager.DescribeSnapshot(snapshot));
        Assert.Equal("legacy plain text",MarketplaceManager.DescribeSnapshot("legacy plain text"));
        Assert.Equal(new[]{"main","option-1","option-2"},MarketplaceManager.Items(b).Select(x=>x.Key));
        Assert.Single(MarketplaceManager.Items(new Booking()));
    }
}
