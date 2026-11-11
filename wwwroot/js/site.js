(() => {
 'use strict';
 // Preview/final-confirmation pages already provide their own confirmation step.
 const existingReview = new Set(['/community/preview','/community/publish','/community/editpreview','/marketplace/preview','/marketplace/book','/marketplace/editpreview','/governance/preview','/governance/confirm','/account/reauthenticate','/account/deactivate']);
 const changes = new Set([
  '/journey/save','/journey/activity','/journey/removeactivity','/journey/confirm','/journey/draft','/journey/addincident','/journey/plan','/journey/result',
  '/community/connect','/community/respond','/community/propose','/community/agreement','/community/endagreement','/community/withdraw','/community/stop',
  '/provider/edit','/provider/respond','/provider/replychange','/provider/results','/admin/review',
  '/marketplace/editpending','/marketplace/withdraw','/marketplace/replyoffer','/marketplace/proposechange','/marketplace/replychangeoffer','/marketplace/feedbackitem',
  '/account/register','/account/updateprofile','/account/changepassword','/verification/submit','/verification/decide','/governance/explain'
 ]);
 const dialog=document.getElementById('action-confirmation');
 const title=document.getElementById('confirmation-title');
 const message=document.getElementById('confirmation-message');
 const summary=document.getElementById('confirmation-summary');
 const accept=document.getElementById('confirmation-accept');
 const cancel=document.getElementById('confirmation-cancel');
 let pending=null;
 const approved=new WeakSet();
 const sending=new WeakSet();
 function describe(form,button,path){
  const label=(button?.textContent||'Lưu thay đổi').trim().replace(/\s+/g,' ');
  const destructive=/remove|delete|withdraw|\/stop$/.test(path)||/rút|từ chối|hủy|ngừng/i.test(label);
  return {label,destructive,message:button?.dataset.confirm||form.dataset.confirm||
   (destructive?'Bạn có chắc muốn thực hiện “'+label+'”? Thao tác này sẽ thay đổi dữ liệu hoặc trạng thái hiện tại.':'Bạn có chắc muốn thực hiện “'+label+'” với các thông tin đã nhập?')};
 }
 function fillSummary(form){
  summary.replaceChildren();
  for(const field of form.elements){
   if(!field.name||field.disabled||['hidden','password','submit','button','reset'].includes(field.type)||field.closest('fieldset[disabled]'))continue;
   if(field.type==='radio'&&!field.checked)continue;
   let value=field.type==='file'?Array.from(field.files||[]).map(f=>f.name).join(', '):field.type==='checkbox'?(field.checked?'Có':'Không'):field.tagName==='SELECT'?Array.from(field.selectedOptions).map(o=>o.textContent).join(', '):field.value;
   if(!value?.trim())continue;
   const label=field.labels?.[0];
   // Only direct label text; never echo a select's entire option list or password.
   const caption=label?Array.from(label.childNodes).filter(n=>n.nodeType===Node.TEXT_NODE).map(n=>n.textContent).join(' ').trim():'';
   if(!caption)continue;
   const term=document.createElement('dt'),detail=document.createElement('dd');
   term.textContent=caption;detail.textContent=value;summary.append(term,detail);
  }
  if(!summary.childElementCount){
   const target=form.closest('article')?.querySelector('h2,h3')?.textContent?.trim();
   if(target){const term=document.createElement('dt'),detail=document.createElement('dd');term.textContent='Mục đang thao tác';detail.textContent=target;summary.append(term,detail);}
  }
 }
 function finishSending(form,button){
  sending.add(form);
  // Defer disabling so the clicked button's name/value remains in the POST.
  if(button)window.setTimeout(()=>{button.disabled=true;button.textContent='Đang xử lý…';},0);
 }
 document.addEventListener('submit',event=>{
  const form=event.target,button=event.submitter;
  if(event.defaultPrevented||!(form instanceof HTMLFormElement)||(button?.getAttribute('formmethod')||form.method).toLowerCase()!=='post')return;
  if(sending.has(form)){event.preventDefault();return;}
  if(!form.checkValidity())return;
  if(approved.delete(form)){finishSending(form,button);return;}
  const path=new URL(button?.getAttribute('formaction')||form.action,location.href).pathname.toLowerCase().replace(/\/\d+\/?$/,'').replace(/\/$/,'');
  const needsConfirmation=button?.dataset.confirm||form.dataset.confirm||!existingReview.has(path)&&changes.has(path);
  if(!needsConfirmation){finishSending(form,button);return;}
  event.preventDefault();
  if(pending)return;
  const info=describe(form,button,path);
  if(!dialog||typeof dialog.showModal!=='function'){
   if(window.confirm(info.message)){approved.add(form);button?form.requestSubmit(button):form.requestSubmit();}
   return;
  }
  pending={form,button,focus:document.activeElement};
  title.textContent=info.destructive?'Xác nhận thay đổi dữ liệu':'Xác nhận: '+info.label;
  message.textContent=info.message;accept.classList.toggle('danger',info.destructive);
  fillSummary(form);dialog.showModal();cancel.focus();
 });
 cancel?.addEventListener('click',()=>dialog.close());
 dialog?.addEventListener('close',()=>{const previous=pending;pending=null;previous?.focus?.focus();});
 accept?.addEventListener('click',()=>{
  if(!pending)return;
  const {form,button}=pending;
  pending=null;dialog.close();
  if(!form.reportValidity()){button?.focus();return;}
  approved.add(form);button?form.requestSubmit(button):form.requestSubmit();
 });
 // Back/forward cache must not leave a previously submitted form disabled.
 window.addEventListener('pageshow',event=>{if(event.persisted)location.reload();});
 document.querySelectorAll('form[data-unit-price]').forEach(form=>{
  const quantity=form.querySelector('[name="People"]'),output=form.querySelector('[data-estimate]');
  // Lưu trú (data-per-night): nhân thêm số đêm, giống MarketplaceManager.EstimateTotal ở server.
  const nights=()=>{const s=form.elements.Date?.value,e=form.elements.EndDate?.value;if(form.dataset.perNight!=='true'||!s||!e)return 1;const d=(new Date(e)-new Date(s))/864e5;return d>0?d:1;};
  const update=()=>{if(output)output.textContent=new Intl.NumberFormat('vi-VN').format(Number(form.dataset.unitPrice)*Number(quantity?.value||0)*nights())+' đ';};
  quantity?.addEventListener('input',update);form.addEventListener('change',update);update();
 });
})();
// A day shortcut prepares the editor without writing anything to the server.
document.addEventListener('click', event => {
 const dayTab=event.target.closest?.('[data-workspace-day]');
 if(dayTab){
  const container=dayTab.closest('.active-trip');
  if(container){
   container.querySelectorAll('[data-workspace-day]').forEach(tab=>{
    const selected=tab===dayTab;
    tab.classList.toggle('selected',selected);
    tab.setAttribute('aria-selected',String(selected));
    tab.tabIndex=selected?0:-1;
   });
   container.querySelectorAll('.workspace-day-panel').forEach(panel=>panel.hidden=panel.id!=='workspace-day-'+dayTab.dataset.workspaceDay);
  }
 }
 const dayLink=event.target.closest?.('[data-activity-day]');
 if(dayLink){
  const form=document.querySelector('[data-testid="activity-form-0"]');
  if(form){
   const day=dayLink.dataset.activityDay;
   form.elements.Start.value=day+'T08:00';
   form.elements.End.value=day+'T09:00';
  }
 }
 const tab=event.target.closest?.('.day-navigation a');
 if(tab){
  document.querySelectorAll('.day-navigation a').forEach(link=>link.classList.toggle('first-day',link===tab));
 }
});
document.addEventListener('keydown',event=>{
 const tab=event.target.closest?.('[data-workspace-day]');
 if(!tab||!['ArrowLeft','ArrowRight','Home','End'].includes(event.key))return;
 const tabs=Array.from(tab.parentElement.querySelectorAll('[data-workspace-day]'));
 const current=tabs.indexOf(tab);
 const index=event.key==='Home'?0:event.key==='End'?tabs.length-1:(current+(event.key==='ArrowRight'?1:-1)+tabs.length)%tabs.length;
 event.preventDefault();tabs[index].click();tabs[index].focus();
});

