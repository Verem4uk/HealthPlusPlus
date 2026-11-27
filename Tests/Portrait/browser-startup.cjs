const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }
const root = path.resolve('Build/GraphicsUpdate');
const server = http.createServer((request, response) => {
  const name = new URL(request.url, 'http://localhost').pathname;
  const file = path.resolve(root, '.' + (name === '/' ? '/index.html' : name));
  if (!file.startsWith(root + path.sep) || !fs.existsSync(file) || !fs.statSync(file).isFile()) { response.writeHead(404).end(); return; }
  const type = file.includes('.wasm') ? 'application/wasm' : file.includes('.js.') ? 'application/javascript'
    : ({ '.html': 'text/html', '.mjs': 'application/javascript', '.js': 'application/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.png': 'image/png', '.mp4': 'video/mp4' })[path.extname(file)] || 'application/octet-stream';
  response.setHeader('Content-Type', type);
  if (file.endsWith('.unityweb')) response.setHeader('Content-Encoding', 'gzip');
  response.setHeader('Cache-Control', 'no-store');
  fs.createReadStream(file).pipe(response);
});
(async () => {
  let browser;
  const results = [];
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    // Real autoplay policy: no bypass flag, fresh context, no clicks before playback.
    browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const page = await context.newPage();
    await page.addInitScript(() => {
      window.startupVideos = [];
      window.startupLog = [];
      const create = document.createElement.bind(document);
      document.createElement = (name, ...args) => {
        const element = create(name, ...args);
        if (String(name).toLowerCase() === 'video') {
          startupVideos.push(element);
          for (const type of ['playing', 'pause', 'ended', 'error']) element.addEventListener(type, () => startupLog.push({ type, time: element.currentTime, src: element.currentSrc, gate: !document.querySelector('#account-gate')?.hidden }));
        }
        return element;
      };
    });
    await page.route('https://www.gstatic.com/firebasejs/**', route => route.abort());
    await page.route('https://verlema.life/**', route => route.abort());
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForFunction(() => window.healthCloud && window.startHealthUnity);
    await page.evaluate(async () => {
      window.healthFullscreen = { arm() {} };
      window.startupInstance = await window.startHealthUnity();
    });
    // Simulate delayed account restore, which must not consume the menu intro behind the gate.
    await page.waitForTimeout(2500);
    const premature = await page.evaluate(() => startupLog.filter(event => event.type === 'playing' && event.gate));
    results.push({ premature });
    await page.evaluate(() => {
      startupInstance.SendMessage('HealthCloudProgress', 'Restore', JSON.stringify({ level: 0, baseNumber: 0, completedTrainingDay: '' }));
    });
    await page.waitForFunction(() => document.querySelector('#account-gate').hidden);
    await page.waitForFunction(() => startupVideos.some(video => video.currentTime > .2 && !video.paused), null, { timeout: 15000 });
    results.push({ playing: await page.evaluate(() => startupVideos.map(video => ({ src: video.currentSrc, time: video.currentTime, paused: video.paused, muted: video.muted }))) });
    results.push({ log: await page.evaluate(() => startupLog) });
    fs.mkdirSync('Logs/Startup', { recursive: true });
    fs.writeFileSync('Logs/Startup/browser.json', JSON.stringify(results, null, 2));
    assert.equal(premature.length, 0, 'Intro must wait for progress restore and the account gate');
    await context.close();
    console.log('PASS: startup video waits for progress restore and plays without a click under the normal browser autoplay policy.');
  } finally { if (browser) await browser.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
