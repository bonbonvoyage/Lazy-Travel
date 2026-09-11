// 首頁 Hero 旁邊的互動地球：WebGL 球體，貼的是用真實世界地圖資料（wwwroot/data/world.json，Natural Earth
// 公版地理資料）現場畫出來的「地形著色地圖」風格材質(色塊分明的地形圖，不是照片)，完全是自己產生的內容，
// 不是任何圖庫素材。DOM 圖釘標出目前可加入的行程（依 TravelGroups.Country 定位）。
// 行程資料獨立向 /Home/Data 拿一份（跟 home.js 拿卡片資料的請求分開，各自簡單、互不依賴）。

// 常見國家中文名 -> 概略經緯度，只需要涵蓋畫面上會出現的行程國家即可，不需要精確地理定位。
const COUNTRY_COORDS = {
  '台灣': [23.7, 121.0], '日本': [36.2, 138.3], '韓國': [36.5, 127.9],
  '中國': [35.0, 103.0], '香港': [22.3, 114.2], '泰國': [15.9, 100.9],
  '越南': [14.1, 108.3], '柬埔寨': [12.6, 104.9], '馬來西亞': [4.2, 101.9],
  '新加坡': [1.35, 103.8], '印尼': [-2.5, 118.0], '菲律賓': [12.9, 121.8],
  '印度': [21.0, 78.0], '義大利': [42.8, 12.8], '法國': [46.6, 2.2],
  '西班牙': [40.2, -3.7], '英國': [54.0, -2.0], '德國': [51.2, 10.5],
  '荷蘭': [52.1, 5.3], '瑞士': [46.8, 8.2], '希臘': [39.1, 21.8],
  '冰島': [64.9, -18.0], '挪威': [60.5, 8.5], '瑞典': [60.1, 18.6],
  '芬蘭': [61.9, 25.7], '葡萄牙': [39.4, -8.2], '土耳其': [38.9, 35.2],
  '美國': [39.8, -98.6], '加拿大': [56.1, -106.3], '墨西哥': [23.6, -102.5],
  '澳洲': [-25.3, 133.8], '紐西蘭': [-42.0, 172.8], '埃及': [26.8, 30.8],
  '摩洛哥': [31.8, -7.1], '南非': [-30.6, 22.9], '阿聯': [23.4, 53.8],
};

// 國家 -> 洲別，用來把「區域」篩選欄跟國家/地球連動。
const COUNTRY_CONTINENT = {
  '台灣': '亞洲', '日本': '亞洲', '韓國': '亞洲', '中國': '亞洲', '香港': '亞洲', '泰國': '亞洲',
  '越南': '亞洲', '柬埔寨': '亞洲', '馬來西亞': '亞洲', '新加坡': '亞洲', '印尼': '亞洲', '菲律賓': '亞洲',
  '印度': '亞洲', '土耳其': '亞洲', '阿聯': '亞洲',
  '義大利': '歐洲', '法國': '歐洲', '西班牙': '歐洲', '英國': '歐洲', '德國': '歐洲', '荷蘭': '歐洲',
  '瑞士': '歐洲', '希臘': '歐洲', '冰島': '歐洲', '挪威': '歐洲', '瑞典': '歐洲', '芬蘭': '歐洲', '葡萄牙': '歐洲',
  '美國': '美洲', '加拿大': '美洲', '墨西哥': '美洲',
  '澳洲': '大洋洲', '紐西蘭': '大洋洲',
  '埃及': '非洲', '摩洛哥': '非洲', '南非': '非洲',
};

const VERTEX_SRC = `
  attribute vec3 aPosition;
  attribute vec2 aUV;
  uniform float uRotY;
  uniform float uRotX;
  uniform float uScaleX;
  uniform float uScaleY;
  varying vec2 vUV;
  varying vec3 vNormal;
  void main() {
    vec3 p = aPosition;
    float cosY = cos(uRotY), sinY = sin(uRotY);
    float x1 = p.x * cosY + p.z * sinY;
    float z1 = -p.x * sinY + p.z * cosY;
    float y1 = p.y;
    float cosX = cos(uRotX), sinX = sin(uRotX);
    float y2 = y1 * cosX - z1 * sinX;
    float z2 = y1 * sinX + z1 * cosX;
    float x2 = x1;
    vNormal = vec3(x2, y2, z2);
    gl_Position = vec4(x2 * uScaleX, y2 * uScaleY, -z2, 1.0);
    vUV = aUV;
  }
`;

