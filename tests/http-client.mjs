export const base=process.env.DEMO_URL||'http://localhost:5080';
export const decode=s=>s.replace(/&#x([\da-f]+);/gi,(_,n)=>String.fromCodePoint(parseInt(n,16))).replace(/&#(\d+);/g,(_,n)=>String.fromCodePoint(+n)).replaceAll('&amp;','&').replaceAll('&quot;','"').replaceAll('&lt;','<').replaceAll('&gt;','>');
export const forms=html=>[...html.matchAll(/<form\b[^>]*>[\s\S]*?<\/form>/gi)].map(m=>m[0]);
export const formFor=(html,action)=>forms(html).find(f=>f.includes('action="'+action+'"'))||'';
export const field=(html,name)=>decode(html.match(new RegExp('name="'+name+'"[^>]*value="([^"]*)"','i'))?.[1]||'');
export function hidden(form){return Object.fromEntries([...form.matchAll(/<input\b[^>]*type="hidden"[^>]*>/gi)].map(m=>[m[0].match(/name="([^"]*)"/)?.[1],decode(m[0].match(/value="([^"]*)"/)?.[1]||'')]).filter(([k])=>k));}
export class Client{
 cookies={};
 async request(path,data){
  let r;
  for(let attempt=0;attempt<16;attempt++){
   r=await fetch(base+path,{method:data?'POST':'GET',redirect:'manual',headers:{Cookie:Object.entries(this.cookies).map(([k,v])=>k+'='+v).join('; '),...(data?{'Content-Type':'application/x-www-form-urlencoded'}:{})},body:data?new URLSearchParams(data):undefined});
   if(r.status!==429)break;
   await new Promise(resolve=>setTimeout(resolve,4000));
  }
  for(const c of r.headers.getSetCookie()){const raw=c.split(';')[0],at=raw.indexOf('=');this.cookies[raw.slice(0,at)]=raw.slice(at+1);}
  return{status:r.status,html:decode(await r.text()),location:r.headers.get('location'),errorCode:r.headers.get('X-Business-Error')};
 }
 async get(path){const r=await this.request(path);if(r.status!==200)throw Error('GET '+path+' HTTP '+r.status);return r.html;}
 async post(path,data,source){const html=source.startsWith('/')?await this.get(source):source;return this.request(path,{...data,__RequestVerificationToken:field(html,'__RequestVerificationToken')});}
}
