using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
[Authorize(Roles="Admin")]
public class AdminController(AppDbContext db,MarketplaceManager manager):Controller
{
    int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index()=>View(new AdminDashboardVm{Accounts=await db.Accounts.OrderBy(x=>x.Role).ToListAsync(),Services=await db.Services.ToListAsync(),Revisions=await db.ServiceRevisions.Include(x=>x.TravelService).OrderBy(x=>x.CreatedAt).ThenBy(x=>x.Id).ToListAsync(),BookingCount=await db.Bookings.CountAsync()});
    [HttpPost]public async Task<IActionResult> Review(int id,string decision,string? note,string version){TempData["Message"]=await manager.Review(Me,id,decision,note,version);return RedirectToAction(nameof(Index));}
}
