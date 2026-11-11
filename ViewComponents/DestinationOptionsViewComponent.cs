using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuTuc.Data;

namespace TuTuc.ViewComponents;

// Danh sách gợi ý điểm đến dùng chung cho mọi ô "Điểm đến" / "Nơi phục vụ"
// (tạo kế hoạch, đăng nhu cầu đồng hành, nhà cung cấp thêm dịch vụ).
// Hai phía cùng chọn tên từ bảng Places nên DestinationFilter so khớp được với nhau.
public class DestinationOptionsViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await db.Places.AsNoTracking().OrderBy(x => x.Region).ThenBy(x => x.Name).ToListAsync());
}
