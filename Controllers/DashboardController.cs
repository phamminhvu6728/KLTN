using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Controllers;
[Authorize]
public class DashboardController(AppDbContext db) : Controller
{
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(int? tripId)
    {
        if(User.IsInRole("Admin"))return RedirectToAction("Index","Admin");
        if(User.IsInRole("Provider"))return RedirectToAction("Index","Provider");
        var trips = await db.Trips.AsNoTracking().Include(x=>x.Activities).Where(x=>x.OwnerId==Me).OrderBy(x=>x.Start).ToListAsync();
        var requests = await db.Bookings.AsNoTracking().Include(x=>x.TravelService).Where(x=>x.OwnerId==Me).OrderByDescending(x=>x.Id).ToListAsync();
        var today=TuTuc.Services.JourneyRules.Today;
        if(tripId.HasValue&&!trips.Any(x=>x.Id==tripId.Value))return NotFound();
        // Chuyến hiện trên Tổng quan: ưu tiên chuyến đang diễn ra có nhiều hoạt động hôm nay nhất (giống OnTripManager), rồi chuyến sắp tới gần nhất.
        var activeTrip=tripId.HasValue?trips.Single(x=>x.Id==tripId.Value):trips.Where(x=>x.End.Date>=today).OrderByDescending(x=>x.Start.Date<=today).ThenByDescending(x=>x.Activities.Count(a=>a.Start.Date==today)).ThenBy(x=>x.Start).FirstOrDefault()??trips.OrderByDescending(x=>x.UpdatedAt).FirstOrDefault();
        return View(new DashboardVm { ActiveTrip=activeTrip, TripChoices=trips.OrderByDescending(x=>x.UpdatedAt).ToList(), Trips=trips.OrderByDescending(x=>x.UpdatedAt).Take(3).ToList(), TripCount=trips.Count, Bookings=requests.Take(4).ToList(), RequestCount=requests.Count,
            PendingCount=requests.Count(x=>TuTuc.Services.MarketplaceManager.PendingBookingStates.Contains(x.Status)), UnreadCount=await db.Notifications.CountAsync(x=>x.OwnerId==Me&&!x.IsRead),
            Notifications=await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==Me).OrderByDescending(x=>x.Id).Take(5).ToListAsync() });
    }
    public async Task<IActionResult> Notifications() => View(await db.Notifications.AsNoTracking().Where(x=>x.OwnerId==Me).OrderByDescending(x=>x.Id).Take(100).ToListAsync());
    [HttpPost] public async Task<IActionResult> Read(int id)
    {
        var n=await db.Notifications.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==Me);
        if(n==null)return NotFound(); n.IsRead=true; await db.SaveChangesAsync();
        return LocalRedirect(Url.IsLocalUrl(n.Url)?n.Url:"/Dashboard/Notifications");
    }
    [HttpPost] public async Task<IActionResult> ReadAll()
    {
        await db.Notifications.Where(x=>x.OwnerId==Me&&!x.IsRead).ExecuteUpdateAsync(x=>x.SetProperty(n=>n.IsRead,true));
        return RedirectToAction("Notifications");
    }
    [Authorize(Roles="Admin")] public async Task<IActionResult> Audit() => View(await db.AuditEntries.AsNoTracking().OrderByDescending(x=>x.Id).Take(200).ToListAsync());
}

