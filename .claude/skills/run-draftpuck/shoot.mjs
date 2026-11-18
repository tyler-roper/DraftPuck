// Screenshots pages of the running copy (run.sh up) in headless Chrome, signed in as a seeded player.
//
//   node shoot.mjs <email> <out-dir> <step> [<step> ...]
//
// Each step is a path to open, optionally followed by "|" and a Playwright selector to click before the screenshot
// (e.g. "/lobby/ABCD|text=ABCD" opens the lobby and clicks its code). Screenshots are saved as 01.png, 02.png, ...
// in <out-dir>, at a phone-sized viewport. Console errors and failed API calls are printed, so a page that renders
// its shell while its data fails is noticed.
import { chromium } from 'playwright-core'

const [email, outDir, ...steps] = process.argv.slice(2)
if (!email || !outDir || steps.length === 0) {
  console.error('Usage: node shoot.mjs <email> <out-dir> <path[|selector-to-click]> ...')
  process.exit(1)
}

const base = process.env.DRAFTPUCK_URL ?? 'http://localhost:17100'
const password = process.env.DRAFTPUCK_PASSWORD ?? 'draftpuck-run-1'

// channel "chrome": the Chrome already installed on the machine, so Playwright downloads no browser.
const browser = await chromium.launch({ channel: 'chrome' })
const page = await (await browser.newContext({ viewport: { width: 400, height: 860 }, deviceScaleFactor: 2 })).newPage()
page.on('console', (m) => m.type() === 'error' && console.log(`console error: ${m.text()}`))
page.on('response', (r) => r.url().includes('/api/') && r.status() >= 400 && console.log(`${r.status()} ${r.request().method()} ${r.url()}`))

await page.goto(`${base}/login`)
await page.getByPlaceholder('Email').fill(email)
await page.getByPlaceholder('Password').fill(password)
await page.getByRole('button', { name: 'Log In' }).click()
await page.waitForURL((url) => !url.pathname.startsWith('/login'))

let n = 0
for (const step of steps) {
  const [path, click] = step.split('|')
  await page.goto(`${base}${path}`)
  await page.waitForLoadState('networkidle').catch(() => {})
  if (click) await page.locator(click).first().click()
  // Let transitions and lazy data settle.
  await page.waitForTimeout(1200)
  const file = `${outDir}/${String(++n).padStart(2, '0')}.png`
  await page.screenshot({ path: file })
  console.log(`${file}  ${step}`)
}

await browser.close()
