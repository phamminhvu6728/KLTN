// Core smoke checks; fuller business checks are in the three module test scripts.
const base=process.env.BASE_URL||'http://localhost:5080';
for (const path of ['/','/Home/Place/1','/Home/Search','/Marketplace','/Community','/Account/Login','/Account/Register']) {
 const response=await fetch(base+path);if(response.status!==200)throw new Error(`${path}: HTTP ${response.status}`);console.log(`PASS GET ${path}`);
}
for(const path of ['/Journey','/Dashboard','/Provider','/Admin']) {
 const response=await fetch(base+path,{redirect:'manual'});if(response.status!==302)throw new Error(`Private route not protected: ${path}`);console.log(`PASS protected ${path}`);
}
