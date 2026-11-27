const { spawnSync } = require('node:child_process');
const path = require('node:path');
for (const name of ['browser-layout.cjs', 'browser-fullscreen.cjs', 'browser-theme.cjs', 'media-audit.cjs']) {
  const result = spawnSync(process.execPath, [path.join(__dirname, name)], { stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status || 1);
}
