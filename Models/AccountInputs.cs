using System.ComponentModel.DataAnnotations;
namespace TuTuc.Models;
public class LoginInput {
 [Required(ErrorMessage="Nhập email hoặc số điện thoại."),StringLength(200)]public string Email {get;set;}="";
 [Required(ErrorMessage="Nhập mật khẩu."),StringLength(100)]public string Password {get;set;}="";
 public string? ReturnUrl {get;set;}
}
public class RegisterInput:IValidatableObject {
 [Required(ErrorMessage="Nhập họ tên."),StringLength(100,MinimumLength=2)]public string Name {get;set;}="";
 [Required(ErrorMessage="Nhập email."),EmailAddress(ErrorMessage="Email không hợp lệ."),StringLength(200)]public string Email {get;set;}="";
 [Required(ErrorMessage="Nhập số điện thoại."),StringLength(30)]public string Phone {get;set;}="";
 [Required(ErrorMessage="Nhập mật khẩu."),StringLength(100,MinimumLength=8)]public string Password {get;set;}="";
 [Compare(nameof(Password),ErrorMessage="Mật khẩu xác nhận chưa khớp.")]public string ConfirmPassword {get;set;}="";
 public string Role {get;set;}="Traveler";
 public string? CompanyName {get;set;} public string? CompanyType {get;set;} public string? ContactName {get;set;} public string? Address {get;set;} public string? ContactPhone {get;set;} public string? ContactEmail {get;set;} public string? DossierContent {get;set;} public string? Representative {get;set;} public List<IFormFile> Files {get;set;}=[];
 public IEnumerable<ValidationResult> Validate(ValidationContext context){if(Role=="Provider"){var required=new[]{(CompanyName,nameof(CompanyName),200,"Tên cơ sở"),(CompanyType,nameof(CompanyType),50,"Loại hình"),(ContactName,nameof(ContactName),100,"Người liên hệ"),(Address,nameof(Address),300,"Địa chỉ"),(ContactPhone,nameof(ContactPhone),30,"Số điện thoại liên hệ"),(ContactEmail,nameof(ContactEmail),200,"Email liên hệ"),(DossierContent,nameof(DossierContent),2000,"Nội dung hồ sơ"),(Representative,nameof(Representative),100,"Người đứng tên hồ sơ")};foreach(var(value,key,max,label)in required)if(string.IsNullOrWhiteSpace(value)||value.Length>max)yield return new ValidationResult($"{label} bắt buộc, tối đa {max} ký tự.",[key]);if(!string.IsNullOrEmpty(ContactEmail)&&!new EmailAddressAttribute().IsValid(ContactEmail))yield return new ValidationResult("Email liên hệ chưa hợp lệ.",[nameof(ContactEmail)]);if(Files.Count is <1 or >5)yield return new ValidationResult("Nhà cung cấp cần nộp từ 1 đến 5 tệp xác minh.",[nameof(Files)]);}}
}
public class ProfileInput {
 [Required,StringLength(100,MinimumLength=2)]public string Name {get;set;}="";
 [StringLength(30)]public string? Phone {get;set;}
 [StringLength(1000)]public string? Bio {get;set;}
 [StringLength(200)]public string? Residence {get;set;}
 [StringLength(500)]public string? Interests {get;set;}
 public string SharingScope {get;set;}="Private";public bool ShareContact {get;set;}
 public IFormFile? Avatar {get;set;}
 [Required]public string Version {get;set;}="";
 public string? ProviderVersion {get;set;}
 [StringLength(100)]public string? ContactName {get;set;}[StringLength(300)]public string? Address {get;set;}[StringLength(30)]public string? ContactPhone {get;set;}[EmailAddress,StringLength(200)]public string? ContactEmail {get;set;}
}
public class ProfilePageVm {public Account Account {get;set;}=new();public UserProfile Profile {get;set;}=new();public ProviderProfile? Provider {get;set;}public ProfileInput Input {get;set;}=new();}
public class PasswordInput {
 [Required(ErrorMessage="Nhập mật khẩu hiện tại.")]public string CurrentPassword {get;set;}="";
 [Required,StringLength(100,MinimumLength=8)]public string NewPassword {get;set;}="";
 [Compare(nameof(NewPassword),ErrorMessage="Mật khẩu xác nhận chưa khớp.")]public string ConfirmPassword {get;set;}="";
}
public class DeactivationObligation {public string Text {get;set;}="";public string Url {get;set;}="";}
public class DeactivationPage {public List<DeactivationObligation> Obligations {get;set;}=[];public int Demands {get;set;}public int Connections {get;set;}public int Services {get;set;}public string Token {get;set;}="";public string Version {get;set;}="";public bool Ready=>Obligations.Count==0&&Token.Length>0;}
