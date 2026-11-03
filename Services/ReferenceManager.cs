using Microsoft.EntityFrameworkCore;
using TuTuc.Data;
using TuTuc.Models;
namespace TuTuc.Services;
public class ReferenceManager(AppDbContext db) {
 public async Task<List<Place>> GetTemporary(List<int> ids)=>await db.Places.Where(x=>ids.Contains(x.Id)).ToListAsync();
 public Task<bool> PlaceExists(int id)=>db.Places.AnyAsync(x=>x.Id==id);
 public async Task<List<SavedReferenceList>> Lists(int owner)=>await db.SavedReferenceLists.Include(x=>x.Items).ThenInclude(x=>x.Place).Where(x=>x.OwnerId==owner).OrderByDescending(x=>x.CreatedAt).ToListAsync();
 public async Task<(int Id,string Error)> Save(int owner,List<int> ids,string name,Guid operation){
 name=(name??"").Trim();if(name.Length<2||name.Length>150)return(0,"Tên danh sách cần từ 2 đến 150 ký tự.");if(operation==Guid.Empty)return(0,"Lượt lưu không hợp lệ. Vui lòng tải lại trang.");
 var previous=await db.SavedReferenceLists.SingleOrDefaultAsync(x=>x.OperationId==operation);if(previous is not null)return previous.OwnerId==owner?(previous.Id,""):(0,"Lượt lưu không hợp lệ.");
 var valid=await db.Places.Where(x=>ids.Contains(x.Id)).Select(x=>x.Id).ToListAsync();if(valid.Count==0)return(0,"Hãy chọn ít nhất một điểm đến hợp lệ.");if(valid.Count!=ids.Distinct().Count())return(0,"Một số điểm đến không còn tồn tại. Hãy kiểm tra danh sách tạm.");
 var list=new SavedReferenceList{OwnerId=owner,Name=name,OperationId=operation,Items=valid.Select(id=>new SavedReferenceItem{PlaceId=id}).ToList()};db.SavedReferenceLists.Add(list);
 await using var tx=await db.Database.BeginTransactionAsync();try{await db.SaveChangesAsync();await tx.CommitAsync();return(list.Id,"");}catch(DbUpdateException){await tx.RollbackAsync();return(0,"Danh sách chưa thể lưu hoặc lượt lưu đã được ghi nhận. Hãy kiểm tra danh sách đã lưu.");}}
 public Task<SavedReferenceList?> Detail(int owner,int id)=>db.SavedReferenceLists.Include(x=>x.Items).ThenInclude(x=>x.Place).SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);
 public async Task<bool> Delete(int owner,int id){var list=await db.SavedReferenceLists.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==owner);if(list is null)return false;db.SavedReferenceLists.Remove(list);await db.SaveChangesAsync();return true;}
 public async Task<bool> Remove(int owner,int id,int item){var row=await db.SavedReferenceItems.SingleOrDefaultAsync(x=>x.Id==item&&x.SavedReferenceListId==id&&db.SavedReferenceLists.Any(l=>l.Id==id&&l.OwnerId==owner));if(row is null)return false;db.SavedReferenceItems.Remove(row);await db.SaveChangesAsync();return true;}
}
