using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using TuTuc.Models;
using TuTuc.Services;
namespace TuTuc.Controllers;
public class CommunityController(CommunityManager manager,IDataProtectionProvider protection,CompanionSuggestion suggestion):Controller
{
 int Me=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
 IDataProtector Protector=>protection.CreateProtector("Community.DemandPreview.v1");
 public async Task<IActionResult> Index(CommunitySearch input){if(input.From>input.To)ModelState.AddModelError("To","Ngày kết thúc tìm kiếm phải từ ngày bắt đầu.");if(!string.IsNullOrEmpty(input.TravelStyle)&&!CommunityManager.TravelStyles.Contains(input.TravelStyle))ModelState.AddModelError("TravelStyle","Hình thức đi không hợp lệ.");if(!ModelState.IsValid){Response.StatusCode=400;return View(new CommunityPage{Search=input,Me=Me,IsTraveler=User.IsInRole("Traveler")});}var page=await manager.Browse(Me,input);
  // Mục tiêu 3: chấm mức độ phù hợp theo tiêu chí và gợi ý tiêu chí từ chuyến đi sắp tới.
  await suggestion.Apply(page,Me);
  if(page.IsTraveler)ViewBag.MyTrips=await suggestion.UpcomingTrips(Me);
  return View(page);}
 public async Task<IActionResult> Details(int id){var card=await manager.Detail(Me,id);return card==null?NotFound():View(card);}
 [Authorize(Roles="Traveler")]public async Task<IActionResult> Dashboard()=>View(await manager.Dashboard(Me));
 [Authorize(Roles="Traveler")]public async Task<IActionResult> Compose(int? id){if(!id.HasValue)return View(new DemandInput());var input=await manager.Edit(Me,id.Value);return input==null?NotFound():View(input);}
 [Authorize(Roles="Traveler"),HttpPost]public Task<IActionResult> Save(DemandInput input)=>Preview(input);
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Preview(DemandInput input){RequirePosted("Date","EndDate");var error=manager.ValidateDemand(input);if(error!=null)ModelState.AddModelError("",error);if(!ModelState.IsValid){Response.StatusCode=400;return View("Compose",input);}var m=await manager.Preview(Me,input);m.Token=Protector.Protect(JsonSerializer.Serialize(new PublishEnvelope(Me,DateTime.UtcNow,input)));return View("Preview",m);}
 [Authorize(Roles="Traveler"),HttpPost]public IActionResult EditPreview(string token){var envelope=Read(token);if(envelope==null)return BadRequest("Bản xem trước không hợp lệ hoặc đã hết hạn.");return View("Compose",envelope.Draft);}
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Publish(string token){var envelope=Read(token);if(envelope==null)return BadRequest("Bản xem trước không hợp lệ hoặc đã hết hạn. Hãy soạn lại.");var r=await manager.Save(Me,envelope.Draft);if(!r.Success){ModelState.AddModelError("",r.Message);Response.StatusCode=r.StatusCode;return View("Compose",envelope.Draft);}TempData["Message"]=r.Message;return RedirectToAction("Details",new{id=r.Id});}
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Stop(int id,string version)=>await DashboardResult(await manager.Stop(Me,id,version));
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Connect(int id,string? message)=>await DashboardResult(await manager.Connect(Me,id,message));
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Respond(int id,string version,string decision)=>await DashboardResult(await manager.Respond(Me,id,version,decision));
 [Authorize(Roles="Traveler")]public async Task<IActionResult> Propose(int connectionId){var m=await manager.AgreementEditor(Me,connectionId);return m==null?NotFound():View(m);}
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Propose(AgreementInput input){RequirePosted("Start","End");if(!ModelState.IsValid)return await AgreementError(input,"Kiểm tra nội dung thỏa thuận.",400);var r=await manager.Propose(Me,input);if(!r.Success)return await AgreementError(input,r.Message,r.StatusCode);TempData["Message"]=r.Message;return RedirectToAction("Dashboard");}
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> EndAgreement(int connectionId,int baseRevision)=>await DashboardResult(await manager.EndProposal(Me,connectionId,baseRevision));
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Agreement(int id,int revision,string version,string decision,string? reason){ViewBag.DraftReason=reason;return await DashboardResult(await manager.Agreement(Me,id,revision,version,decision,reason));}
 [Authorize(Roles="Traveler"),HttpPost]public async Task<IActionResult> Withdraw(int connectionId,string version,string? reason){ViewBag.DraftReason=reason;return await DashboardResult(await manager.Withdraw(Me,connectionId,version,reason));}
 void RequirePosted(params string[] names){foreach(var name in names)if(!Request.Form.ContainsKey(name)||string.IsNullOrWhiteSpace(Request.Form[name]))ModelState.AddModelError(name,"Trường ngày bắt buộc chưa được nhập.");}
 async Task<IActionResult> DashboardResult(CommunityOutcome r){if(r.Success){TempData["Message"]=r.Message;return RedirectToAction("Dashboard");}ModelState.AddModelError("",r.Message);Response.StatusCode=r.StatusCode;return View("Dashboard",await manager.Dashboard(Me));}
 async Task<IActionResult> AgreementError(AgreementInput input,string error,int status){var m=await manager.AgreementEditor(Me,input.ConnectionId);if(m==null)return NotFound();m.Draft=input;ModelState.AddModelError("",error);Response.StatusCode=status;return View("Propose",m);}
 PublishEnvelope? Read(string token){try{var e=JsonSerializer.Deserialize<PublishEnvelope>(Protector.Unprotect(token));return e?.Owner==Me&&DateTime.UtcNow-e.Created<TimeSpan.FromMinutes(30)?e:null;}catch(Exception ex)when(ex is System.Security.Cryptography.CryptographicException or JsonException or ArgumentNullException){return null;}}
 record PublishEnvelope(int Owner,DateTime Created,DemandInput Draft);
}

