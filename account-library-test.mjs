const base='http://localhost:5080';
const decode=s=>s.replace(/&#x([0-9a-f]+);/gi,(_,x)=>String.fromCodePoint(parseInt(x,16))).replace(/&#(\d+);/g,(_,x)=>String.fromCodePoint(+x)).replaceAll('&amp;','&').replaceAll('&quot;','"');
class Client {
 cookies={};
 async request(path,body){const r=await fetch(base+path,{method:body?'POST':'GET',headers:{Cookie:Object.entries(this.cookies).map(([k,v])=>`${k}=${v}`).join('; '),...(body?{'Content-Type':'application/x-www-form-urlencoded'}:{})},body:body?new URLSearchParams(body):undefined,redirect:'manual'});for(const raw of r.headers.getSetCookie()){const pair=raw.split(';')[0],at=pair.indexOf('=');this.cookies[pair.slice(0,at)]=pair.slice(at+1);}return {status:r.status,text:decode(await r.text()),location:r.headers.get('location')};}
 async post(path,data,from){const p=await this.request(from);if(p.status!==200)throw Error(`Cannot open form ${from}: ${p.status}`);const token=p.text.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);if(!token)throw Error(`Missing CSRF ${from}`);return this.request(path,{...data,__RequestVerificationToken:token[1]});}
 async login(email,password,returnUrl=''){return this.post('/Account/Login',{email,password,returnUrl},'/Account/Login');}
}
let passed=0;
function ok(test,message){if(!test)throw Error(message);console.log('PASS '+message);passed++;}
const unique=Date.now().toString(); const email=`account-${unique}@test.vn`,password='Travel2027abc',newPassword='Changed2027abc';
const owner=new Client();
ok((await owner.request('/Library')).status===200,'anonymous library opens');
let r=await owner.post('/Library/Add',{id:'1',returnUrl:'/Library'},'/Library');ok(r.status===302&&r.location==='/Library','anonymous can select temporary place');
r=await owner.request('/Library');ok(r.text.includes('Đà Lạt')&&r.text.includes('Đăng nhập để lưu'),'temporary selection visible without pretending persisted');
r=await owner.post('/Account/Register',{name:'Kiểm thử tài khoản',email,password,confirmPassword:password,role:'Traveler'},'/Account/Register');ok(r.status===302&&r.location==='/Journey','new traveler registration succeeds and signs in');
r=await owner.request('/Library');ok(r.text.includes('Đà Lạt')&&r.text.includes('Lưu danh sách vào tài khoản'),'selection survives registration');
const operation=r.text.match(/name="operation" value="([^"]+)"/)[1];
r=await owner.post('/Library/SaveList',{name:`Danh sách ${unique}`,operation},'/Library');ok(r.status===302&&/^\/Library\/Details\/\d+$/.test(r.location),'explicit save creates persisted list');const listUrl=r.location;const listId=listUrl.split('/').at(-1);
r=await owner.request(listUrl);ok(r.text.includes(`Danh sách ${unique}`)&&r.text.includes('Đà Lạt'),'saved list contents rendered');
r=await owner.request('/Library');ok(r.text.includes('Chưa có điểm đến trong tập tạm')&&r.text.includes(`Danh sách ${unique}`),'save clears temporary state and keeps persisted list');
r=await owner.request('/Library/Delete',{id:listId});ok(r.status===400,'missing CSRF cannot delete saved list');
const other=new Client();const otherEmail=`other-${unique}@test.vn`;
r=await other.post('/Account/Register',{name:'Người khác',email:otherEmail,password,confirmPassword:password,role:'Traveler'},'/Account/Register');ok(r.status===302,'second account registered');
ok((await other.request(listUrl)).status===404,'another account cannot read owned list');
r=await other.post('/Library/Delete',{id:listId},'/Library');ok(r.status===404,'another account cannot delete owned list');
const invalid=new Client();r=await invalid.post('/Account/Register',{name:'Role injection',email:`admin-inject-${unique}@test.vn`,password,confirmPassword:password,role:'Admin'},'/Account/Register');ok(r.status===200&&r.text.includes('Loại tài khoản không hợp lệ'),'public registration rejects Admin role');
r=await owner.post('/Account/UpdateProfile',{name:'Tên cập nhật',phone:'',bio:'Thích đi núi và chụp ảnh'},'/Account/Profile');ok(r.status===302,'profile update accepts blank optional phone');
r=await owner.request('/Account/Profile');ok(r.text.includes('Tên cập nhật')&&r.text.includes('Thích đi núi và chụp ảnh'),'profile changes persist');
const secondSession=new Client();r=await secondSession.login(email,password,'/Library');ok(r.status===302&&r.location==='/Library','login preserves local returnUrl');
r=await owner.post('/Account/ChangePassword',{currentPassword:password,newPassword,confirmPassword:newPassword},'/Account/Profile');ok(r.status===302,'dedicated test account changes password');
ok((await secondSession.request('/Account/Profile')).status===302,'password change revokes old session');
ok((await owner.request('/Account/Profile')).status===200,'password-changing session renewed');
const external=new Client();r=await external.login(email,newPassword,'https://example.com');ok(r.status===302&&r.location==='/Journey','external returnUrl ignored');
r=await owner.post('/Account/Deactivate',{password:newPassword},'/Account/Profile');ok(r.status===302&&r.location==='/','account with no obligations can deactivate');
ok((await owner.request('/Account/Profile')).status===302,'deactivated account signed out');
ok((await external.request('/Account/Profile')).status===302,'deactivated account old session rejected');
const disabled=new Client();r=await disabled.login(email,newPassword);ok(r.status===200&&r.text.includes('tài khoản đã ngừng hoạt động'),'deactivated account cannot sign in');
console.log(`Account/library verification complete: ${passed} assertions.`);
