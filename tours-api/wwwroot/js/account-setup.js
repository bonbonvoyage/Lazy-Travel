(() => {
'use strict';
const root=document.getElementById('ltSetup');if(!root)return;
const $=id=>document.getElementById(id);
let dirty=false,avatarUrl=null;
const dimensions=[
 {key:'planning',label:'行程規劃',left:'隨機應變',right:'按照計畫',icon:'M3 5l6-2 6 2 6-2v16l-6 2-6-2-6 2z M9 3v16 M15 5v16'},
 {key:'budget',label:'消費價值觀',left:'精打細算',right:'奢華享受',icon:'M4 6c0-4 16-4 16 0s-16 4-16 0v12c0 4 16 4 16 0V6 M4 12c0 4 16 4 16 0'},
 {key:'scenery',label:'景點偏好',left:'擁抱山海',right:'都會霓虹',icon:'M2 20L9 5l6 15 M11 12l4-7 7 15H2 M6 12h6'},
 {key:'food',label:'飲食追求',left:'巷弄尋寶',right:'指標名店',icon:'M4 3v6c0 4 6 4 6 0V3 M7 3v19 M18 3v19 M18 3c-5 4-5 10 0 10'}
];
const scores=new Map();
for(const d of dimensions){
 const details=document.createElement('details');details.className='lt-dna-item';
 details.innerHTML='<summary><svg viewBox="0 0 24 24" aria-hidden="true"><path d="'+d.icon+'"/></svg><strong>'+d.label+'</strong><span class="lt-dna-state" data-state="'+d.key+'">尚未選擇</span></summary><div class="lt-dna-choice"><fieldset><legend>選擇更接近你的旅行方式</legend><div class="lt-choice-row"></div></fieldset><div class="lt-choice-labels"><span>'+d.left+'</span><span>'+d.right+'</span></div><button class="lt-reset" type="button">清除此項選擇</button></div>';
 const row=details.querySelector('.lt-choice-row');
 [0,25,50,75,100].forEach((value,i)=>{
 const label=document.createElement('label');label.className='lt-choice';
 const radio=document.createElement('input');radio.type='radio';radio.name=d.key;radio.value=String(value);
 const caption=[d.left,'偏向'+d.left,'兩者皆可','偏向'+d.right,d.right][i];
 radio.setAttribute('aria-label',caption);
 const span=document.createElement('span');span.textContent=String(i+1);
 label.append(radio,span);row.append(label);
 radio.addEventListener('change',()=>{scores.set(d.key,{score:value,label:caption});dirty=true;details.querySelector('.lt-dna-state').textContent=caption;renderDna();});
 });
 details.querySelector('.lt-reset').addEventListener('click',()=>{details.querySelectorAll('input').forEach(x=>x.checked=false);scores.delete(d.key);details.querySelector('.lt-dna-state').textContent='尚未選擇';dirty=true;renderDna();});
 $('ltDnaFields').append(details);
}
function renderDna(){
 $('ltDnaPreview').replaceChildren();
 dimensions.forEach(d=>{const row=document.createElement('div');row.className='lt-dna-preview-row';const label=document.createElement('strong');label.textContent=d.label;const value=document.createElement('span');value.textContent=scores.get(d.key)?.label || '尚未設定';row.append(label,value);$('ltDnaPreview').append(row);});
}
function step(number){
 root.querySelectorAll('[data-step]').forEach(x=>x.hidden=Number(x.dataset.step)!==number);
 root.querySelectorAll('[data-step-marker]').forEach(x=>{if(Number(x.dataset.stepMarker)===number)x.setAttribute('aria-current','step');else x.removeAttribute('aria-current');});
 $('ltDnaPreview').hidden=number!==2;
 if(number===2)renderDna();
 root.querySelector('[data-step="'+number+'"] h1').focus();
 root.scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth',block:'start'});
}
$('ltAboutForm').addEventListener('submit',e=>{e.preventDefault();$('ltNickname').setCustomValidity($('ltNickname').value.trim()?'':'請填寫暱稱。');if(e.currentTarget.reportValidity())step(2);});
$('ltNickname').addEventListener('input',()=>{$('ltNickname').setCustomValidity('');$('ltNamePreview').textContent=$('ltNickname').value.trim()||'你的暱稱';});
$('ltMbti').addEventListener('change',()=>{$('ltMbtiPreview').textContent=$('ltMbti').value;$('ltMbtiPreview').hidden=!$('ltMbti').value;});
$('ltBio').addEventListener('input',()=>{$('ltBioPreview').textContent=$('ltBio').value||'在這裡，收藏你的旅行故事。';$('ltBioCount').textContent=String($('ltBio').value.length);});
const today=new Date();$('ltBirthday').max=today.getFullYear()+'-'+String(today.getMonth()+1).padStart(2,'0')+'-'+String(today.getDate()).padStart(2,'0');
root.addEventListener('input',()=>dirty=true);
$('ltAvatarInput').addEventListener('change',async e=>{
 const file=e.target.files?.[0];$('ltAvatarError').textContent='';if(!file)return;
 if(!['image/jpeg','image/png','image/webp'].includes(file.type)||file.size>5*1024*1024){$('ltAvatarError').textContent='請選擇 5 MB 以內的 JPG、PNG 或 WebP 圖片。';e.target.value='';return;}
 const nextUrl=URL.createObjectURL(file),probe=new Image();
 try{probe.src=nextUrl;await probe.decode();if(avatarUrl)URL.revokeObjectURL(avatarUrl);avatarUrl=nextUrl;$('ltAvatarPreview').src=nextUrl;$('ltAvatarPreview').hidden=false;$('ltAvatarPlaceholder').hidden=true;}
 catch{URL.revokeObjectURL(nextUrl);$('ltAvatarError').textContent='無法讀取這張圖片，請換一張再試。';e.target.value='';}
});
$('ltPrevious').addEventListener('click',()=>step(1));
const complete=$('ltCompleteDialog');
$('ltFinish').addEventListener('click',()=>complete.showModal());
$('ltSkip').addEventListener('click',()=>complete.showModal());
$('ltContinueEditing').addEventListener('click',()=>complete.close());
$('ltGoProfile').addEventListener('click',()=>dirty=false);
window.addEventListener('beforeunload',e=>{if(dirty){e.preventDefault();e.returnValue='';}});
window.addEventListener('pagehide',()=>{if(avatarUrl)URL.revokeObjectURL(avatarUrl);});
})();