const FRAGMENT_SRC = `
  precision mediump float;
  uniform sampler2D uEarthTex;
  varying vec2 vUV;
  varying vec3 vNormal;
  void main() {
    vec3 n = normalize(vNormal);
    vec3 lightDir = normalize(vec3(-0.45, 0.35, 0.82));
    float diff = max(dot(n, lightDir), 0.0);
    // 提高底色亮度、壓低明暗落差，讓整顆球體均勻明亮，不要背光側暗成一大片陰影。
    float lighting = 0.78 + 0.35 * diff;

    // 材質本身顏色已經是手繪選好的鮮豔色階，這裡只做很輕微的對比微調，不用再疊加大量飽和度。
    // 不加高光反射、不加邊緣光圈，單純用明暗漸層表現球體立體感，維持插畫地圖平實的質感。
    vec3 texColor = texture2D(uEarthTex, vUV).rgb;
    vec3 color = clamp((texColor - 0.5) * 1.05 + 0.5, 0.0, 1.0) * lighting;

    gl_FragColor = vec4(color, 1.0);
  }
`;

function buildSphere(latBands, lngBands) {
  const positions = [];
  const uvs = [];
  const indices = [];
  for (let lat = 0; lat <= latBands; lat++) {
    const theta = (lat * Math.PI) / latBands;
    const sinTheta = Math.sin(theta), cosTheta = Math.cos(theta);
    for (let lng = 0; lng <= lngBands; lng++) {
      const phi = (lng * 2 * Math.PI) / lngBands - Math.PI;
      const sinPhi = Math.sin(phi), cosPhi = Math.cos(phi);
      positions.push(sinTheta * sinPhi, cosTheta, sinTheta * cosPhi);
      uvs.push(lng / lngBands, lat / latBands);
    }
  }
  for (let lat = 0; lat < latBands; lat++) {
    for (let lng = 0; lng < lngBands; lng++) {
      const a = lat * (lngBands + 1) + lng;
      const b = a + lngBands + 1;
      indices.push(a, b, a + 1, b, b + 1, a + 1);
    }
  }
  return { positions, uvs, indices };
}

function compileShader(gl, type, src) {
  const sh = gl.createShader(type);
  gl.shaderSource(sh, src);
  gl.compileShader(sh);
  if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS)) {
    console.error('地球 shader 編譯失敗', gl.getShaderInfoLog(sh));
    gl.deleteShader(sh);
    return null;
  }
  return sh;
}

// 大圈（點很多的國家，如加拿大、南極）取樣減少一點密度，畫起來才不會太吃效能；
// 小圈（島嶼）全保留，才不會讓台灣、冰島這種行程國家的島型消失。
function simplifyRing(ring) {
  if (ring.length <= 90) return ring;
  const stride = ring.length > 320 ? 3 : 2;
  const out = [];
  for (let i = 0; i < ring.length; i += stride) out.push(ring[i]);
  return out;
}

// 一塊小小的黑白顆粒雜訊貼片，之後用 pattern 重複貼滿陸地當作淡淡的山脈紋理，
// 不是用陰影堆疊做立體，是插畫地圖常見的那種細顆粒質感。
function buildNoisePattern(ctx) {
  const size = 48;
  const nc = document.createElement('canvas');
  nc.width = size; nc.height = size;
  const nctx = nc.getContext('2d');
  const img = nctx.createImageData(size, size);
  for (let i = 0; i < img.data.length; i += 4) {
    // 顆粒偏亮一點、少一點純黑，紋理才不會讀成一片陰影斑點。
    const v = Math.random() < 0.35 ? 40 : 255;
    img.data[i] = v; img.data[i + 1] = v; img.data[i + 2] = v;
    img.data[i + 3] = Math.random() * 60;
  }
  nctx.putImageData(img, 0, 0);
  return ctx.createPattern(nc, 'repeat');
}

