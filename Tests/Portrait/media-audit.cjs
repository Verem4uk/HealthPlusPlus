// Reads dimensions only; does not convert or change any original media.
const fs = require('node:fs');
const path = require('node:path');
let chromium;
try { ({ chromium } = require('playwright')); }
catch { ({ chromium } = require(path.join(path.dirname(process.execPath), '../node_modules/playwright'))); }
function walk(root) { return fs.readdirSync(root, { withFileTypes: true }).flatMap(item => item.isDirectory() ? walk(path.join(root, item.name)) : [path.join(root, item.name)]); }
function mp4Size(buffer) {
  for (let at = buffer.indexOf('tkhd'); at >= 4; at = buffer.indexOf('tkhd', at + 4)) {
    const length = buffer.readUInt32BE(at - 4);
    const end = at - 4 + length;
    if (length < 16 || end > buffer.length) continue;
    const width = buffer.readUInt32BE(end - 8) / 65536;
    const height = buffer.readUInt32BE(end - 4) / 65536;
    if (width > 0 && height > 0) return { width, height };
  }
  throw new Error('No complete video track header in this byte range');
}
(async () => {
  const local = walk('Assets/Images').filter(file => file.endsWith('.mp4')).map(file => ({ file, ...mp4Size(fs.readFileSync(file)) }));
  const urls = [...new Set(walk('Assets/BD').filter(file => file.endsWith('.asset')).flatMap(file => [...fs.readFileSync(file, 'utf8').matchAll(/videoUrl: (https:\/\/\S+)/g)].map(match => match[1])))];
  const browser = await chromium.launch({ headless: true });
  const remote = [];
  try {
    for (const url of urls) {
      const page = await browser.newPage();
      const result = await page.evaluate(url => new Promise(resolve => {
        const video = document.createElement('video');
        video.preload = 'metadata';
        const timer = setTimeout(() => finish({ error: 'Metadata timeout' }), 20000);
        function finish(value) { clearTimeout(timer); video.removeAttribute('src'); video.load(); resolve(value); }
        video.onloadedmetadata = () => finish({ width: video.videoWidth, height: video.videoHeight });
        video.onerror = () => finish({ error: `Media error ${video.error?.code}` });
        video.src = url;
      }), url);
      let header;
      try {
        for (const range of ['bytes=0-1048575', 'bytes=-1048576']) {
          const response = await fetch(url, { headers: { Range: range }, signal: AbortSignal.timeout(20000) });
          if (!response.ok) throw new Error(`HTTP ${response.status}`);
          try { header = mp4Size(Buffer.from(await response.arrayBuffer())); break; } catch { /* Try the tail for files without fast-start metadata. */ }
        }
      } catch (error) { header = { error: error.message }; }
      remote.push({ url, ...(header || { error: 'No readable track dimensions' }), chromiumMetadata: result });
      await page.close();
    }
  } finally { await browser.close(); }
  fs.mkdirSync('Logs/Graphics', { recursive: true });
  fs.writeFileSync('Logs/Graphics/media-audit.json', JSON.stringify({ local, remote }, null, 2));
  console.log(JSON.stringify({ local, remote }, null, 2));
})().catch(error => { console.error(error); process.exitCode = 1; });
