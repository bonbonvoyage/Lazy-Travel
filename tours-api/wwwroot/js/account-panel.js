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
dialog.querySelectorAll('[data-auth-switch]').forEach(b=>b.addEventListener('click',()=>{
 if(b.dataset.authSwitch==='recovery') resetRecovery();
 switchPane(b.dataset.authSwitch);
}));
dialog.addEventListener('click',e=>{
 const b=e.target.closest('[data-toggle-password]');if(!b)return;
 const input=document.getElementById(b.dataset.togglePassword);if(!input)return;input.dataset.secret='true';
 const visible=input.type==='password';input.type=visible?'text':'password';b.textContent=visible?'隱藏':'顯示';b.setAttribute('aria-label',visible?'隱藏密碼':'顯示密碼');
});
const register=document.getElementById('ltRegisterForm');
const password=document.getElementById('ltRegisterPassword'),confirm=document.getElementById('ltConfirmPassword');
function validateMatch(){confirm.setCustomValidity(confirm.value&&confirm.value!==password.value?'兩次輸入的密碼不一致。':'');}
function updatePasswordRules(){
 if(!password)return;
 const value=password.value;
 const rules={upper:/[A-Z]/.test(value),lower:/[a-z]/.test(value),number:/[0-9]/.test(value)};
 document.querySelectorAll('[data-password-rule]').forEach(rule=>{
  const met=rules[rule.dataset.passwordRule]===true;
  rule.classList.toggle('is-met',met);
  rule.setAttribute('aria-label',`${met?'已符合':'尚未符合'}：${rule.textContent.trim()}`);
 });
}
password?.addEventListener('input',()=>{validateMatch();updatePasswordRules();});confirm?.addEventListener('input',validateMatch);
const demoNameInput=register?.querySelector('[name="Name"]');
demoNameInput?.addEventListener('keydown',event=>{
 if(register.dataset.demoAutofill!=='true'||event.key!=='Tab'||event.shiftKey||demoNameInput.value.trim())return;
 event.preventDefault();
 const suffix=Math.floor(1000+Math.random()*9000);
 const names=['小旅','晴晴','阿山','悠遊','米米','若安'];
 const generatedPassword=`Travel${suffix}Ab`;
 demoNameInput.value=names[Math.floor(Math.random()*names.length)];
 register.querySelector('[name="email"]').value=`demo${Date.now()}${suffix}@example.com`;
 password.value=generatedPassword;
 confirm.value=generatedPassword;
 [demoNameInput,password,confirm].forEach(input=>input.dispatchEvent(new Event('input',{bubbles:true})));
 register.querySelector('[type="submit"]')?.focus();
});
register?.addEventListener('submit',async e=>{
 e.preventDefault();validateMatch();if(!register.reportValidity())return;
 const button=register.querySelector('[type=submit]'),error=document.getElementById('ltRegisterError');
 if(button.disabled)return;button.disabled=true;button.textContent='建立中…';error.textContent='';
 try {
  const response=await fetch(register.action,{method:'POST',body:new FormData(register),credentials:'same-origin'});
  const data=await response.json().catch(()=>({}));
  if(!response.ok){error.textContent=data.message||'目前無法建立帳號，請稍後再試。';return;}
  resetSecrets();window.location.assign(data.redirectUrl||'/Members/Profile');
 }catch{error.textContent='連線中斷。若帳號已建立，請嘗試登入；不用重新填寫個人資料。';}
 finally{button.disabled=false;button.textContent='建立帳號';}
});
document.getElementById('ltLoginForm')?.addEventListener('submit',async e=>{
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
document.getElementById('ltLogoutForm')?.addEventListener('submit',async e=>{
 e.preventDefault();const form=e.currentTarget,button=form.querySelector('[type=submit]'),error=document.getElementById('ltLogoutError');
 if(button.disabled)return;button.disabled=true;button.textContent='正在登出…';error.textContent='';
 try{
  const response=await fetch(form.action,{method:'POST',body:new FormData(form),credentials:'same-origin',headers:{Accept:'application/json'}});
  if(!response.ok){error.textContent='登出失敗，請重新整理後再試。';return;}
  location.assign('/');
 }catch{error.textContent='目前無法連線，請稍後再試。';}
 finally{button.disabled=false;button.textContent='登出此帳號';}
});
const recoveryForm=document.getElementById('ltRecoveryForm');
const recoveryFields=document.getElementById('ltRecoveryFields');
const recoveryTitle=document.getElementById('ltRecoveryTitle');
const recoveryKicker=document.getElementById('ltRecoveryKicker');
const recoveryDescription=document.getElementById('ltRecoveryDescription');
const recoveryHelp=document.getElementById('ltRecoveryHelp');
const recoveryError=document.getElementById('ltRecoveryError');
const recoverySubmit=document.getElementById('ltRecoverySubmit');
const recoveryProgress=[...dialog.querySelectorAll('.lt-recovery-progress span')];
const recoveryState={stage:'email',requestId:''};
const recoveryScreens={
 email:{kicker:'ACCOUNT SECURITY · 1 / 3',title:'驗證你的身分',description:'輸入註冊信箱，我們會寄送一次性驗證碼，作為重設密碼的身分驗證。',help:'驗證碼將在 10 分鐘後失效；系統不會顯示或寄送你的舊密碼。',button:'寄送驗證碼',action:'/Auth/PasswordRecovery/RequestCode',fields:'<label class="lt-field">電子信箱<input name="Email" type="email" autocomplete="email" placeholder="name@example.com" required maxlength="254" /></label>',progress:1},
 emailOtp:{kicker:'ACCOUNT SECURITY · 2 / 3',title:'輸入信箱驗證碼',description:'請輸入寄至你註冊信箱的 6 位一次性驗證碼。',help:'未收到信件？請確認垃圾郵件，或返回上一步重新寄送。',button:'驗證並繼續',action:'/Auth/PasswordRecovery/VerifyEmailCode',fields:'<label class="lt-field">6 位驗證碼</label><div class="lt-recovery-code" data-otp-inputs><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 1 位" required><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 2 位" required><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 3 位" required><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 4 位" required><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 5 位" required><input inputmode="numeric" maxlength="1" aria-label="驗證碼第 6 位" required></div>',progress:2},
 password:{kicker:'ACCOUNT SECURITY · 3 / 3',title:'設定新密碼',description:'身分驗證完成。請設定新密碼以重新登入。',help:'至少 8 個字元；完成後會更新 Security Stamp，讓其他裝置上的舊登入失效。',button:'更新密碼並重新登入',action:'/Auth/PasswordRecovery/ResetPassword',fields:'<label class="lt-field">新密碼<span class="lt-password"><input id="ltRecoveryPassword" name="Password" type="password" autocomplete="new-password" required minlength="8" maxlength="128"><button type="button" data-toggle-password="ltRecoveryPassword" aria-label="顯示新密碼">顯示</button></span></label><label class="lt-field">確認新密碼<span class="lt-password"><input id="ltRecoveryConfirmPassword" name="ConfirmPassword" type="password" autocomplete="new-password" required minlength="8" maxlength="128"><button type="button" data-toggle-password="ltRecoveryConfirmPassword" aria-label="顯示確認新密碼">顯示</button></span></label>',progress:3},
 complete:{kicker:'ACCOUNT SECURITY · COMPLETE',title:'密碼已更新',description:'你的密碼已安全更新，其他裝置上的舊登入已失效。請使用新密碼重新登入。',help:'',button:'返回登入',action:'',fields:'<div class="lt-recovery-complete" aria-hidden="true">✓</div>',progress:3}
};
function resetRecovery(){recoveryState.stage='email';recoveryState.requestId='';renderRecovery();}
function renderRecovery(){
 if(!recoveryForm)return;const screen=recoveryScreens[recoveryState.stage];
 recoveryKicker.textContent=screen.kicker;recoveryTitle.textContent=screen.title;recoveryDescription.textContent=screen.description;recoveryHelp.textContent=screen.help;recoverySubmit.textContent=screen.button;recoveryForm.action=screen.action||'#';recoveryFields.innerHTML=screen.fields;recoveryError.textContent='';
 recoveryProgress.forEach((s,index)=>s.classList.toggle('is-active',index<screen.progress));
 recoveryHelp.hidden=recoveryState.stage==='complete';
 recoveryFields.querySelectorAll('[data-otp-inputs] input').forEach((input,index,inputs)=>input.addEventListener('input',()=>{input.value=input.value.replace(/\D/g,'').slice(0,1);if(input.value&&index<inputs.length-1)inputs[index+1].focus();}));
}
function recoveryOtp(){return [...recoveryFields.querySelectorAll('[data-otp-inputs] input')].map(x=>x.value).join('');}
function recoveryData(){const data=new FormData(recoveryForm);if(recoveryState.requestId)data.set('RequestId',recoveryState.requestId);if(recoveryState.stage==='emailOtp')data.set('Code',recoveryOtp());return data;}
recoveryForm?.addEventListener('submit',async e=>{
 e.preventDefault();const screen=recoveryScreens[recoveryState.stage];
 if(recoveryState.stage==='complete'){switchPane('login');return;}
 if(recoveryState.stage==='password'){
  const p=recoveryFields.querySelector('[name=Password]'),c=recoveryFields.querySelector('[name=ConfirmPassword]');
  if(p.value!==c.value){recoveryError.textContent='兩次輸入的新密碼不一致。';c.focus();return;}
 }
 if(!recoveryForm.reportValidity())return;
 recoverySubmit.disabled=true;recoverySubmit.textContent='處理中…';recoveryError.textContent='';
 try{
  const response=await fetch(screen.action,{method:'POST',body:recoveryData(),credentials:'same-origin',headers:{Accept:'application/json'}});
  const data=await response.json().catch(()=>({}));
  if(!response.ok){recoveryError.textContent=data.message||'驗證失敗，請再試一次。';return;}
  if(recoveryState.stage==='email'){
   if(!data.requestId){recoveryError.textContent=data.message||'若此信箱已註冊，驗證碼將寄送至該信箱。';return;}
   recoveryState.requestId=data.requestId;recoveryState.stage='emailOtp';renderRecovery();
   if(data.demoCode){const note=document.createElement('p');note.className='lt-recovery-demo';note.textContent=`開發預覽驗證碼：${data.demoCode}`;recoveryForm.insertBefore(note,recoveryFields);}
  }else if(recoveryState.stage==='emailOtp'){recoveryState.stage='password';renderRecovery();}
  else if(recoveryState.stage==='password'){recoveryState.stage='complete';renderRecovery();}
 }catch{recoveryError.textContent='目前無法連線，請稍後再試。';}
 finally{recoverySubmit.disabled=false;if(recoveryState.stage!=='complete')recoverySubmit.textContent=recoveryScreens[recoveryState.stage].button;}
});
renderRecovery();
const authenticatorForm=document.getElementById('ltAuthenticatorForm');
const authenticatorSetup=document.getElementById('ltAuthenticatorSetup');
const authenticatorStatus=document.getElementById('ltAuthenticatorStatus');
const authenticatorIntro=document.getElementById('ltAuthenticatorIntro');
const authenticatorSubmit=document.getElementById('ltAuthenticatorSubmit');
const authenticatorError=document.getElementById('ltAuthenticatorError');
const authenticatorSetupId=document.getElementById('ltAuthenticatorSetupId');
const authenticatorCode=document.getElementById('ltAuthenticatorCode');
const authenticatorQr=document.getElementById('ltAuthenticatorQr');
const authenticatorManualKey=document.getElementById('ltAuthenticatorManualKey');
const authenticatorToken=authenticatorForm?.querySelector('input[name="__RequestVerificationToken"]');
function authenticatorData(){const data=new FormData();if(authenticatorToken)data.set('__RequestVerificationToken',authenticatorToken.value);return data;}
function setAuthenticatorEnabled(){authenticatorIntro.textContent='此帳號已啟用登入雙重驗證。';authenticatorStatus.innerHTML='<span class="lt-security-icon" aria-hidden="true">✓</span><div><strong>已啟用</strong><p>登入時需使用驗證器 App 顯示的 6 位驗證碼。</p></div>';authenticatorSetup.hidden=true;authenticatorSubmit.disabled=true;authenticatorSubmit.textContent='已啟用';}
async function loadAuthenticatorStatus(){
 if(!authenticatorForm)return;
 try{const response=await fetch('/Auth/Authenticator/Status',{credentials:'same-origin',headers:{Accept:'application/json'}});const data=await response.json();if(response.ok&&data.enabled)setAuthenticatorEnabled();}catch{}
}
document.querySelectorAll('[data-auth-switch="two-factor"]').forEach(button=>button.addEventListener('click',loadAuthenticatorStatus));
document.getElementById('ltManualKeyToggle')?.addEventListener('click',()=>{authenticatorManualKey.hidden=!authenticatorManualKey.hidden;});
authenticatorCode?.addEventListener('input',()=>{authenticatorCode.value=authenticatorCode.value.replace(/\D/g,'').slice(0,6);});
authenticatorForm?.addEventListener('submit',async event=>{
 event.preventDefault();if(authenticatorSubmit.disabled)return;authenticatorError.textContent='';authenticatorSubmit.disabled=true;
 const confirming=!authenticatorSetup.hidden;
 authenticatorSubmit.textContent=confirming?'驗證中…':'產生 QR Code…';
 try{
  const data=authenticatorData();let endpoint='/Auth/Authenticator/Start';
  if(confirming){if(!/^[0-9]{6}$/.test(authenticatorCode.value)){authenticatorError.textContent='請輸入驗證器 App 顯示的 6 位數字。';authenticatorCode.focus();return;}endpoint='/Auth/Authenticator/Confirm';data.set('SetupId',authenticatorSetupId.value);data.set('Code',authenticatorCode.value);}
  const response=await fetch(endpoint,{method:'POST',body:data,credentials:'same-origin',headers:{Accept:'application/json'}});const body=await response.json().catch(()=>({}));
  if(!response.ok){authenticatorError.textContent=body.message||'目前無法完成設定，請稍後再試。';return;}
  if(!confirming){authenticatorSetupId.value=body.setupId;authenticatorQr.src=body.qrCodeDataUri;authenticatorManualKey.textContent=body.manualKey;authenticatorSetup.hidden=false;authenticatorSubmit.textContent='驗證並啟用';authenticatorCode.focus();}
  else{setAuthenticatorEnabled();}
 }catch{authenticatorError.textContent='目前無法連線，請稍後再試。';}
 finally{if(authenticatorSubmit.textContent!=='已啟用'){authenticatorSubmit.disabled=false;authenticatorSubmit.textContent=authenticatorSetup.hidden?'開始設定':'驗證並啟用';}}
});
const query=new URLSearchParams(location.search);const authError=query.get('authError');
if(authError){open('login');document.getElementById('ltLoginError').textContent=authError;history.replaceState({},'',location.pathname+location.hash);}
if(document.body.hasAttribute('data-register-page'))open('register');
})();
