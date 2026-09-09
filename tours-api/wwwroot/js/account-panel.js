(() => {
'use strict';
const dialog = document.getElementById('ltAuthDialog');
if (!dialog || dialog.dataset.ready) return;
dialog.dataset.ready = 'true';
let trigger;
const panes = [...dialog.querySelectorAll('[data-auth-pane]')];
const resetSecrets = () => dialog.querySelectorAll('input[type=password], input[data-secret]').forEach(x => { x.value=''; x.type='password'; });
function switchPane(mode) {
 panes.forEach(p=>p.hidden=p.dataset.authPane!==mode);
 const heading=panes.find(p=>!p.hidden)?.querySelector('h2');
 panes.forEach(p=>p.querySelector('h2')?.removeAttribute('id'));
 if(heading) heading.id='ltAuthTitle';
 dialog.querySelectorAll('.lt-error').forEach(x=>x.textContent='');
 resetSecrets();
 requestAnimationFrame(()=>panes.find(p=>!p.hidden)?.querySelector('input, a, button')?.focus());
}
function open(mode, button) {
 trigger=button || document.activeElement;
 switchPane(mode);
 if(!dialog.open) dialog.showModal();
}
document.addEventListener('click',e=>{
 const button=e.target.closest('[data-auth-open]');
 if(button) { e.preventDefault();open(button.dataset.authOpen || 'login',button); }
});
dialog.querySelector('[data-auth-close]').addEventListener('click',()=>dialog.close());
dialog.addEventListener('click',e=>{if(e.target===dialog){const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();}});
dialog.addEventListener('close',()=>{resetSecrets();trigger?.focus();});
dialog.querySelectorAll('[data-auth-switch]').forEach(b=>b.addEventListener('click',()=>switchPane(b.dataset.authSwitch)));
dialog.querySelectorAll('[data-toggle-password]').forEach(b=>b.addEventListener('click',()=>{
 const input=document.getElementById(b.dataset.togglePassword); input.dataset.secret='true';
 const visible=input.type==='password';input.type=visible?'text':'password';b.textContent=visible?'隱藏':'顯示';b.setAttribute('aria-label',visible?'隱藏密碼':'顯示密碼');
}));
const register=document.getElementById('ltRegisterForm');
const password=document.getElementById('ltRegisterPassword'),confirm=document.getElementById('ltConfirmPassword');
function validateMatch(){confirm.setCustomValidity(confirm.value&&confirm.value!==password.value?'兩次輸入的密碼不一致。':'');}
password.addEventListener('input',validateMatch);confirm.addEventListener('input',validateMatch);
register.addEventListener('submit',e=>{
 e.preventDefault();validateMatch();if(!register.reportValidity())return;
 resetSecrets();window.location.assign('/Account/Setup');
});
document.getElementById('ltLoginForm').addEventListener('submit',async e=>{
 e.preventDefault();const form=e.currentTarget,button=form.querySelector('[type=submit]'),error=document.getElementById('ltLoginError');
 if(!form.reportValidity())return;
 button.disabled=true;button.textContent='登入中…';error.textContent='';
 try{
 const response=await fetch(form.action,{method:'POST',body:new FormData(form),credentials:'same-origin',headers:{Accept:'application/json'}});
 const body=await response.text();let data;try{data=JSON.parse(body);}catch{data=null;}
 if(!response.ok){error.textContent=typeof data==='string'?data:'登入失敗，請確認帳號密碼，或重新整理後再試。';return;}
 resetSecrets();
 if(document.body.hasAttribute('data-register-page'))location.assign('/');else location.reload();
 }catch{error.textContent='目前無法連線，請稍後再試。';}
 finally{button.disabled=false;button.textContent='登入';}
});
if(document.body.hasAttribute('data-register-page'))open('register');
})();