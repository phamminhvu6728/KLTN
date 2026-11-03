using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Services;
public class TravelSearchManager(AppDbContext db)
{
    public async Task<List<TravelSearchItem>> Search(TravelSearchInput input)
    {
        var items=new List<TravelSearchItem>();
        if((input.Type is null or "" or "Địa điểm")&&!input.MinPrice.HasValue&&!input.MaxPrice.HasValue)
        {
            var query=db.Places.AsNoTracking().AsQueryable();
            if(!string.IsNullOrWhiteSpace(input.Keyword))query=query.Where(x=>x.Name.Contains(input.Keyword)||x.Description.Contains(input.Keyword));
            if(!string.IsNullOrWhiteSpace(input.Region))query=query.Where(x=>x.Region.Contains(input.Region)||x.Name.Contains(input.Region));
            if(input.MinPrice.HasValue)query=query.Where(x=>x.Price>=input.MinPrice);
            if(input.MaxPrice.HasValue)query=query.Where(x=>x.Price<=input.MaxPrice);
            items.AddRange(await query.Select(x=>new TravelSearchItem{Id=x.Id,Name=x.Name,Region=x.Region,Category=x.Category,Description=x.Description,Image=x.Image,Price=x.Price}).ToListAsync());
        }
        if(input.Type!="Địa điểm")
        {
            var query=from s in db.Services.AsNoTracking() join p in db.Accounts.AsNoTracking() on s.OwnerId equals p.Id
                where s.ApprovalStatus=="Đã duyệt"&&s.IsPublished&&p.ProviderStatus!="Đã thu hồi" select new{s,p};
            if(!string.IsNullOrWhiteSpace(input.Keyword))query=query.Where(x=>x.s.Name.Contains(input.Keyword)||x.s.Description.Contains(input.Keyword));
            if(!string.IsNullOrWhiteSpace(input.Region))query=query.Where(x=>x.s.Location.Contains(input.Region));
            if(!string.IsNullOrWhiteSpace(input.Type))query=query.Where(x=>x.s.Category==input.Type);
            if(input.MinPrice.HasValue)query=query.Where(x=>x.s.Price>=input.MinPrice);
            if(input.MaxPrice.HasValue)query=query.Where(x=>x.s.Price<=input.MaxPrice);
            var rows=await query.ToListAsync();
            items.AddRange(rows.Select(x=>new TravelSearchItem{Id=x.s.Id,SourceType="Service",Name=x.s.Name,Region=x.s.Location,Category=x.s.Category,Description=x.s.Description,Image=x.s.Image,Price=x.s.Price,CanRequest=ProviderRules.CanReceiveNew(x.s,x.p),State=ProviderRules.CanReceiveNew(x.s,x.p)?"Có thể gửi yêu cầu; chưa xác nhận khả năng phục vụ":"Không tiếp nhận yêu cầu mới"}));
        }
        return input.Sort switch{"price-asc"=>items.OrderBy(x=>x.Price).ThenBy(x=>x.Name).ToList(),"price-desc"=>items.OrderByDescending(x=>x.Price).ThenBy(x=>x.Name).ToList(),_=>items.OrderBy(x=>x.Name).ToList()};
    }
}
