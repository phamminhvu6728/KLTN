using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TuTuc.Data;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
[Authorize(Roles="Provider")]
public class ProviderController(AppDbContext db,MarketplaceManager manager):Controller
{
    int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index()
    {
        var services=await db.Services.Where(x=>x.OwnerId==Me).OrderByDescending(x=>x.Id).ToListAsync();var ids=services.Select(x=>x.Id).ToArray();var user=await db.Accounts.FindAsync(Me);
        return View(new ProviderDashboardVm{Services=services,Bookings=await db.Bookings.Where(x=>ids.Contains(x.TravelServiceId)).ToListAsync(),Revisions=await db.ServiceRevisions.Where(x=>ids.Contains(x.TravelServiceId)).OrderByDescending(x=>x.RevisionNumber).ToListAsync(),Verified=ProviderRules.CanPublish(user),Status=user?.VerificationStatus+" · "+user?.ProviderStatus});
    }
    public async Task<IActionResult> Edit(int? id)
    {
        if(!ProviderRules.CanPublish(await db.Accounts.FindAsync(Me))){TempData["Message"]="Chưa có quyền gửi đăng/cập nhật dịch vụ. Hãy xem hồ sơ xác minh hoặc quyết định quyền hoạt động.";return RedirectToAction(nameof(Index));}
        if(id==null)return View(new ServiceEditVm());var s=await db.Services.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==Me);if(s==null)return NotFound();
        var latest=await db.ServiceRevisions.Where(x=>x.TravelServiceId==id).OrderByDescending(x=>x.RevisionNumber).FirstOrDefaultAsync();
        if(latest?.Status=="Chờ duyệt"){TempData["Message"]="Dịch vụ đang có một phiên bản chờ duyệt.";return RedirectToAction(nameof(Index));}
        ViewBag.ReviewNote=latest?.ReviewNote;
        var model=new ServiceEditVm{Id=s.Id,Name=s.Name,Description=s.Description,Category=s.Category,Location=s.Location,Price=s.Price,Image=s.Image,ServingHours=s.ServingHours,Conditions=s.Conditions,ClosureStart=s.ClosureStart,ClosureEnd=s.ClosureEnd,ClosureReason=s.ClosureReason,ClosureScope=s.ClosureScope,Version=Convert.ToBase64String(s.Version)};
        if(latest!=null&&new[]{"Cần sửa","Từ chối"}.Contains(latest.Status)){model.Name=latest.Name;model.Description=latest.Description;model.Category=latest.Category;model.Location=latest.Location;model.Price=latest.Price;model.Image=latest.Image;model.ServingHours=latest.ServingHours;model.Conditions=latest.Conditions;model.ClosureStart=latest.ClosureStart;model.ClosureEnd=latest.ClosureEnd;model.ClosureReason=latest.ClosureReason;model.ClosureScope=latest.ClosureScope;}
        ViewBag.IsDraft=s.ApprovalStatus=="Nháp";return View(model);
    }
    [HttpPost]public async Task<IActionResult> Edit(ServiceEditVm model)
    {
        var invalid=MarketplaceManager.ValidateService(model);if(invalid.Length>0)ModelState.AddModelError("",invalid);if(!ModelState.IsValid)return View(model);
        var result=await manager.SubmitService(Me,model);if(!result.StartsWith("Đã ")){ModelState.AddModelError("",result);return View(model);}TempData["Message"]=result;return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Requests(int trang=1,string? trangThai=null){if(trang<1)return BadRequest();return View("~/Views/Marketplace/Requests.cshtml",await manager.Requests(Me,true,page:trang,status:trangThai));}
    [ActionName("Request")]public async Task<IActionResult> RequestDetails(int id){var vm=await manager.Requests(Me,true,id);return vm.Bookings.Count==0?NotFound():View("~/Views/Marketplace/Requests.cshtml",vm);}
    [HttpPost]public async Task<IActionResult> Respond(int id,string decision,string version,int conditionsVersion,string? reason,BookingTermsInput terms){if(decision=="Đề xuất khác"&&!ModelState.IsValid)throw new BusinessFault("0701-03",400,"Điều kiện đề xuất chưa hợp lệ. Hãy kiểm tra ngày, số lượng và chi phí.");TempData["Message"]=await manager.Respond(Me,id,decision,version,conditionsVersion,reason,terms);return RedirectToAction("Request",new{id});}
    [HttpPost]public async Task<IActionResult> ReplyChange(int id,string decision,string version,int logicalVersion,string? reason,BookingTermsInput terms){TempData["Message"]=await manager.ReplyChange(Me,id,decision,version,logicalVersion,reason,terms);return RedirectToAction(nameof(Requests));}
    [HttpPost]public async Task<IActionResult> Results(int id,string version,List<BookingResultInput> items){TempData["Message"]=await manager.RecordResults(Me,id,version,items);return RedirectToAction(nameof(Requests));}
}
