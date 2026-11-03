using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
[Authorize]
public class GovernanceController(GovernanceManager manager):Controller
{
    int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [Authorize(Roles="Admin")]public async Task<IActionResult> Index(string? filter)=>View(await manager.List(filter));
    [Authorize(Roles="Admin")]public async Task<IActionResult> Details(int id){var vm=await manager.Detail(id,true);return vm==null?NotFound():View(vm);}
    [Authorize(Roles="Provider")]public async Task<IActionResult> Mine(){var vm=await manager.Detail(Me,false);return vm==null?NotFound():View("Details",vm);}
    [Authorize(Roles="Admin"),HttpPost]public async Task<IActionResult> Preview(GovernanceInput input)
    {
        if(!ModelState.IsValid){var detail=await manager.Detail(input.ProviderId,true);if(detail==null)return NotFound();detail.Input=input;return View("Details",detail);}
        var result=await manager.Preview(Me,input);if(result.Preview==null){ModelState.AddModelError("",result.Error);var detail=await manager.Detail(input.ProviderId,true);if(detail==null)return NotFound();detail.Input=input;return View("Details",detail);}
        HttpContext.Session.SetString("governance:"+result.Preview.OperationId,JsonSerializer.Serialize(result.Preview));return View(result.Preview);
    }
    [Authorize(Roles="Admin"),HttpPost]public async Task<IActionResult> Confirm(Guid operation)
    {
        var json=HttpContext.Session.GetString("governance:"+operation);if(json==null){TempData["Message"]="Phiên xác nhận không hợp lệ hoặc đã xử lý. Hãy xem lại nhà cung cấp.";return RedirectToAction("Index");}
        var preview=JsonSerializer.Deserialize<GovernancePreview>(json)!;
        TempData["Message"]=await manager.Confirm(Me,preview);HttpContext.Session.Remove("governance:"+operation);return RedirectToAction("Details",new{id=preview.Input.ProviderId});
    }
    [Authorize(Roles="Provider"),HttpPost,RequestSizeLimit(27*1024*1024)]public async Task<IActionResult> Explain(string? content,List<IFormFile>? files){TempData["Message"]=await manager.Explain(Me,content,files);return RedirectToAction("Mine");}
}
