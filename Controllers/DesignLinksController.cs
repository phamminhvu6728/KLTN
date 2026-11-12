using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TuTuc.Controllers;

// Thông báo ghi đúng đường dẫn trong Bảng 4.249 (thiết kế). Web dùng route tiếng Anh,
// nên các đường dẫn thiết kế này chuyển hướng sang trang thật tương ứng.
[Authorize]
public class DesignLinksController : Controller
{
    [HttpGet("/dong-hanh/de-nghi/xem-de-nghi/{id:int}")]
    public IActionResult Connection(int id) => Redirect("/Community/Dashboard#connection-" + id);

    [HttpGet("/dong-hanh/thoa-thuan/xem-de-nghi/{id:int}")]
    public IActionResult Agreement(int id) => Redirect("/Community/Dashboard#connections");

    [HttpGet("/nha-cung-cap/dich-vu/{id:int}")]
    public IActionResult ProviderService(int id) => Redirect("/Provider/Edit/" + id);

    [HttpGet("/yeu-cau/{id:int}")]
    [HttpGet("/yeu-cau/{id:int}/ket-qua/xem-ket-qua")]
    public IActionResult CustomerRequest(int id) => Redirect("/Marketplace/Request/" + id);

    [HttpGet("/nha-cung-cap/yeu-cau/{id:int}")]
    [HttpGet("/yeu-cau/{id:int}/ket-qua/mo-ghi-nhan")]
    public IActionResult ProviderRequest(int id) => Redirect("/Provider/Request/" + id);
}
