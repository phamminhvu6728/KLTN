using TuTuc.Models;
using TuTuc.Services;

namespace TuTuc.Tests;

public class JourneyRulesTests
{
    static TripInput ValidTrip() => new() { Name="Đà Lạt",Destination="Lâm Đồng",Start=JourneyRules.Today,End=JourneyRules.Today.AddDays(2),Budget=2_000_000 };
    static Trip Trip() => new() { Start=JourneyRules.Today,End=JourneyRules.Today.AddDays(2) };
    static ActivityInput ValidActivity() => new() { Title="Bảo tàng",Start=JourneyRules.Today.AddHours(8),End=JourneyRules.Today.AddHours(9) };

    [Fact] public void ValidTripPasses() => Assert.Empty(JourneyRules.ValidateTrip(ValidTrip()));
    [Theory]
    [InlineData("empty-name")][InlineData("spaces-name")][InlineData("long-name")]
    [InlineData("empty-destination")][InlineData("long-destination")][InlineData("missing-start")]
    [InlineData("missing-end")][InlineData("reversed")]
    [InlineData("negative-budget")][InlineData("large-budget")]
    public void InvalidTripIsRejected(string scenario)
    {
        var x=ValidTrip();
        switch(scenario) {
            case "empty-name":x.Name="";break; case "spaces-name":x.Name="  ";break;
            case "long-name":x.Name=new string('x',151);break;
            case "empty-destination":x.Destination="";break;case "long-destination":x.Destination=new string('x',201);break;
            case "missing-start":x.Start=default;break;case "missing-end":x.End=default;break;
            case "reversed":x.End=x.Start.AddDays(-1);break;
            case "negative-budget":x.Budget=-1;break;case "large-budget":x.Budget=1_000_000_001;break;
        }
        Assert.NotEmpty(JourneyRules.ValidateTrip(x));
    }
    [Fact] public void MaximumTripBoundaryAccepted() {var x=ValidTrip();x.End=x.Start.AddDays(365);x.Name=new string('x',150);x.Destination=new string('x',200);x.Budget=1_000_000_000;Assert.Empty(JourneyRules.ValidateTrip(x));}
    [Theory]
    [InlineData("before-trip")][InlineData("after-trip")][InlineData("end-outside")]
    [InlineData("same-time")][InlineData("reversed-time")][InlineData("negative-cost")]
    [InlineData("large-cost")][InlineData("zero-order")][InlineData("empty-title")]
    [InlineData("long-title")][InlineData("long-location")][InlineData("long-notes")]
    [InlineData("bad-source")][InlineData("missing-source")]
    public void InvalidActivityIsRejected(string scenario)
    {
        var x=ValidActivity();
        switch(scenario) {
            case "before-trip":x.Start=x.Start.AddDays(-1);break;case "after-trip":x.Start=x.Start.AddDays(3);x.End=x.Start.AddHours(1);break;
            case "end-outside":x.End=x.End.AddDays(3);break;case "same-time":x.End=x.Start;break;
            case "reversed-time":x.End=x.Start.AddMinutes(-1);break;case "negative-cost":x.Cost=-1;break;
            case "large-cost":x.Cost=1_000_000_001;break;case "zero-order":x.SortOrder=0;break;
            case "empty-title":x.Title=" ";break;case "long-title":x.Title=new string('x',301);break;
            case "long-location":x.Location=new string('x',301);break;case "long-notes":x.Notes=new string('x',1001);break;
            case "bad-source":x.SourceType="Unknown";break;case "missing-source":x.SourceType="Place";x.SourceId=null;break;
        }
        Assert.NotEmpty(JourneyRules.ValidateActivity(x,Trip()));
    }
    [Fact] public void UntimedActivityCanUseSameDay() {var x=ValidActivity();x.HasTime=false;x.End=x.Start;Assert.Empty(JourneyRules.ValidateActivity(x,Trip()));}
    [Fact] public void ReferenceMaySupplyTheTitle() {var x=ValidActivity();x.Title="";x.SourceType="Place";x.SourceId=1;Assert.Empty(JourneyRules.ValidateActivity(x,Trip()));}
    [Fact] public void BoundaryLengthsAndZeroCostAccepted() {var x=ValidActivity();x.Title=new string('x',300);x.Location=new string('x',300);x.Notes=new string('x',1000);Assert.Empty(JourneyRules.ValidateActivity(x,Trip()));}
    [Fact] public void LastDayOfTripAccepted() {var x=ValidActivity();x.Start=Trip().End.AddHours(22);x.End=Trip().End.AddHours(23);Assert.Empty(JourneyRules.ValidateActivity(x,Trip()));}
}
