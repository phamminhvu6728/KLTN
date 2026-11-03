const base=process.env.DEMO_URL||'http://localhost:5080';
const decode=s=>s.replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"');
let count=0;function check(value,message){if(!value)throw Error(message);console.log('PASS '+(++count)+' '+message);}
class Client{
 cookies={};
 async request(path,data){const r=await fetch(base+path,{method:data?'POST':'GET',redirect:'manual',headers:{Cookie:Object.entries(this.cookies).map(([k,v])=>k+'='+v).join('; '),...(data?{'Content-Type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined});for(const c of r.headers.getSetCookie()){const p=c.split(';')[0],i=p.indexOf('=');this.cookies[p.slice(0,i)]=p.slice(i+1);}return{status:r.status,location:r.headers.get('location'),html:decode(await r.text())};}
 async post(path,data,from='/Account/Login'){const page=await this.request(from);const token=page.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]*)"/)?.[1];return this.request(path,{...data,__RequestVerificationToken:token});}
}
const guest=new Client(),a=new Client(),b=new Client(),stamp=Date.now();
check((await guest.request('/Account/Login',{Email:'admin@demo.vn',Password:'Demo12345'})).status===400,'CSRF missing rejected');
let r=await guest.post('/Account/Login',{Email:'admin@demo.vn',Password:'Wrong12345'});check(r.status===200&&r.html.includes('không đúng'),'invalid password rejected');
check((await guest.request('/Journey')).status===302,'anonymous journey access rejected');
check((await guest.request('/Library')).status===404,'retired collection route unavailable');
check((await guest.request('/Home/Search?MinPrice=-1')).html.includes('validation-summary-errors'),'negative search price rejected');
check((await guest.request('/Home/Search?MinPrice=500&MaxPrice=100')).html.includes('Giá từ không được lớn hơn'),'reversed search price rejected');
check((await guest.request('/Home/Search?Type=Injected')).html.includes('Loại thông tin không hợp lệ'),'unsupported search type rejected');
check((await guest.request('/Home/Search?Sort=Injected')).html.includes('Cách sắp xếp không hợp lệ'),'unsupported search order rejected');
check((await guest.request('/Home/Search?Keyword=zzzz-no-match-'+stamp)).html.includes('Chưa tìm thấy'),'empty results give recoverable feedback');
check((await guest.request('/Home/Search?Type='+encodeURIComponent('Địa điểm')+'&Sort=price-asc')).status===200,'destination filtering and sort work');
const email='security-'+stamp+'@example.test',password='SessionTest12345',newPassword='NewSession12345';
r=await a.post('/Account/Register',{Name:'Security Test',Email:email,Phone:'08'+String(stamp%100000000).padStart(8,'0'),Password:password,ConfirmPassword:password,Role:'Traveler'},'/Account/Register');check(r.status===302,'create isolated session test account');
r=await a.post('/Account/Login',{Email:email,Password:password,ReturnUrl:'https://example.com/steal'});check(r.location==='/Journey','external return URL blocked');
await b.post('/Account/Login',{Email:email,Password:password});
check((await a.request('/Admin')).location?.includes('/Account/Denied'),'traveler cannot access admin');
check((await a.request('/Provider')).location?.includes('/Account/Denied'),'traveler cannot access supplier');
check((await a.request('/Governance')).location?.includes('/Account/Denied'),'traveler cannot manage provider operation');
check((await a.request('/Verification')).location?.includes('/Account/Denied'),'traveler cannot verify suppliers');
check((await a.request('/Verification/Download/2147483647')).status===404,'nonexistent private document denied');
await a.post('/Account/ChangePassword',{CurrentPassword:'wrong',NewPassword:newPassword,ConfirmPassword:newPassword},'/Account/Profile');check((await b.request('/Account/Profile')).status===200,'failed password change preserves other session');
await a.post('/Account/ChangePassword',{CurrentPassword:password,NewPassword:newPassword,ConfirmPassword:newPassword},'/Account/Profile');check((await a.request('/Account/Profile')).status===200,'password changer keeps current session');check((await b.request('/Account/Profile')).status===302,'password change revokes previous sessions');
const old=new Client();check((await old.post('/Account/Login',{Email:email,Password:password})).status===200,'old password no longer authenticates');
await a.post('/Account/Logout',{},'/Account/Profile');check((await a.request('/Account/Profile')).status===302,'logout removes authentication');
check((await a.post('/Account/Login',{Email:email,Password:newPassword})).status===302,'new password authenticates');
console.log('SECURITY SEARCH TEST PASSED: '+count+' assertions');
