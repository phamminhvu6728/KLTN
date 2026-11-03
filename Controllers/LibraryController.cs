using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
[NonController]
public class LibraryController(ReferenceManager references):Controller {
 const string Key="reference_selection";
 int Me=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 List<int> Selection(){try{return JsonSerializer.Deserialize<List<int>>(HttpContext.Session.GetString(Key)??"[]")??[];}catch(JsonException){return [];}}
 void Store(List<int> ids)=>HttpContext.Session.SetString(Key,JsonSerializer.Serialize(ids.Distinct().Take(100)));
 public async Task<IActionResult> Index()=>View(new ReferenceLibraryViewModel{Temporary=await references.GetTemporary(Selection()),Saved=User.Identity?.IsAuthenticated==true?await references.Lists(Me):[]});
 [HttpPost]public async Task<IActionResult> Add(int id,string? returnUrl){if(!await references.PlaceExists(id))return NotFound();var selection=Selection();if(!selection.Contains(id))selection.Add(id);Store(selection);TempData["Message"]="Đã thêm vào tập tham khảo tạm. Vào mục Đã lưu để lưu thành danh sách.";return Url.IsLocalUrl(returnUrl)?LocalRedirect(returnUrl):RedirectToAction(nameof(Index));}
 [HttpPost]public IActionResult RemoveTemporary(int id){var selection=Selection();selection.RemoveAll(x=>x==id);Store(selection);return RedirectToAction(nameof(Index));}
 [HttpPost]public IActionResult Clear(){HttpContext.Session.Remove(Key);return RedirectToAction(nameof(Index));}
 [Authorize,HttpPost]public async Task<IActionResult> SaveList(string name,Guid operation){var result=await references.Save(Me,Selection(),name,operation);if(result.Error.Length>0){TempData["Message"]=result.Error;return RedirectToAction(nameof(Index));}HttpContext.Session.Remove(Key);TempData["Message"]="Đã lưu danh sách tham khảo vào tài khoản.";return RedirectToAction(nameof(Details),new{id=result.Id});}
 [Authorize]public async Task<IActionResult> Details(int id){var list=await references.Detail(Me,id);return list is null?NotFound():View(list);}
 [Authorize,HttpPost]public async Task<IActionResult> Delete(int id){if(!await references.Delete(Me,id))return NotFound();TempData["Message"]="Đã xóa danh sách tham khảo.";return RedirectToAction(nameof(Index));}
 [Authorize,HttpPost]public async Task<IActionResult> Remove(int id,int item){if(!await references.Remove(Me,id,item))return NotFound();return RedirectToAction(nameof(Details),new{id});}
}

