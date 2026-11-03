using TuTuc.Models;
using TuTuc.Services;

namespace TuTuc.Tests;

public class DesignAlignmentTests
{
    [Fact]
    public void Table4179AllowsUnspecifiedBudget()
    {
        var input=new TripInput{Name="Chuyến đi",Destination="Đà Lạt",Start=JourneyRules.Today,End=JourneyRules.Today.AddDays(2),Budget=null};
        Assert.Empty(JourneyRules.ValidateTrip(input));
    }

    [Fact]
    public void Table4181AllowsUnspecifiedCost()
    {
        var trip=new Trip{Start=JourneyRules.Today,End=JourneyRules.Today.AddDays(2),Budget=null};
        var activity=new ActivityInput{Title="Tham quan",Start=trip.Start.AddHours(8),End=trip.Start.AddHours(9),Cost=null};
        Assert.Empty(JourneyRules.ValidateActivity(activity,trip));
    }

    [Fact]
    public void DetailedDesignDoesNotLimitAPlanToOneYear()
    {
        var input=new TripInput{Name="Kế hoạch dài",Destination="Việt Nam",Start=JourneyRules.Today,End=JourneyRules.Today.AddDays(500),Budget=null};
        Assert.Empty(JourneyRules.ValidateTrip(input));
    }

    [Theory]
    [InlineData("Hoàn thành","Hoàn thành","Đã ghi nhận",2,"Hoàn thành")]
    [InlineData("Không thực hiện","Khách không đến","Đã ghi nhận",2,"Không thực hiện")]
    [InlineData("Khách không đến","Khách không đến","Đã ghi nhận",2,"Không thực hiện")]
    [InlineData("Hoàn thành","Khách không đến","Đã ghi nhận",2,"Thực hiện một phần")]
    [InlineData("Hoàn thành","Thực hiện một phần","Đã ghi nhận",2,"Thực hiện một phần")]
    [InlineData("Hoàn thành","Chưa xác định","Chờ kiểm tra",2,"Chờ kiểm tra")]
    [InlineData("Hoàn thành","Hoàn thành","Chờ kiểm tra",2,"Chờ kiểm tra")]
    [InlineData("Hoàn thành","Hoàn thành","Đã ghi nhận",3,"Chờ kiểm tra")]
    public void Section4914AggregatesActualItems(string first,string second,string verification,int expectedItems,string expected)
    {
        var results=new[]{new BookingItemResult{Outcome=first,VerificationStatus="Đã ghi nhận"},new BookingItemResult{Outcome=second,VerificationStatus=verification}};
        Assert.Equal(expected,MarketplaceManager.AggregateResultStatus(results,expectedItems));
    }

    [Fact]
    public void TermsValidationCanUseTheInjectedBusinessDate()
    {
        var today=new DateTime(2030,1,2);
        var terms=new BookingTermsInput{Date=today.AddDays(-1),People=1,Guests=1};
        Assert.NotEmpty(MarketplaceManager.ValidateTerms(terms,today));
        terms.Date=today;
        Assert.Empty(MarketplaceManager.ValidateTerms(terms,today));
    }
}
