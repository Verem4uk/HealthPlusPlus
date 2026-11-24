const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }

const templateRoot = 'Assets/WebGLTemplates/HealthCloud';
const template = fs.readFileSync(path.join(templateRoot, 'index.html'), 'utf8')
  .replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '')
  .replace('</body>', '<script type="module" src="portrait-viewport.mjs"></script></body>');
const profiles = [[320,568], [360,780], [390,844], [430,932], [432,960], [768,1024], [820,1180], [1024,1366]];
const output = path.resolve('Logs/PortraitLayout/Web');
fs.mkdirSync(output, { recursive: true });
const server = http.createServer((request, response) => {
  if (request.url === '/') { response.setHeader('Content-Type', 'text/html'); response.end(template); }
  else if (request.url === '/portrait-viewport.mjs') {
    response.setHeader('Content-Type', 'application/javascript');
    response.end(fs.readFileSync(path.join(templateRoot, 'portrait-viewport.mjs')));
  } else { response.statusCode = 404; response.end(); }
});

(async () => {
  let browser, checks = 0;
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    for (const [width,height] of profiles) {
      await page.setViewportSize({ width,height });
      for (const inset of [false,true]) for (const mode of ['welcome','required','conflict']) {
        await page.evaluate(({ inset,mode }) => {
          document.body.style.padding = inset ? '44px 12px 34px' : '0px';
          document.querySelector('#sign-in').hidden = mode === 'conflict';
          document.querySelector('#continue-guest').hidden = mode !== 'welcome';
          document.querySelector('#conflict').hidden = mode !== 'conflict';
          document.querySelector('#sign-out').hidden = mode !== 'conflict';
          document.querySelector('#retry').hidden = mode !== 'conflict';
          document.querySelector('#save-status').textContent = mode === 'conflict'
            ? 'Saved on this device; waiting to sync your progress' : 'Health++';
          document.querySelector('#account-message').textContent = mode === 'welcome'
            ? 'Sign in to restore your progress, or try your first workout as a guest.'
            : mode === 'required'
              ? 'Great job completing your first workout! To save your progress and continue training, sign in with Google.'
              : 'This account already has progress. Choose which progress you want to keep.';
          document.querySelector('#use-local').textContent = 'This device: level 10000, workload 1000000, last workout: 2026-11-24';
          document.querySelector('#use-cloud').textContent = 'Account: level 10000, workload 1000000, last workout: 2026-11-24';
        }, { inset,mode });
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
        const result = await page.evaluate(() => {
          const viewport = document.querySelector('#game-viewport').getBoundingClientRect();
          const canvas = document.querySelector('#unity-canvas').getBoundingClientRect();
          const gate = document.querySelector('#account-gate');
          return {
            horizontal: document.documentElement.scrollWidth > innerWidth,
            bottom: viewport.bottom > innerHeight + 1,
            canvasClipped: canvas.width > viewport.width + 1 || canvas.height > viewport.height + 1,
            landscapeCanvas: canvas.width > canvas.height * .751,
            overflow: gate.scrollHeight > gate.clientHeight + 1,
            smallButtons: [...document.querySelectorAll('button')].filter(button => button.checkVisibility()).some(button => button.getBoundingClientRect().height < 44),
            buttonsClipped: [...gate.querySelectorAll('button')].filter(button => button.checkVisibility()).some(button => {
              const rect = button.getBoundingClientRect();
              return rect.left < viewport.left - 1 || rect.right > viewport.right + 1;
            })
          };
        });
        for (const [problem, present] of Object.entries(result)) {
          assert.equal(present, false, `${width}x${height}, insets=${inset}, ${mode}: ${problem}`);
          checks++;
        }
        if (!inset && (width === 390 || width === 768))
          await page.screenshot({ path: path.join(output, `${width}x${height}_${mode}.png`) });
      }
    }
    const result = `PASS: ${checks} browser layout checks, ${profiles.length} portrait viewports, three account panels, with/without simulated safe-area padding. Uses the real HTML/CSS and viewport module; no Unity player or Firebase requests.`;
    fs.writeFileSync(path.join(output, 'result.txt'), result);
    console.log(result);
  } finally { if (browser) await browser.close(); server.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
