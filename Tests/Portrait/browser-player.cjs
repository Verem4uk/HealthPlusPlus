// Smoke test of the actual compiled Unity player; account SDK is deliberately offline.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }
const root = path.resolve('Build/GraphicsUpdate');
const output = path.resolve('Logs/Graphics');
fs.mkdirSync(output, { recursive: true });
const server = http.createServer((request, response) => {
  const name = new URL(request.url, 'http://localhost').pathname;
  const file = path.resolve(root, '.' + (name === '/' ? '/index.html' : decodeURIComponent(name)));
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
  const errors = [];
  const playback = [];
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    browser = await chromium.launch({ headless: true });
    for (const [width, height] of [[1920, 1080], [390, 844]]) {
      const context = await browser.newContext({ viewport: { width, height } });
      const page = await context.newPage();
      await page.addInitScript(() => {
        window.smokeVideoFrames = [];
        const create = document.createElement.bind(document);
        document.createElement = (name, ...options) => {
          const element = create(name, ...options);
          if (String(name).toLowerCase() === 'video') {
            let recorded = false;
            element.addEventListener('timeupdate', () => {
              if (!recorded && element.currentTime > .1 && element.videoWidth > 0) {
                recorded = true;
                smokeVideoFrames.push({ source: element.currentSrc, width: element.videoWidth, height: element.videoHeight, time: element.currentTime });
              }
            });
          }
          return element;
        };
      });
      page.on('pageerror', error => errors.push(error.message));
      page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
      await page.route('https://www.gstatic.com/firebasejs/**', route => route.abort());
      // Production serves these videos on the app's own origin. For the localhost
      // fixture only, permit reads of those same original bytes across origins.
      await page.route('https://verlema.life/HealthPlusPlus/*.mp4', async route => {
        const response = await route.fetch();
        await route.fulfill({ response, headers: { ...response.headers(), 'access-control-allow-origin': '*' } });
      });
      await page.goto(`http://127.0.0.1:${server.address().port}/`);
      await page.locator('#continue-guest').waitFor({ state: 'visible', timeout: 30000 });
      await page.evaluate(() => {
        // Fullscreen itself is covered separately; hold viewport dimensions stable for screenshots.
        window.healthFullscreen = { arm() {} };
        window.smokeEvents = [];
        const track = window.healthAnalytics.track;
        window.healthAnalytics.track = (name, params) => { smokeEvents.push({ name, params }); return track(name, params); };
      });
      await page.locator('#continue-guest').click();
      await page.waitForFunction(() => window.healthUnity && document.querySelector('#account-gate').hidden, null, { timeout: 180000 });
      await page.waitForTimeout(11000);
      await page.screenshot({ path: path.join(output, `${width}x${height}_WebGL_Menu.png`) });
      await page.locator('#sound-toggle').click();
      assert.equal(await page.locator('#sound-toggle').getAttribute('aria-pressed'), 'true');
      await page.locator('#sound-toggle').click();
      const viewport = await page.locator('#game-viewport').boundingBox();
      await page.mouse.click(width / 2, viewport.y + viewport.height * .87);
      await page.waitForFunction(() => smokeEvents.some(event => event.name === 'training_start'), null, { timeout: 15000 });
      await page.waitForTimeout(1500);
      await page.screenshot({ path: path.join(output, `${width}x${height}_WebGL_Training.png`) });
      const columnWidth = Math.min(width, viewport.height * .5625);
      await page.mouse.click(width / 2 - columnWidth / 4, viewport.y + viewport.height * .93);
      await page.waitForTimeout(300);
      await page.screenshot({ path: path.join(output, `${width}x${height}_WebGL_Regression.png`) });
      playback.push({ width, height, frames: await page.evaluate(() => smokeVideoFrames) });
      // Cache prefetches can still be in flight when the verified page is closed.
      await page.unrouteAll({ behavior: 'ignoreErrors' });
      await context.close();
    }
    const unexpected = errors.filter(error => !/gstatic|ERR_FAILED|ERR_ABORTED|Failed to fetch dynamically imported module|Failed to load resource|VIDEOJS|video.*(format|decode|error)/i.test(error));
    fs.writeFileSync(path.join(output, 'player-console.json'), JSON.stringify({ errors, unexpected, playback }, null, 2));
    assert.deepEqual(unexpected, [], 'Unexpected errors from the actual WebGL player');
    await browser.close();
    browser = null;
    console.log('PASS: compiled WebGL player loads as guest, starts training, opens result entry, and toggles sound on desktop and phone. Screenshots in Logs/Graphics.');
  } finally { if (browser) await browser.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
