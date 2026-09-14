(() => {
  const frame = document.getElementById('sceneFrame');
  if (!frame) return;

  const shield = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3 20 6v5c0 5.15-3.45 8.94-8 10-4.55-1.06-8-4.85-8-10V6l8-3Z"/><path d="m8.5 12 2.2 2.2 4.8-5"/></svg>';
  const style = `
    #lt2fa-card{position:absolute;left:196px;top:642px;width:350px;min-height:93px;z-index:9000;box-sizing:border-box;padding:12px 14px;background:linear-gradient(103deg,#fff9eb,#f3e6cd);border:1px solid #c9b28b;box-shadow:0 3px 7px rgba(72,47,22,.14);font-family:'Noto Serif TC','Microsoft JhengHei',serif;color:#514536;transform:rotate(-1.3deg);}
    #lt2fa-card::before{content:'';position:absolute;left:9px;right:9px;top:8px;border-top:1px dashed rgba(128,97,59,.33)}
    .lt2fa-row{display:flex;align-items:center;gap:9px}.lt2fa-icon{width:27px;height:27px;color:#a4512a;flex:none}.lt2fa-icon svg{width:100%;height:100%;fill:none;stroke:currentColor;stroke-width:1.7;stroke-linecap:round;stroke-linejoin:round}.lt2fa-title{font-size:14px;font-weight:700;letter-spacing:.08em}.lt2fa-note{margin-top:1px;font:10px/1.4 'IBM Plex Mono','Microsoft JhengHei',monospace;color:#9b8464}.lt2fa-state{margin-left:auto;padding:3px 6px;border:1px solid #dbc8a7;background:#fffaf0;font:10px/1 'IBM Plex Mono',monospace;color:#a9512c;white-space:nowrap}.lt2fa-state.is-on{color:#2d7563;border-color:#9ebcac;background:#eff7ed}.lt2fa-action{display:block;width:100%;min-height:34px;margin-top:9px;border:1px solid #ad5c35;background:#f9f0df;color:#8a4324;font:600 12px 'Noto Serif TC','Microsoft JhengHei',serif;cursor:pointer;transition:background .18s ease,color .18s ease}.lt2fa-action:hover{background:#a9562e;color:#fffaf0}.lt2fa-action:focus-visible,.lt2fa-close:focus-visible,.lt2fa-secondary:focus-visible{outline:3px solid #2f7d6b;outline-offset:2px}
    #lt2fa-modal{position:fixed;inset:0;z-index:10000;display:grid;place-items:center;background:rgba(48,32,18,.43);font-family:'Noto Serif TC','Microsoft JhengHei',serif}.lt2fa-sheet{position:relative;width:min(430px,calc(100vw - 34px));max-height:calc(100vh - 34px);overflow:auto;box-sizing:border-box;padding:28px 32px 25px;background:radial-gradient(circle at 9% 8%,#fffdf5,#f7ebd7 69%,#eeddbd);border:1px solid #c8af89;box-shadow:0 18px 48px rgba(28,15,6,.45);color:#4d4032}.lt2fa-sheet::after{content:'';position:absolute;inset:8px;border:1px solid rgba(152,116,72,.27);pointer-events:none}.lt2fa-close{position:absolute;right:16px;top:12px;border:0;background:transparent;color:#725a42;font-size:28px;line-height:1;cursor:pointer}.lt2fa-kicker{margin:0;color:#a35a37;font:700 10px/1.4 'IBM Plex Mono',monospace;letter-spacing:.15em}.lt2fa-sheet h2{margin:7px 0 5px;font-size:23px;line-height:1.35}.lt2fa-sheet p{margin:0;color:#786752;font-size:13px;line-height:1.7}.lt2fa-steps{display:flex;align-items:center;margin:19px 0 17px;color:#9a856b;font:10px 'IBM Plex Mono',monospace}.lt2fa-steps span{display:grid;place-items:center;width:22px;height:22px;border-radius:50%;border:1px solid #c5ac88;background:#fff8e9}.lt2fa-steps span:first-child{background:#a9562e;border-color:#a9562e;color:#fff}.lt2fa-steps i{height:1px;flex:1;background:#cfb997}.lt2fa-qr{display:block;width:166px;height:166px;margin:0 auto 12px;padding:8px;box-sizing:border-box;background:#fff;border:1px solid #d6c29f}.lt2fa-manual{margin:9px 0 15px;padding:8px 10px;background:#fff9ed;border:1px dashed #c8af89;font:10px/1.55 'IBM Plex Mono',monospace;word-break:break-all;color:#6c5842}.lt2fa-code-label{display:block;margin:13px 0 7px;font-size:12px;font-weight:700}.lt2fa-code{width:100%;height:44px;box-sizing:border-box;border:1px solid #b99771;background:#fffdf7;color:#473826;text-align:center;letter-spacing:.55em;font:700 20px 'IBM Plex Mono',monospace}.lt2fa-primary{width:100%;min-height:44px;margin-top:16px;border:0;background:#a9562e;color:#fffdf7;font:700 14px 'Noto Serif TC','Microsoft JhengHei',serif;cursor:pointer}.lt2fa-primary:disabled{opacity:.6;cursor:wait}.lt2fa-secondary{display:block;margin:11px auto 0;border:0;background:transparent;color:#786753;text-decoration:underline;font:12px 'Noto Serif TC','Microsoft JhengHei',serif;cursor:pointer}.lt2fa-message{min-height:20px;margin-top:9px;text-align:center;color:#a1422e;font-size:12px;line-height:1.5}.lt2fa-message.ok{color:#28725d}@media (prefers-reduced-motion:reduce){.lt2fa-action{transition:none}}
    /* It lives inside the existing book transform, so desk and flat-reading views move together. */
    #lt2fa-card{left:24px;top:392px;transform:none;transform-origin:0 0;transition:opacity .18s ease}
  `;

  let token = '';
  let setupId = '';
  const request = async (url, data) => {
    if (!token) { const tokenResponse = await fetch('/Members/CsrfToken', { credentials: 'same-origin' }); token = (await tokenResponse.json()).token; }
    const response = await fetch(url, { method: data ? 'POST' : 'GET', credentials: 'same-origin', headers: data ? { 'RequestVerificationToken': token, 'Content-Type': 'application/x-www-form-urlencoded' } : {}, body: data ? new URLSearchParams(data) : undefined });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(payload.message || '操作失敗，請稍後再試。');
    return payload;
  };

  function closeModal(doc) { doc.getElementById('lt2fa-modal')?.remove(); }
  function findJournal(doc) {
    return [...doc.querySelectorAll('div')].find(element =>
      element.style.position === 'absolute' && element.style.left === '132px' &&
      element.style.top === '266px' && element.style.width === '900px' && element.style.height === '600px');
  }
  function modal(doc, state) {
    closeModal(doc);
    const root = doc.createElement('div'); root.id = 'lt2fa-modal'; root.setAttribute('role', 'dialog'); root.setAttribute('aria-modal', 'true'); root.setAttribute('aria-labelledby', 'lt2fa-modal-title');
    root.innerHTML = `<section class="lt2fa-sheet"><button class="lt2fa-close" type="button" aria-label="關閉設定">×</button><h2 id="lt2fa-modal-title">設定 Google Authenticator</h2><p>掃描 QR Code 後輸入 App 顯示的 6 位驗證碼，才會正式開啟兩步驟驗證。</p><div class="lt2fa-steps"><span>1</span><i></i><span>2</span><i></i><span>3</span></div><img class="lt2fa-qr" alt="Google Authenticator 設定 QR Code" src="${state.qrCodeDataUri}"><label class="lt2fa-code-label" for="lt2fa-code">Google Authenticator 驗證碼</label><input id="lt2fa-code" class="lt2fa-code" inputmode="numeric" autocomplete="one-time-code" maxlength="6" pattern="[0-9]{6}" aria-describedby="lt2fa-message"><button type="button" class="lt2fa-primary">確認並啟用 2FA</button><p id="lt2fa-message" class="lt2fa-message" aria-live="polite"></p><button type="button" class="lt2fa-secondary">暫時不要，稍後再說</button></section>`;
    root.addEventListener('click', event => { if (event.target === root) closeModal(doc); });
    root.querySelector('.lt2fa-close').addEventListener('click', () => closeModal(doc)); root.querySelector('.lt2fa-secondary').addEventListener('click', () => closeModal(doc));
    root.querySelector('.lt2fa-primary').addEventListener('click', async event => { const btn = event.currentTarget; const message = root.querySelector('#lt2fa-message'); const code = root.querySelector('#lt2fa-code').value.replace(/\D/g, ''); if (code.length !== 6) { message.textContent = '請輸入 6 位數驗證碼。'; return; } btn.disabled = true; message.textContent = '正在驗證…'; try { const result = await request('/Auth/Authenticator/Confirm', { setupId, code }); message.className = 'lt2fa-message ok'; message.textContent = result.message; setTimeout(() => { closeModal(doc); renderCard(doc, true); }, 850); } catch (error) { message.textContent = error.message; btn.disabled = false; } });
    doc.body.append(root); root.querySelector('#lt2fa-code').focus();
  }

  function renderCard(doc, enabled) {
    doc.getElementById('lt2fa-card')?.remove(); const card = doc.createElement('aside'); card.id = 'lt2fa-card';
    card.innerHTML = `<div class="lt2fa-row"><span class="lt2fa-icon">${shield}</span><div><div class="lt2fa-title">帳戶安全 · 2FA</div><div class="lt2fa-note">Google Authenticator</div></div><span class="lt2fa-state ${enabled ? 'is-on' : ''}">${enabled ? '已啟用' : '尚未啟用'}</span></div>${enabled ? '<div class="lt2fa-note" style="margin-top:9px">重設密碼時，系統會要求 6 位驗證碼。</div>' : '<button class="lt2fa-action" type="button">設定 Google Authenticator</button>'}`;
    if (!enabled) card.querySelector('button').addEventListener('click', async () => { const button = card.querySelector('button'); button.disabled = true; button.textContent = '正在產生 QR Code…'; try { const result = await request('/Auth/Authenticator/Start', {}); setupId = result.setupId; modal(doc, result); } catch (error) { button.disabled = false; button.textContent = error.message; } });
    (findJournal(doc) || doc.body).append(card);
  }

  frame.addEventListener('load', async () => {
    try { const doc = frame.contentDocument; if (!doc || doc.getElementById('lt2fa-overlay-style')) return; const tag = doc.createElement('style'); tag.id = 'lt2fa-overlay-style'; tag.textContent = style; doc.head.append(tag); const status = await request('/Auth/Authenticator/Status'); renderCard(doc, status.enabled); } catch { /* visitors and non-signed-in profiles must not expose an account-security control */ }
  });
})();
