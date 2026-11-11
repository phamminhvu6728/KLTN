import {Client,field,hidden,formFor,requestStatus} from './tests/http-client.mjs';
let count=0;function check(value,message){if(!value)throw Error(message);console.log('PASS '+(++count)+' '+message);}
const stamp=Date.now(),day=n=>new Date(Date.now()+(n*24+7)*3600000).toISOString().slice(0,10);
const a=new Client(),b=new Client(),provider=new Client();
async function register(client,index){const email='focus-'+stamp+'-'+index+'@example.test';let r=await client.post('/Account/Register',{Name:'Khách kiểm thử '+index,Role:'Traveler',Email:email,Phone:'05'+String((stamp+index)%100000000).padStart(8,'0'),Password:'FocusTest123',ConfirmPassword:'FocusTest123'},'/Account/Register');check(r.status===302,'register focus actor '+index);r=await client.post('/Account/Login',{Email:email,Password:'FocusTest123'},'/Account/Login');check(r.status===302,'login focus actor '+index);}
await register(a,1);await register(b,2);await provider.post('/Account/LoginDemo',{role:'Provider'},'/Account/Login');
let r=await a.post('/Journey/Save',{Name:'Thiếu ngày',Destination:'Đà Lạt',Budget:0},'/Journey');check(r.status===400,'missing date fields cannot silently use model defaults');
r=await a.post('/Journey/Save',{Name:'   ',Destination:'Đà Lạt',Start:day(7),End:day(9),Budget:0},'/Journey');check(r.status===400,'whitespace trip name blocked');
r=await a.post('/Journey/Save',{Name:'Vượt giới hạn',Destination:'Đà Lạt',Start:day(7),End:day(373),Budget:0},'/Journey');check(r.status===302,'long itinerary accepted without an undocumented 366-day restriction');
r=await a.post('/Journey/Save',{Name:'Focus plan '+stamp,Destination:'Đà Lạt',Start:day(7),End:day(9),Budget:100000},'/Journey');check(r.status===302,'valid focused trip created');
const tripPath=r.location,tripId=tripPath.split('/').pop();let html=await a.get(tripPath);const version=()=>field(formFor(html,'/Journey/Confirm'),'version');let v=version();
check((await a.request('/')).location==='/Dashboard','signed-in home opens the trip management workspace');
const workspace=await a.get('/Dashboard');check(workspace.includes('data-testid="travel-workspace"')&&workspace.includes('Focus plan '+stamp),'workspace renders the owner trip');
check((await a.request('/Dashboard?tripId='+tripId)).status===200,'workspace trip selector opens an owned plan');
check((await b.request('/Dashboard?tripId='+tripId)).status===404,'workspace trip selector cannot expose another customer plan');
check((await b.request(tripPath)).status===404,'another traveler cannot read focused trip');
async function activity(title,start,end,more={}){return a.post('/Journey/Activity',{TripId:tripId,Version:v,Title:title,SourceType:'Manual',Start:day(7)+'T'+start,End:day(7)+'T'+end,HasTime:'true',SortOrder:1,Cost:0,...more},tripPath);}
r=await activity('Chiều nhập trước','14:00','15:00',{SortOrder:1,Cost:60000});check(r.status===302,'first activity saved');html=await a.get(tripPath);v=version();
r=await activity('Sáng nhập sau','08:00','09:00',{SortOrder:9,Cost:60000});check(r.status===302,'second activity saved with different order');html=await a.get(tripPath);const old=v;v=version();
check(html.indexOf('<h3>Chiều nhập trước</h3>')<html.indexOf('<h3>Sáng nhập sau</h3>'),'itinerary preserves per-day ordering specified by design');
check(html.includes('vượt ngân sách')&&!html.includes('Trùng giờ:'),'budget warning without false overlap');
check((await a.post('/Journey/Confirm',{id:tripId,version:old},tripPath)).status===409,'old plan version cannot confirm');
r=await activity('Không được lưu','08:00','08:00');check(r.status===422,'equal timed start and end rejected');
r=await activity('Nguồn giả','10:00','11:00',{SourceType:'Service',SourceId:2147483647});check(r.status===422,'nonexistent source rejected');
r=await activity('x'.repeat(301),'10:00','11:00');check(r.status===400,'title length enforced even via direct POST');
r=await a.post('/Journey/Save',{Id:tripId,Version:v,Name:'Focus plan '+stamp,Destination:'Đà Lạt',Start:day(8),End:day(9),Budget:100000},tripPath);check(r.status===422,'shrinking trip cannot orphan existing activities');
r=await a.post('/Journey/Confirm',{id:tripId,version:v},tripPath);check(r.status===302,'budget warning permits accepting plan');html=await a.get(tripPath);v=version();check(html.includes('Đã chấp nhận'),'accepted state rendered');
r=await activity('Hoạt động tạm','10:00','11:00');check(r.status===302,'editing accepted plan allowed');html=await a.get(tripPath);v=version();check(html.includes('data-testid="trip-status">Nháp'),'editing resets accepted plan to draft');
const block=[...html.matchAll(/<article[^>]*data-testid="activity-(\d+)"[\s\S]*?<\/article>/g)].find(m=>m[0].includes('Hoạt động tạm'));const aid=block[1];
check((await b.post('/Journey/RemoveActivity',{tripId,id:aid,version:v},'/Journey')).status===404,'stranger cannot delete activity');
r=await a.post('/Journey/RemoveActivity',{tripId,id:aid,version:v},tripPath);check(r.status===302,'owned unreferenced activity removed');check(!(await a.get(tripPath)).includes('<h3>Hoạt động tạm</h3>'),'deleted activity absent after reload');
let serviceId,detail;
// Lấy dịch vụ ở Đà Lạt: đều thuộc nhà cung cấp demo (ncc@demo.vn) mà kịch bản dùng để phản hồi.
const catalog=await a.get('/Marketplace?dest='+encodeURIComponent('Đà Lạt'));
for(const match of catalog.matchAll(/href="\/Marketplace\/Details\/(\d+)"/g)){const page=await a.get('/Marketplace/Details/'+match[1]);if(page.includes('Tiếp tục kiểm tra yêu cầu')){serviceId=match[1];detail=page;break;}}
check(Boolean(serviceId),'receiving service available for deep booking tests');
const terms={Service:serviceId,Operation:crypto.randomUUID(),Date:day(7),People:2,Guests:2,Options:'Bữa sáng\nĐón khách',ArrivalDeparture:'08:00',Note:'Focus request '+stamp,Total:0};
r=await a.post('/Marketplace/Preview',{...terms,People:0},detail);check(r.status===400&&r.html.includes('request-errors')&&!r.html.includes('request-send-form'),'invalid quantity preserves edit form, does not create preview');
check((await a.get('/Marketplace/Requests')).includes('Chưa có yêu cầu dịch vụ'),'invalid preview creates no booking');
r=await a.post('/Marketplace/Preview',terms,detail);check(r.status===200&&r.html.includes('request-preview'),'valid request reaches review screen');const preview=r.html,posted=hidden(formFor(preview,'/Marketplace/Book'));
check(Boolean(posted.PreviewToken),'preview binds confirmation to a protected expiring token');
r=await a.post('/Marketplace/EditPreview',{previewToken:posted.PreviewToken},preview);check(r.status===200&&r.html.includes('Focus request '+stamp)&&r.html.includes('Bữa sáng'),'back-to-edit preserves notes and selected items');
r=await a.post('/Marketplace/Book',{...posted,People:3},preview);check(r.status===409,'tampered confirmation returns conflict without mutation');check(!(await a.get('/Marketplace/Requests')).includes('id="request-'),'tampered quantity cannot create booking');
await b.post('/Marketplace/Book',posted,'/Marketplace');check(!(await b.get('/Marketplace/Requests')).includes('id="request-'),'preview ticket cannot be reused by another customer');
await a.post('/Marketplace/Book',{...posted,PreviewToken:'invalid-token'},preview);check(!(await a.get('/Marketplace/Requests')).includes('id="request-'),'invalid preview ticket cannot create booking');
await a.post('/Marketplace/Book',posted,preview);html=await a.get('/Marketplace/Requests');let match=html.match(/id="request-(\d+)"/);check(Boolean(match),'valid reviewed request saved');const bookingId=match[1];
await a.post('/Marketplace/Book',posted,preview);check((await a.get('/Marketplace/Requests')).match(/<article[^>]* id="request-\d+"/g).length===1,'repeated final send creates exactly one booking');
check((await b.request('/Marketplace/Request/'+bookingId)).status===404,'single-request page enforces customer ownership');
check((await a.request('/Marketplace/Request/'+bookingId)).status===200,'owner can open isolated request detail');
html=await provider.get('/Provider/Request/'+bookingId);const respond=hidden(formFor(html,'/Provider/Respond'));
await provider.post('/Provider/Respond',{...respond,decision:'Đề xuất khác',Date:day(8),People:'abc',Guests:2,Total:200000,reason:'Invalid quantity binding'},html);check((await a.get('/Marketplace/Request/'+bookingId)).includes('Chờ phản hồi'),'invalid provider numeric input cannot silently become the default quantity');
html=await a.get('/Marketplace/Request/'+bookingId);await a.post('/Marketplace/EditPending',{id:bookingId,...hidden(formFor(html,'/Marketplace/EditPending')),Date:day(8),People:'abc',Guests:2,Total:200000},html);check((await a.get('/Marketplace/Request/'+bookingId)).includes('Điều kiện phiên bản 1'),'invalid customer numeric edit cannot create a new version');
html=await provider.get('/Provider/Request/'+bookingId);await provider.post('/Provider/Respond',{...respond,decision:'Từ chối',reason:''},html);check((await a.get('/Marketplace/Request/'+bookingId)).includes('Chờ phản hồi'),'empty rejection reason leaves booking pending');
html=await provider.get('/Provider/Request/'+bookingId);await provider.post('/Provider/Respond',{...hidden(formFor(html,'/Provider/Respond')),decision:'Đã xác nhận'},html);check(requestStatus(await a.get('/Marketplace/Request/'+bookingId))==='Đã xác nhận','provider acceptance completes the exact customer agreement');
console.log('FOCUS DEEP TEST PASSED: '+count+' assertions');
