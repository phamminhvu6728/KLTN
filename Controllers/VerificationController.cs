using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
[Authorize]
public class VerificationController(VerificationManager verification,PrivateDocumentStore documents):Controller {
 int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [Authorize(Roles="Admin")]public async Task<IActionResult> Index(string? status)=>View(await verification.Queue(status));
 [Authorize(Roles="Admin")]public async Task<IActionResult> Details(int id){var vm=await verification.AdminDetails(id);return vm is null?NotFound():View(vm);}
 [Authorize(Roles="Admin"),HttpPost]public async Task<IActionResult> Decide(VerificationDecisionInput input){var error=ModelState.IsValid?await verification.Decide(Me,input):"Quyết định chưa hợp lệ. Hãy kiểm tra nội dung và phiên bản.";if(error.Length>0){ModelState.AddModelError("",error);var vm=await verification.AdminDetails(input.Id);if(vm is null)return NotFound();vm.Decision=input;return View("Details",vm);}TempData["Message"]="Đã ghi quyết định xác minh và thông báo nhà cung cấp.";return RedirectToAction(nameof(Details),new{id=input.Id});}
 [Authorize(Roles="Provider")]public async Task<IActionResult> Mine(){var vm=await verification.Details(Me,null,false);return vm is null?NotFound():View("Details",vm);}
 [Authorize(Roles="Provider"),HttpPost,RequestSizeLimit(28000000)]public async Task<IActionResult> Submit(VerificationSubmissionInput input){var error=ModelState.IsValid?await verification.Submit(Me,input):"Nội dung hồ sơ chưa hợp lệ. Kiểm tra người đứng tên và nội dung đăng ký.";if(error.Length>0){ModelState.AddModelError("",error);var vm=await verification.Details(Me,null,false);if(vm is null)return NotFound();vm.Submission=input;return View("Details",vm);}TempData["Message"]="Đã gửi hồ sơ, chờ Admin xem xét lại. Việc gửi không tự tạo kết quả đã xác minh.";return RedirectToAction(nameof(Mine));}
 public async Task<IActionResult> Download(int id){var d=await documents.FindForReadAsync(Me,User.IsInRole("Admin"),id);if(d is null)return NotFound();var path=documents.GetPath(d);if(!System.IO.File.Exists(path))return NotFound();return PhysicalFile(path,d.Mime,d.OriginalName);}
 public async Task<IActionResult> Avatar(int id){var d=await documents.FindForReadAsync(Me,User.IsInRole("Admin"),id);if(d is null||d.Scope!="Avatar")return NotFound();var path=documents.GetPath(d);if(!System.IO.File.Exists(path))return NotFound();return PhysicalFile(path,d.Mime);}
}