// Form thêm hoạt động: chọn địa điểm (ô 1) trước, ô 2 chỉ hiện hoạt động/dịch vụ của địa điểm đó.
// Mỗi mục ở ô 2 có data-places = mã các địa điểm nó phục vụ (tính ở server bằng DestinationFilter.ServesPlace).
function filterActivityOptions(form,pickFirst){
 const place=form.querySelector('[data-place-filter]');
 const select=form.elements.SourceSelection;
 if(!place||!select)return;
 select.querySelectorAll('option').forEach(option=>{
  const places=(option.dataset.places||'').split(',');
  // Luôn giữ "Tự nhập" và mục đang được chọn sẵn khi sửa hoạt động cũ.
  option.hidden=!(option.value==='Manual'||option.defaultSelected||(place.value!==''&&places.includes(place.value)));
 });
 select.querySelectorAll('optgroup').forEach(group=>group.hidden=Array.from(group.children).every(option=>option.hidden));
 if(pickFirst){
  const first=Array.from(select.options).find(option=>!option.hidden&&option.value!=='Manual');
  select.value=first&&place.value!==''?first.value:'Manual';
  // Điền sẵn ô "Địa điểm / địa chỉ" nếu khách chưa tự gõ.
  const location=form.elements.Location;
  if(location&&(location.value===''||location.dataset.auto==='1')){
   location.value=place.value===''?'':place.options[place.selectedIndex].text;
   location.dataset.auto='1';
  }
  fillActivityCost(form);
 }
}
// Chọn dịch vụ thì điền sẵn chi phí theo giá dịch vụ, nếu khách chưa tự nhập.
function fillActivityCost(form){
 const option=form.elements.SourceSelection.selectedOptions[0];
 const cost=form.elements.Cost;
 if(!cost||!(cost.value===''||cost.dataset.auto==='1'))return;
 cost.value=option?.dataset.price||'';
 cost.dataset.auto=cost.value===''?'':'1';
}
document.querySelectorAll('form').forEach(form=>filterActivityOptions(form,false));
document.addEventListener('change',event=>{
 const form=event.target.form;
 if(!form)return;
 if(event.target.matches('[data-place-filter]'))filterActivityOptions(form,true);
 else if(event.target.name==='SourceSelection')fillActivityCost(form);
 else if(event.target.name==='Location'||event.target.name==='Cost')event.target.dataset.auto='';
});
