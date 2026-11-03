using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
public class MarketplaceController(AppDbContext db,MarketplaceManager manager):Controller
{
    int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index(string? q,string? category,decimal? minPrice,decimal? maxPrice)
    {
        var model=new MarketCatalogVm{Query=q??"",Category=category??"",MinPrice=minPrice,MaxPrice=maxPrice};
        if(minPrice<0||maxPrice<0||minPrice>maxPrice){model.Error="Khoảng giá chưa hợp lệ: giá từ phải nhỏ hơn hoặc bằng giá đến.";return View(model);}
        var query=db.Services.Where(x=>x.ApprovalStatus=="Đã duyệt"&&x.IsPublished&&db.Accounts.Any(a=>a.Id==x.OwnerId&&a.ProviderStatus!="Đã thu hồi"));
        if(!string.IsNullOrWhiteSpace(q))query=query.Where(x=>x.Name.Contains(q)||x.Location.Contains(q));
        if(!string.IsNullOrWhiteSpace(category))query=query.Where(x=>x.Category==category);
        if(minPrice.HasValue)query=query.Where(x=>x.Price>=minPrice);if(maxPrice.HasValue)query=query.Where(x=>x.Price<=maxPrice);
        model.Services=await query.OrderByDescending(x=>x.Id).ToListAsync();ViewBag.Providers=await db.Accounts.Where(x=>x.Role=="Provider").ToDictionaryAsync(x=>x.Id);return View(model);
    }
    public async Task<IActionResult> Details(int id,DateTime? date)
    {
        var service=await db.Services.FindAsync(id);var provider=service==null?null:await db.Accounts.FindAsync(service.OwnerId);
        if(service==null||provider==null||!ProviderRules.IsPublic(service,provider)){TempData["Message"]="Dịch vụ không còn được công bố hoặc chưa thể truy cập.";return RedirectToAction(nameof(Index));}
        ViewBag.Provider=provider;ViewBag.CanReceive=ProviderRules.CanReceiveNew(service,provider);
        if(date.HasValue&&date.Value.Date>=MarketplaceManager.Today)ViewBag.Terms=new BookingTermsInput{Service=id,Date=date.Value.Date};
        return View("ServiceDetails",service);
    }
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Preview(BookingTermsInput terms)
    {
        var result=ModelState.IsValid?await manager.Preview(Me,terms):new BookingPreviewOutcome(null,"Điều kiện chưa hợp lệ. Hãy kiểm tra các trường.");
        if(result.Preview==null){Response.StatusCode=result.StatusCode;Response.Headers["X-Business-Error"]=result.Code;ModelState.AddModelError("",result.Error);var service=await db.Services.FindAsync(terms.Service);var provider=service==null?null:await db.Accounts.FindAsync(service.OwnerId);if(service==null||provider==null||!ProviderRules.IsPublic(service,provider))throw new BusinessFault(result.Code,result.StatusCode,result.Error);ViewBag.Provider=provider;ViewBag.CanReceive=ProviderRules.CanReceiveNew(service,provider);ViewBag.Terms=terms;return View("ServiceDetails",service);}return View("RequestPreview",result.Preview);
    }
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> EditPreview(string previewToken)
    {
        var terms=manager.ReadPreview(Me,previewToken);
        if(terms==null){TempData["Message"]="Bản xem trước đã hết hạn. Hãy nhập lại yêu cầu.";return RedirectToAction(nameof(Index));}
        var service=await db.Services.FindAsync(terms.Service);var provider=service==null?null:await db.Accounts.FindAsync(service.OwnerId);
        if(service==null||provider==null||!ProviderRules.IsPublic(service,provider))return RedirectToAction(nameof(Index));
        ModelState.Clear();ViewBag.Terms=terms;ViewBag.Provider=provider;ViewBag.CanReceive=ProviderRules.CanReceiveNew(service,provider);
        return View("ServiceDetails",service);
    }
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Book(BookingTermsInput terms){if(!ModelState.IsValid)throw new BusinessFault("0701-03",400,"Điều kiện đặt chưa hợp lệ.");TempData["Message"]=await manager.Book(Me,terms);var id=await db.Bookings.Where(x=>x.OwnerId==Me&&x.OperationId==terms.Operation).Select(x=>x.Id).FirstAsync();return RedirectToAction("Request",new{id});}
    [Authorize(Roles="Traveler")]public async Task<IActionResult> Requests(int trang=1,string? trangThai=null){if(trang<1)return BadRequest();return View(await manager.Requests(Me,false,page:trang,status:trangThai));}
    [Authorize(Roles="Traveler"),ActionName("Request")]public async Task<IActionResult> RequestDetails(int id){var vm=await manager.Requests(Me,false,id);return vm.Bookings.Count==0?NotFound():View("Requests",vm);}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Withdraw(int id,string version){TempData["Message"]=await manager.Withdraw(Me,id,version);return RedirectToAction(nameof(Requests));}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> EditPending(int id,BookingTermsInput terms){if(!ModelState.IsValid)throw new BusinessFault("0701-03",400,"Điều kiện đặt chưa hợp lệ.");TempData["Message"]=await manager.EditPending(Me,id,terms);return RedirectToAction(nameof(Requests));}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> ReplyOffer(int id,bool accept,string version,int conditionsVersion){TempData["Message"]=await manager.ReplyOffer(Me,id,accept,version,conditionsVersion);return RedirectToAction("Request",new{id});}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> ProposeChange(int id,string kind,BookingTermsInput terms){TempData["Message"]=await manager.ProposeChange(Me,id,kind,terms);return RedirectToAction(nameof(Requests));}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> ReplyChangeOffer(int id,bool accept,string version,int logicalVersion){TempData["Message"]=await manager.ReplyChangeOffer(Me,id,accept,version,logicalVersion);return RedirectToAction(nameof(Requests));}
    [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> FeedbackItem(int id,string feedback,string version){TempData["Message"]=await manager.FeedbackItem(Me,id,feedback,version);return RedirectToAction(nameof(Requests));}
}
