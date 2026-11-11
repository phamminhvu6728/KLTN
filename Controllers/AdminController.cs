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
    // Danh sách địa điểm để Admin thấy nơi phục vụ của dịch vụ có khớp địa điểm trong hệ thống không (DestinationFilter.Same).
    public async Task<IActionResult> Index(){ViewBag.Places=await db.Places.AsNoTracking().ToListAsync();return View(new AdminDashboardVm{Accounts=await db.Accounts.OrderBy(x=>x.Role).ToListAsync(),Services=await db.Services.ToListAsync(),Revisions=await db.ServiceRevisions.Include(x=>x.TravelService).OrderBy(x=>x.CreatedAt).ThenBy(x=>x.Id).ToListAsync(),BookingCount=await db.Bookings.CountAsync()});}
    [HttpPost]public async Task<IActionResult> Review(int id,string decision,string? note,string version){TempData["Message"]=await manager.Review(Me,id,decision,note,version);return RedirectToAction(nameof(Index));}
}