// 把 GeoJSON 世界地圖畫成一張等距圓柱投影(equirectangular)材質，跟 buildSphere() 的 UV 對應公式完全一致：
// canvasX = ((lng+180)/360)*width，canvasY = ((90-lat)/180)*height，貼上去才不會對不齊、跑掉。
// 風格是插畫地形圖：色塊分明但邊界平滑過渡、淡淡顆粒紋理模擬山脈，不用黑色陰影堆疊出立體感。
function buildReliefTexture(geojson, texW, texH) {
  const cv = document.createElement('canvas');
  cv.width = texW; cv.height = texH;
  const ctx = cv.getContext('2d');

  // 海洋：由極區到赤道的藍色漸層
  const ocean = ctx.createLinearGradient(0, 0, 0, texH);
  ocean.addColorStop(0, '#BFE3EE');
  ocean.addColorStop(0.5, '#2E8FC0');
  ocean.addColorStop(1, '#BFE3EE');
  ctx.fillStyle = ocean;
  ctx.fillRect(0, 0, texW, texH);

  const toXY = (lng, lat) => [((lng + 180) / 360) * texW, ((90 - lat) / 180) * texH];

  // 陸地依緯度平滑漸層：極區白霜、溫帶綠、副熱帶偏黃(乾燥)、熱帶深綠(雨林)，色帶之間平順過渡。
  const landGrad = ctx.createLinearGradient(0, 0, 0, texH);
  const bandStops = [
    [76, [236, 242, 240]], [51, [104, 168, 96]], [31, [178, 188, 78]], [15, [222, 190, 96]], [0, [86, 154, 100]],
  ];
  bandStops.forEach(([lat, c]) => landGrad.addColorStop((90 - lat) / 180, `rgb(${c[0]},${c[1]},${c[2]})`));
  for (let i = bandStops.length - 2; i >= 0; i--) {
    const [lat, c] = bandStops[i];
    landGrad.addColorStop((90 + lat) / 180, `rgb(${c[0]},${c[1]},${c[2]})`);
  }

  const noisePattern = buildNoisePattern(ctx);

  (geojson.features || []).forEach((feat) => {
    const geom = feat.geometry;
    if (!geom) return;
    const polyRingsList = geom.type === 'Polygon' ? [geom.coordinates]
      : geom.type === 'MultiPolygon' ? geom.coordinates
      : null;
    if (!polyRingsList) return;

    const name = (feat.properties && feat.properties.name) || '';

    // 跨 180 度經線的國家(如俄羅斯)先把經度攤平成連續數列，只有真的跨越時才往左右各補畫一份。
    let neededUnwrap = false;
    const ringsData = polyRingsList.map((rings) => rings.map((ring) => {
      const simp = simplifyRing(ring);
      const out = [simp[0]];
      let offset = 0;
      for (let i = 1; i < simp.length; i++) {
        let lng = simp[i][0] + offset;
        if (lng - out[i - 1][0] > 180) { offset -= 360; lng -= 360; neededUnwrap = true; }
        else if (lng - out[i - 1][0] < -180) { offset += 360; lng += 360; neededUnwrap = true; }
        out.push([lng, simp[i][1]]);
      }
      return out;
    }));

    const path = new Path2D();
    const shifts = neededUnwrap ? [0, -360, 360] : [0];
    ringsData.forEach((rings) => {
      shifts.forEach((shift) => {
        rings.forEach((ring) => {
          const proj = ring.map(([lng, lat]) => toXY(lng + shift, lat));
          proj.forEach(([x, y], i) => { if (i === 0) path.moveTo(x, y); else path.lineTo(x, y); });
          path.closePath();
        });
      });
    });

    ctx.save();
    ctx.clip(path, 'evenodd');
    ctx.fillStyle = landGrad;
    ctx.fill(path, 'evenodd');
    // 淡淡的顆粒紋理模擬山脈起伏，只用低透明度疊一層，不會變成生硬的陰影色塊。
    ctx.globalAlpha = 0.16;
    ctx.fillStyle = noisePattern;
    ctx.fillRect(0, 0, texW, texH);
    ctx.globalAlpha = 1;
    ctx.restore();
  });

  // 極淡的經緯線，呼應地圖的感覺(刻意壓得很淡，不搶陸地/海洋的層次)
  ctx.strokeStyle = 'rgba(255,255,255,.1)'; ctx.lineWidth = .6;
  for (let lng = -180; lng <= 180; lng += 20) {
    const [x] = toXY(lng, 0);
    ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, texH); ctx.stroke();
  }
  for (let lat = -80; lat <= 80; lat += 20) {
    const [, y] = toXY(0, lat);
    ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(texW, y); ctx.stroke();
  }

  // 整張材質再套一層柔化模糊，讓色塊邊界跟顆粒紋理都平順一點，是插畫感而不是向量圖死硬邊線。
  const soft = document.createElement('canvas');
  soft.width = texW; soft.height = texH;
  const softCtx = soft.getContext('2d');
  softCtx.filter = 'blur(1.6px)';
  softCtx.drawImage(cv, 0, 0);
  return soft;
}

