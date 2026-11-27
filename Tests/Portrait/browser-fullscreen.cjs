const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }
const root = 'Assets/WebGLTemplates/HealthCloud';
const fixture = fs.readFileSync(path.join(root, 'index.html'), 'utf8')
  .replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '')
  .replace('</body>', '<script type="module" src="portrait-viewport.mjs"></script><script type="module" src="fullscreen.mjs"></script></body>');
const server = http.createServer((request, response) => {
  if (request.url === '/') { response.setHeader('Content-Type', 'text/html'); response.end(fixture); }
  else if (/^\/(portrait-viewport\.mjs|fullscreen\.mjs|theme[\w.-]+)$/.test(request.url) && fs.existsSync(path.join(root, request.url.slice(1)))) {
    response.setHeader('Content-Type', ({ '.mjs': 'application/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.png': 'image/png' })[path.extname(request.url)]);
    response.end(fs.readFileSync(path.join(root, request.url.slice(1))));
  } else { response.statusCode = 404; response.end(); }
});
(async () => {
  let browser;
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForFunction(() => window.healthFullscreen);
    assert.equal(await page.evaluate(() => Boolean(document.fullscreenElement)), false);
    assert.equal(await page.locator('#fullscreen-toggle').isVisible(), false);
    await page.evaluate(() => { document.querySelector('#account-gate').hidden = true; window.healthFullscreen.arm(); });
    await page.locator('#unity-canvas').click();
    await page.waitForFunction(() => document.fullscreenElement === document.documentElement);
    assert.equal(await page.locator('#fullscreen-toggle').textContent(), 'Exit full screen');
    await page.waitForTimeout(100);
    const fit = await page.evaluate(() => {
      const rect = document.querySelector('#unity-canvas').getBoundingClientRect();
      const viewport = document.querySelector('#game-viewport').getBoundingClientRect();
      return rect.right <= viewport.right + 1 && rect.bottom <= viewport.bottom + 1 &&
        document.documentElement.scrollWidth <= innerWidth;
    });
    assert.equal(fit, true, 'Portrait canvas must still fit in fullscreen');
    await page.locator('#fullscreen-toggle').click();
    await page.waitForFunction(() => !document.fullscreenElement);
    await page.locator('#unity-canvas').click();
    assert.equal(await page.evaluate(() => Boolean(document.fullscreenElement)), false, 'No forced re-entry after exit');
    await page.locator('#fullscreen-toggle').click();
    await page.waitForFunction(() => Boolean(document.fullscreenElement));
    await page.evaluate(() => document.exitFullscreen());
    await page.waitForFunction(() => !document.fullscreenElement);
    await page.waitForFunction(() => document.querySelector('#fullscreen-toggle').getAttribute('aria-pressed') === 'false');
    assert.equal(await page.locator('#fullscreen-toggle').getAttribute('aria-pressed'), 'false');
    console.log('PASS: ready-only first-tap fullscreen, whole-page target, portrait fit, exit and explicit re-entry in Chromium. No Unity build or Firebase requests.');
  } finally { if (browser) await browser.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
