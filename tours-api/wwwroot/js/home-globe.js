// Orthographic sphere projection; local texture sourced from three.js r160 examples.
(() => {
  const canvas = document.getElementById('home-globe');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const sphere = document.createElement('canvas');
  sphere.width = sphere.height = 256;
  const paint = sphere.getContext('2d');
  const frame = paint.createImageData(256, 256);
  const texture = new Image();
  let pixels, tw, th, longitude = -2.1, tilt = .25, dragging = false, lastX = 0, lastY = 0, lastFrame = 0;
  const reduceMotion = matchMedia('(prefers-reduced-motion: reduce)');
  function draw() {
    if (!pixels) return;
    const sin = Math.sin(tilt), cos = Math.cos(tilt);
    for (let y = 0; y < 256; y++) for (let x = 0; x < 256; x++) {
      const nx = (x - 127.5) / 125, ny = (127.5 - y) / 125;
      const r2 = nx * nx + ny * ny, i = (y * 256 + x) * 4;
      if (r2 > 1) { frame.data[i + 3] = 0; continue; }
      const nz = Math.sqrt(1 - r2), ry = ny * cos + nz * sin, rz = nz * cos - ny * sin;
      const u = ((Math.atan2(nx, rz) + longitude) / (2 * Math.PI) + .5 + 2) % 1;
      const v = .5 - Math.asin(Math.max(-1, Math.min(1, ry))) / Math.PI;
      const source = (Math.min(th - 1, Math.floor(v * th)) * tw + Math.floor(u * tw)) * 4;
      const light = .52 + .48 * Math.max(0, -.35 * nx + .25 * ny + .9 * nz);
      for (let c = 0; c < 3; c++) frame.data[i + c] = pixels[source + c] * light;
      frame.data[i + 3] = Math.min(255, (1 - r2) * 6000);
    }
    paint.putImageData(frame, 0, 0);
    const w = canvas.clientWidth, h = canvas.clientHeight, dpr = Math.min(devicePixelRatio || 1, 2);
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) { canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr); }
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);
    const size = Math.max(1, Math.min(w - 32, h - 86));
    ctx.save(); ctx.shadowColor = 'rgba(66,156,193,.45)'; ctx.shadowBlur = 20;
    ctx.drawImage(sphere, (w - size) / 2, (h - size) / 2, size, size); ctx.restore();
  }
  function animate(time) {
    if (time - lastFrame > 50 && !document.hidden) {
      if (!dragging && !reduceMotion.matches) longitude += .003;
      draw(); lastFrame = time;
    }
    requestAnimationFrame(animate);
  }
  texture.onload = () => {
    const source = document.createElement('canvas'); source.width = tw = texture.width; source.height = th = texture.height;
    const sc = source.getContext('2d'); sc.drawImage(texture, 0, 0); pixels = sc.getImageData(0, 0, tw, th).data;
    requestAnimationFrame(animate);
  };
  texture.onerror = () => { document.getElementById('globe-hint').textContent = '地球暫時無法載入'; };
  texture.src = '/img/earth-daymap.jpg';
  canvas.addEventListener('pointerdown', e => { dragging = true; lastX = e.clientX; lastY = e.clientY; canvas.setPointerCapture(e.pointerId); canvas.classList.add('is-dragging'); document.getElementById('globe-hint').classList.add('is-hidden'); });
  canvas.addEventListener('pointermove', e => { if (!dragging) return; longitude -= (e.clientX - lastX) * .008; tilt = Math.max(-1.2, Math.min(1.2, tilt + (e.clientY - lastY) * .008)); lastX = e.clientX; lastY = e.clientY; });
  const stop = () => { dragging = false; canvas.classList.remove('is-dragging'); };
  canvas.addEventListener('pointerup', stop); canvas.addEventListener('pointercancel', stop); canvas.addEventListener('lostpointercapture', stop);
})();
