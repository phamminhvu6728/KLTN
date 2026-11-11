using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuTuc.Models;
using TuTuc.Services;

namespace TuTuc.Controllers;

[Authorize(Roles = "Traveler")]
public class JourneyController(JourneyManager manager) : Controller
{
    // Danh tính lấy từ cookie đã xác thực, không nhận chủ kế hoạch từ form.
    int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    #region 1. Danh sách và chi tiết kế hoạch

    public async Task<IActionResult> Index(int trang = 1)
    {
        if (trang < 1)
            return BadRequest();
        var items = await manager.List(Me);
        ViewBag.Page = trang;
        ViewBag.TotalCount = items.Count;
        return View("PlanList", items
            .Skip((int)Math.Min((long)(trang - 1) * 20, items.Count))
            .Take(20)
            .ToList());
    }

    public async Task<IActionResult> Details(int id, string? sourceType, int? sourceId, int dayPage = 1)
    {
        var m = await manager.Detail(Me, id);
        if (m == null)
            return NotFound();
        if (dayPage < 1 || (long)(dayPage - 1) * 31 > (m.Trip.End.Date - m.Trip.Start.Date).Days)
            return BadRequest();
        m.DayOffset = (dayPage - 1) * 31;
        if (sourceType is "Place" or "Service" && sourceId.HasValue)
            m.DraftActivity = new()
            {
                TripId = id,
                Version = Convert.ToBase64String(m.Trip.Version),
                Start = m.Trip.Start.AddHours(8),
                End = m.Trip.Start.AddHours(9),
                SourceType = sourceType,
                SourceId = sourceId
            };
        return View("PlanDetails", m);
    }

    public async Task<IActionResult> ChooseSource(string sourceType, int sourceId)
    {
        if (sourceType is not ("Place" or "Service") || sourceId < 1)
            return BadRequest();
        ViewBag.SourceType = sourceType;
        ViewBag.SourceId = sourceId;
        var (trips, sourceName) = await manager.TripsForSource(Me, sourceType, sourceId);
        if (sourceName == "")
            return NotFound();
        ViewBag.SourceName = sourceName;
        return View(trips);
    }

    #endregion

    #region 2. Tạo, sửa hoạt động và chấp nhận kế hoạch

    // Bảng 4.179 / 4.184: Id = 0 để tạo, Id > 0 để sửa thông tin chung.
    [HttpPost]
    public async Task<IActionResult> Save(TripInput input)
    {
        RequirePosted("Start", "End");
        if (!ModelState.IsValid)
        {
            return await TripError(input, "Kiểm tra thông tin kế hoạch.", 400);
        }

        var r = await manager.Save(Me, input);
        if (!r.Success)
            return await TripError(input, r.Message, r.StatusCode, r.Code);

        TempData["Message"] = r.Message;
        return RedirectToAction("Details", new { id = r.Id });
    }

    // Bảng 4.181 / 4.182: điểm nhận yêu cầu thêm hoặc sửa một hoạt động.
    [HttpPost]
    public async Task<IActionResult> Activity(ActivityInput input)
    {
        // 1. Kiểm tra trường ngày được gửi và đọc lựa chọn nguồn tham khảo.
        RequirePosted("Start", "End");
        if (!string.IsNullOrEmpty(input.SourceSelection))
        {
            var parts = input.SourceSelection.Split(':');
            input.SourceType = parts[0];
            input.SourceId = parts.Length == 2 && int.TryParse(parts[1], out var sourceId)
                ? sourceId
                : null;
        }

        // 2. Lỗi binding / Data Annotations: trả lại form cùng nội dung đang nhập.
        if (!ModelState.IsValid)
            return await ActivityError(input, "Kiểm tra thông tin hoạt động.", 400);

        // 3. Manager kiểm tra nghiệp vụ, quyền đối tượng và ghi dữ liệu.
        var r = await manager.Activity(Me, input);
        if (!r.Success)
            return await ActivityError(input, r.Message, r.StatusCode, r.Code);

        // 4. Lưu thành công: chuyển hướng về chi tiết để nạp dữ liệu mới.
        TempData["Message"] = r.Message;
        return RedirectToAction("Details", new { id = input.TripId });
    }

    // Bảng 4.183: chỉ bỏ hoạt động khi không bị tình huống tham chiếu.
    [HttpPost]
    public async Task<IActionResult> RemoveActivity(int tripId, int id, string version) =>
        await Finish(await manager.RemoveActivity(Me, tripId, id, version), tripId);

    // Bảng 4.185: chấp nhận đúng phiên bản kế hoạch khách đang xem.
    [HttpPost]
    public async Task<IActionResult> Confirm(int id, string version) =>
        await Finish(await manager.Confirm(Me, id, version), id);

