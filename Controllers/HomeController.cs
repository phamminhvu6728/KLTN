using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
public class HomeController(AppDbContext db, TravelSearchManager search) : Controller
{
    public async Task<IActionResult> Index(string? q, string? category)
    {
        if(User.Identity?.IsAuthenticated==true&&string.IsNullOrWhiteSpace(q)&&string.IsNullOrWhiteSpace(category))
            return RedirectToAction("Index","Dashboard");
        var places = db.Places.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) places = places.Where(x => x.Name.Contains(q) || x.Region.Contains(q));
        if (!string.IsNullOrWhiteSpace(category)) places = places.Where(x => x.Category == category);
        ViewBag.Query = q; ViewBag.Category = category;
        return View("SimpleIndex",await places.ToListAsync());
    }
        public async Task<IActionResult> Search(TravelSearchInput input)
    {
        if(input.MinPrice.HasValue&&input.MaxPrice.HasValue&&input.MinPrice>input.MaxPrice)ModelState.AddModelError("MinPrice","Giá từ không được lớn hơn giá đến.");
        if(input.Type!=null&&!new[]{"","Địa điểm","Lưu trú","Ăn uống","Di chuyển","Trải nghiệm"}.Contains(input.Type))ModelState.AddModelError("Type","Loại thông tin không hợp lệ.");
        if(!new[]{"name","price-asc","price-desc"}.Contains(input.Sort))ModelState.AddModelError("Sort","Cách sắp xếp không hợp lệ.");
        var vm=new TravelSearchVm{Input=input};if(ModelState.IsValid)vm.Items=await search.Search(input);return View(vm);
    }
    public async Task<IActionResult> Place(int id)
    {
        var place = await db.Places.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (place == null) return NotFound();
        // Mục tiêu 1: gom thông tin liên quan vào một trang để khách không phải tìm lại ở nơi khác.
        // Dịch vụ "tại nơi này" = dịch vụ có địa điểm phục vụ trùng tên địa điểm này,
        // hoặc trùng tên một địa điểm khác cùng khu vực (ví dụ Bản Cát Cát và Sa Pa cùng Lào Cai).
        var regionNames = await db.Places.AsNoTracking().Where(x => x.Region == place.Region).Select(x => x.Name).ToListAsync();
        regionNames.Add(place.Region);
        var published = await (from s in db.Services.AsNoTracking()
                               join p in db.Accounts.AsNoTracking() on s.OwnerId equals p.Id
                               where s.ApprovalStatus == "Đã duyệt" && s.IsPublished && p.ProviderStatus != "Đã thu hồi"
                               select s).ToListAsync();
        bool Matches(string location, string name) =>
            location.Contains(name, StringComparison.OrdinalIgnoreCase) || name.Contains(location, StringComparison.OrdinalIgnoreCase);
        var services = published
            .Where(s => Matches(s.Location, place.Name) || regionNames.Any(n => Matches(s.Location, n)))
            .OrderByDescending(s => Matches(s.Location, place.Name)).ThenBy(s => s.Category).ThenBy(s => s.Price)
            .Take(8).ToList();
        ViewBag.Services = services;

        ViewBag.Nearby = await db.Places.AsNoTracking().Where(x => x.Region == place.Region && x.Id != place.Id).Take(4).ToListAsync();
        return View(place);
    }
    // Keep old bookmarks working while all writes now use business services.
    public IActionResult Trips() => RedirectToAction("Index", "Journey");
    public IActionResult Companions() => RedirectToAction("Index", "Community");
    public IActionResult Services() => RedirectToAction("Index", "Marketplace");
    public IActionResult Incidents() => RedirectToAction("Incidents", "Journey");
    public IActionResult Status(int code) { Response.StatusCode = code; return View(code); }
    public IActionResult Error() => View();
}