function createTexture(gl) {
  const tex = gl.createTexture();
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([200, 220, 230, 255]));
  return tex;
}

function uploadCanvasTexture(gl, tex, canvasEl) {
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, canvasEl);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
  gl.generateMipmap(gl.TEXTURE_2D);
}

(function () {
  const canvas = document.getElementById('globe-canvas');
  const panel = document.querySelector('.globe-panel');
  const pinsContainer = document.getElementById('globe-pins');
  const tooltip = document.getElementById('globe-tooltip');
  const dragHint = document.getElementById('globe-drag-hint');
  if (!canvas || !panel || !pinsContainer || !tooltip) return;

  // 拖曳提示每次進頁面都會顯示，使用者在這次瀏覽中拖過地球後就淡出；
  // 不記住到下次造訪，這樣久久沒回來的使用者才會再看到提示。
  function dismissDragHint() {
    if (dragHint) dragHint.classList.add('is-hidden');
  }

  const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
  if (!gl) { console.warn('這個瀏覽器不支援 WebGL，互動地球停用'); return; }

  const program = gl.createProgram();
  gl.attachShader(program, compileShader(gl, gl.VERTEX_SHADER, VERTEX_SRC));
  gl.attachShader(program, compileShader(gl, gl.FRAGMENT_SHADER, FRAGMENT_SRC));
  gl.linkProgram(program);
  if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
    console.error('地球 program link 失敗', gl.getProgramInfoLog(program));
    return;
  }
  gl.useProgram(program);

  const sphere = buildSphere(48, 96);
  const posBuf = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, posBuf);
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(sphere.positions), gl.STATIC_DRAW);
  const aPosition = gl.getAttribLocation(program, 'aPosition');
  gl.enableVertexAttribArray(aPosition);
  gl.vertexAttribPointer(aPosition, 3, gl.FLOAT, false, 0, 0);

  const uvBuf = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, uvBuf);
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(sphere.uvs), gl.STATIC_DRAW);
  const aUV = gl.getAttribLocation(program, 'aUV');
  gl.enableVertexAttribArray(aUV);
  gl.vertexAttribPointer(aUV, 2, gl.FLOAT, false, 0, 0);

  const idxBuf = gl.createBuffer();
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, idxBuf);
  gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(sphere.indices), gl.STATIC_DRAW);

  const uRotY = gl.getUniformLocation(program, 'uRotY');
  const uRotX = gl.getUniformLocation(program, 'uRotX');
  const uScaleX = gl.getUniformLocation(program, 'uScaleX');
  const uScaleY = gl.getUniformLocation(program, 'uScaleY');
  const uEarthTex = gl.getUniformLocation(program, 'uEarthTex');

  const earthTex = createTexture(gl);
  fetch('/data/world.json')
    .then((res) => (res.ok ? res.json() : Promise.reject(new Error('HTTP ' + res.status))))
    .then((geojson) => {
      const reliefCanvas = buildReliefTexture(geojson, 2048, 1024);
      uploadCanvasTexture(gl, earthTex, reliefCanvas);
    })
    .catch((err) => console.error('世界地圖材質產生失敗', err));

  gl.enable(gl.DEPTH_TEST);
  gl.depthFunc(gl.LEQUAL);
  gl.clearColor(0, 0, 0, 0);

  let w = 0, h = 0, R = 0, cx = 0, cy = 0;
  let rotY = 0.5, rotX = -0.3;
  const spin = 0.0016;
  let dragging = false, lastX = 0, lastY = 0;
  let markers = [];
  let pinEls = [];
  let latestProjected = [];
  let focusRotY = null; // 不是 null 時代表正在轉向篩選條件選到的國家/區域，暫停自動慢轉
  const countrySelect = document.getElementById('filter-country');
  const regionSelect = document.getElementById('filter-region');

  // 轉到指定國家的圖釘正面朝向鏡頭：跟下面的「國家」篩選欄雙向連動。
  function focusOnCountry(country) {
    const idx = markers.findIndex((m) => m.country === country);
    if (idx < 0) return;
    const lngRad = (markers[idx].lng * Math.PI) / 180;
    focusRotY = -lngRad;
    showTooltip(idx);
    clearTimeout(focusOnCountry._hideTimer);
    focusOnCountry._hideTimer = setTimeout(hideTooltip, 2200);
  }

  // 轉到某個「區域」裡所有行程的平均方位（沒有單一國家精確，取角度平均，正確處理跨 180 度的情況）。
  function focusOnRegion(region) {
    const matched = markers.filter((m) => COUNTRY_CONTINENT[m.country] === region);
    if (!matched.length) return;
    let sinSum = 0, cosSum = 0;
    matched.forEach((m) => {
      const lngRad = (m.lng * Math.PI) / 180;
      sinSum += Math.sin(lngRad); cosSum += Math.cos(lngRad);
    });
    const avgLng = Math.atan2(sinSum, cosSum);
    focusRotY = -avgLng;
    hideTooltip();
  }

  function resize() {
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const rect = canvas.getBoundingClientRect();
    w = rect.width; h = rect.height;
    canvas.width = Math.max(1, w * dpr);
    canvas.height = Math.max(1, h * dpr);
    gl.viewport(0, 0, canvas.width, canvas.height);
    cx = w / 2; cy = h / 2;
    R = Math.min(w, h) / 2 - 14;
  }

  function project(latDeg, lngDeg) {
    const lat = (latDeg * Math.PI) / 180;
    const lng = (lngDeg * Math.PI) / 180;
    const x = Math.cos(lat) * Math.sin(lng);
    const y = Math.sin(lat);
    const z = Math.cos(lat) * Math.cos(lng);
    const cosY = Math.cos(rotY), sinY = Math.sin(rotY);
    const x1 = x * cosY + z * sinY;
    const z1 = -x * sinY + z * cosY;
    const cosX = Math.cos(rotX), sinX = Math.sin(rotX);
    const y2 = y * cosX - z1 * sinX;
    const z2 = y * sinX + z1 * cosX;
    return { sx: cx + x1 * R, sy: cy - y2 * R, depth: z2 };
  }

  function drawGL() {
    if (!w || !h) return;
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

    // 地球本體
    gl.useProgram(program);
    gl.bindBuffer(gl.ARRAY_BUFFER, posBuf);
    gl.vertexAttribPointer(aPosition, 3, gl.FLOAT, false, 0, 0);
    gl.bindBuffer(gl.ARRAY_BUFFER, uvBuf);
    gl.vertexAttribPointer(aUV, 2, gl.FLOAT, false, 0, 0);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, idxBuf);
    gl.uniform1f(uRotY, rotY);
    gl.uniform1f(uRotX, rotX);
    gl.uniform1f(uScaleX, R / (w / 2));
    gl.uniform1f(uScaleY, R / (h / 2));
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, earthTex);
    gl.uniform1i(uEarthTex, 0);
    gl.drawElements(gl.TRIANGLES, sphere.indices.length, gl.UNSIGNED_SHORT, 0);
  }

  function updatePins() {
    latestProjected = markers.map((m, i) => {
      const p = project(m.lat, m.lng);
      const el = pinEls[i];
      if (!el) return { ...m, ...p };
      if (p.depth < -0.05) {
        el.style.display = 'none';
      } else {
        el.style.display = '';
        const scale = 0.72 + p.depth * 0.5;
        el.style.transform = `translate(${p.sx}px, ${p.sy}px) translate(-50%, -100%) scale(${scale})`;
        el.style.zIndex = String(Math.round((p.depth + 1) * 100));
      }
      return { ...m, ...p };
    });
  }

  function loop() {
    if (dragging) {
      focusRotY = null;
    } else if (focusRotY !== null) {
      const diff = (((focusRotY - rotY) % (Math.PI * 2)) + Math.PI * 3) % (Math.PI * 2) - Math.PI;
      rotY += diff * 0.08;
      if (Math.abs(diff) < 0.004) focusRotY = null;
    } else {
      rotY += spin;
    }
    drawGL();
    updatePins();
    requestAnimationFrame(loop);
  }

  function hideTooltip() { tooltip.hidden = true; }
  function showTooltip(idx) {
    const p = latestProjected[idx];
    if (!p) return;
    tooltip.hidden = false;
    tooltip.textContent = p.name;
    tooltip.style.left = p.sx + 'px';
    tooltip.style.top = (p.sy - 22) + 'px';
  }

  function rebuildPins() {
    pinsContainer.innerHTML = '';
    pinEls = markers.map((m, i) => {
      const el = document.createElement('div');
      el.className = 'globe-pin';
      el.innerHTML =
        '<svg width="20" height="26" viewBox="0 0 20 26">' +
        '<path d="M10 25.5C10 25.5 2 15.2 2 9.2A8 8 0 0 1 18 9.2C18 15.2 10 25.5 10 25.5Z" fill="#F0518C" stroke="#fff" stroke-width="1.4"/>' +
        '<circle cx="10" cy="9.2" r="3.3" fill="#fff"/>' +
        '</svg>';
      el.addEventListener('mouseenter', () => showTooltip(i));
      el.addEventListener('mouseleave', hideTooltip);
      el.addEventListener('click', () => {
        if (!countrySelect) { window.location.href = '/TravelGroups/Details/' + encodeURIComponent(m.groupId); return; }
        countrySelect.value = m.country;
        countrySelect.dispatchEvent(new Event('change', { bubbles: true }));
      });
      pinsContainer.appendChild(el);
      return el;
    });
  }

  // 把「國家」篩選欄的選項換成真的有行程的國家（跟地球圖釘一致），並接上雙向連動：
  // 選單改選 -> 地球轉去面對那個國家；點地球圖釘 -> 選單同步選到那個國家。
  function syncCountrySelect() {
    if (!countrySelect) return;
    const current = countrySelect.value;
    countrySelect.innerHTML = '<option>國家</option>';
    markers.forEach((m) => {
      const opt = document.createElement('option');
      opt.value = m.country; opt.textContent = m.country;
      countrySelect.appendChild(opt);
    });
    if (markers.some((m) => m.country === current)) countrySelect.value = current;
  }

  // 「區域」篩選欄的選項也只列出真的有行程的洲別（例如目前資料只有亞洲/歐洲/大洋洲，不會出現用不到的美洲/非洲）。
  function syncRegionSelect() {
    if (!regionSelect) return;
    const current = regionSelect.value;
    const continents = [...new Set(markers.map((m) => COUNTRY_CONTINENT[m.country]).filter(Boolean))];
    regionSelect.innerHTML = '<option>區域</option>';
    continents.forEach((c) => {
      const opt = document.createElement('option');
      opt.value = c; opt.textContent = c;
      regionSelect.appendChild(opt);
    });
    if (continents.includes(current)) regionSelect.value = current;
  }

  panel.addEventListener('mousedown', (e) => {
    dragging = true; lastX = e.clientX; lastY = e.clientY;
    canvas.classList.add('is-dragging');
  });
  window.addEventListener('mousemove', (e) => {
    if (!dragging) return;
    dismissDragHint();
    const dx = e.clientX - lastX, dy = e.clientY - lastY;
    lastX = e.clientX; lastY = e.clientY;
    rotY += dx * 0.006;
    rotX = Math.max(-1.2, Math.min(1.2, rotX - dy * 0.006));
  });
  window.addEventListener('mouseup', () => {
    dragging = false; canvas.classList.remove('is-dragging');
  });

  if (countrySelect) {
    countrySelect.addEventListener('change', () => {
      const country = countrySelect.value;
      if (!country || country === '國家') return;
      focusOnCountry(country);
      // 選了具體國家，「區域」欄跟著補上對應洲別，畫面上兩個篩選欄才不會互相矛盾。
      const continent = COUNTRY_CONTINENT[country];
      if (regionSelect && continent && regionSelect.value !== continent) {
        regionSelect.value = continent;
        regionSelect.dispatchEvent(new Event('change', { bubbles: true }));
      }
    });
  }
  if (regionSelect) {
    regionSelect.addEventListener('change', () => {
      const region = regionSelect.value;
      if (!region || region === '區域') return;
      // 換了區域，如果原本選的國家不屬於這個區域，先清掉，不然「國家：日本 + 區域：歐洲」會篩出矛盾的空結果。
      if (countrySelect && countrySelect.value && COUNTRY_CONTINENT[countrySelect.value] !== region) {
        countrySelect.value = '國家';
        countrySelect.dispatchEvent(new Event('change', { bubbles: true }));
      }
      focusOnRegion(region);
    });
  }

  window.addEventListener('resize', resize);
  resize();
  loop();

  fetch('/Home/Data')
    .then((res) => (res.ok ? res.json() : Promise.reject(new Error('HTTP ' + res.status))))
    .then((vm) => {
      markers = (vm.popularTrips ?? [])
        .map((trip) => {
          const coord = COUNTRY_COORDS[trip.country];
          if (!coord) return null;
          return { groupId: trip.groupId, name: trip.groupTitle, lat: coord[0], lng: coord[1], country: trip.country };
        })
        .filter(Boolean);
      rebuildPins();
      syncCountrySelect();
      syncRegionSelect();
    })
    .catch((err) => console.error('地球行程資料載入失敗', err));
})();