    // Bảng 4.186: lưu nháp rồi quay lại danh sách.
    [HttpPost]
    public async Task<IActionResult> Draft(int id, string version)
    {
        var result = await manager.Draft(Me, id, version);
        if (!result.Success)
            return await Finish(result, id);
        TempData["Message"] = result.Message;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete() => BadRequest("Xóa toàn bộ kế hoạch chưa thuộc phạm vi nghiệp vụ được xác nhận.");

    #endregion

    #region 3. Tình huống phát sinh

    public async Task<IActionResult> Incidents(int? tripId) => View(await manager.Incidents(Me, tripId));
    public async Task<IActionResult> Incident(int id)
    {
        var m = await manager.Incident(Me, id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost]
    public async Task<IActionResult> AddIncident(IncidentInput input)
    {
        if (!ModelState.IsValid)
            return await IncidentCreateError(input, "Nhập loại, mô tả và chọn chuyến đi của bạn.", 400);
        var r = await manager.AddIncident(Me, input);
        if (!r.Success)
            return await IncidentCreateError(input, r.Message, r.StatusCode);
        TempData["Message"] = r.Message;
        return RedirectToAction("Incident", new { id = r.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Plan(IncidentPlanInput input)
    {
        var r = ModelState.IsValid ? await manager.Plan(Me, input) : new JourneyOutcome(false, "Nhập phương án tối đa 1000 ký tự.", input.Id);
        if (r.Success)
        {
            TempData["Message"] = r.Message;
            return RedirectToAction("Incident", new { id = input.Id });
        }

        var m = await manager.Incident(Me, input.Id);
        if (m == null)
            return NotFound();
        m.DraftPlan = input;
        ModelState.AddModelError("", r.Message);
        Response.StatusCode = r.StatusCode;
        return View("Incident", m);
    }

    [HttpPost]
    public async Task<IActionResult> Result(IncidentResultInput input)
    {
        var r = ModelState.IsValid ? await manager.Result(Me, input) : new JourneyOutcome(false, "Kiểm tra kết quả thực tế.", input.Id);
        if (r.Success)
        {
            TempData["Message"] = r.Message;
            return RedirectToAction("Incident", new { id = input.Id });
        }

        var m = await manager.Incident(Me, input.Id);
        if (m == null)
            return NotFound();
        m.DraftResult = input;
        ModelState.AddModelError("", r.Message);
        Response.StatusCode = r.StatusCode;
        return View("Incident", m);
    }

    #endregion

    #region 4. Trả lỗi và giữ nội dung biểu mẫu

    void RequirePosted(params string[] names)
    {
        foreach (var name in names)
            if (!Request.Form.ContainsKey(name) || string.IsNullOrWhiteSpace(Request.Form[name]))
                ModelState.AddModelError(name, "Trường ngày bắt buộc chưa được nhập.");
    }

    async Task<IActionResult> TripError(TripInput input, string message, int status, string code = "0201-01")
    {
        ModelState.AddModelError("", message);
        Response.StatusCode = status;
        Response.Headers["X-Business-Error"] = code;
        if (input.Id == 0)
        {
            ViewBag.Draft = input;
            return View("PlanList", await manager.List(Me));
        }

        var m = await manager.Detail(Me, input.Id);
        if (m == null)
            return NotFound();
        m.DraftTrip = input;
        return View("PlanDetails", m);
    }

    async Task<IActionResult> ActivityError(ActivityInput input, string message, int status, string code = "0201-01")
    {
        var m = await manager.Detail(Me, input.TripId);
        if (m == null)
            return NotFound();
        m.DraftActivity = input;
        ModelState.AddModelError("", message);
        Response.StatusCode = status;
        Response.Headers["X-Business-Error"] = code;
        return View("PlanDetails", m);
    }

    async Task<IActionResult> IncidentCreateError(IncidentInput input, string message, int status)
    {
        var m = await manager.Incidents(Me, input.TripId);
        m.Draft = input;
        ModelState.AddModelError("", message);
        Response.StatusCode = status;
        return View("Incidents", m);
    }

    async Task<IActionResult> Finish(JourneyOutcome r, int id)
    {
        if (r.Success)
        {
            TempData["Message"] = r.Message;
            return RedirectToAction("Details", new { id });
        }

        var m = await manager.Detail(Me, id);
        if (m == null)
            return NotFound();
        ModelState.AddModelError("", r.Message);
        Response.StatusCode = r.StatusCode;
        Response.Headers["X-Business-Error"] = r.Code;
        return View("PlanDetails", m);
    }

    #endregion
}
