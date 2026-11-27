const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }
const root = 'Assets/WebGLTemplates/HealthCloud';
const template = fs.readFileSync(path.join(root, 'index.html'), 'utf8')
  .replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '')
  .replace('</body>', `<script>window.audioCalls=[];window.healthUnity={SendMessage(...args){window.audioCalls.push(args)}};window.healthOpenAccount=()=>window.accountOpened=true;</script><script type="module" src="theme.mjs"></script></body>`);
const server = http.createServer((request, response) => {
  if (request.url === '/') { response.setHeader('Content-Type', 'text/html'); response.end(template); return; }
  const file = path.join(root, path.basename(request.url));
  if (!fs.existsSync(file)) { response.writeHead(404).end(); return; }
  response.setHeader('Content-Type', ({ '.mjs': 'application/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.png': 'image/png' })[path.extname(file)] || 'text/plain');
  response.end(fs.readFileSync(file));
});
(async () => {
  let browser;
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForFunction(() => window.healthAudio);
    await page.locator('#sound-toggle').click();
    assert.equal(await page.locator('#sound-toggle').getAttribute('aria-pressed'), 'true');
    assert.deepEqual(await page.evaluate(() => audioCalls.at(-1)), ['HealthAudio', 'SetMuted', '1']);
    await page.reload();
    await page.waitForFunction(() => window.healthAudio);
    assert.equal(await page.locator('#sound-toggle').getAttribute('aria-pressed'), 'true', 'Mute preference must survive reload');
    await page.locator('#sound-toggle').click();
    assert.deepEqual(await page.evaluate(() => audioCalls.at(-1)), ['HealthAudio', 'SetMuted', '0']);
    await page.locator('#account-toggle').click();
    assert.equal(await page.locator('#account-sign-in').isVisible(), true);
    await page.locator('#account-sign-in').click();
    assert.equal(await page.evaluate(() => window.accountOpened), true);
    await page.evaluate(() => { document.querySelector('#sign-out').hidden = false; });
    await page.locator('#account-toggle').click();
    assert.equal(await page.locator('#account-sign-in').isVisible(), false);
    assert.equal(await page.locator('#sign-out').isVisible(), true);
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('#account-menu').isVisible(), false);
    console.log('PASS: sound toggle sends Unity messages, persists across reload, and account menu exposes the correct action and closes with Escape.');
  } finally { if (browser) await browser.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
