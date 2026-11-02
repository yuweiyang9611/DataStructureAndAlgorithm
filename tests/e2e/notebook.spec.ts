import { test, expect, type Page } from '@playwright/test'

// The lab-notebook theme: the opening screen reads the units record, day and night follow the
// family's `site-theme` key, and the kit's touching-marks pass never has to touch Vue's text.

async function watchRuntime(page: Page) {
  const messages: string[] = []
  page.on('pageerror', (error) => messages.push(String(error)))
  page.on('console', (message) => { if (['error', 'warning'].includes(message.type())) messages.push(message.text()) })
  await page.addInitScript(() => {
    // kit/v1/theme.js squeezes marks by replacing text nodes; Vue never replaces a text node this way.
    const counter = window as unknown as { __replaced: number }
    counter.__replaced = 0
    const replaceWith = Text.prototype.replaceWith
    Text.prototype.replaceWith = function (...nodes) { counter.__replaced += 1; return replaceWith.apply(this, nodes) }
  })
  return messages
}

const replaced = (page: Page) => page.evaluate(() => (window as unknown as { __replaced: number }).__replaced)
const looseRuns = (page: Page) => page.evaluate(() => {
  const loose = /[”’][，。、；：？！]|[，。、；：？！][“‘]/
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT)
  let count = 0
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    if (loose.test((node as Text).data) && !node.parentElement?.closest('code, pre, kbd, samp, textarea, script, style')) count++
  }
  return count
})

test('the opening screen continues from the record and shows no mark without one', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('./')
  const resume = page.locator('.continue-learning')
  await expect(resume).toHaveText('从第一个单元开始：链表、栈、队列与双端队列')
  await expect(resume).toHaveAttribute('href', /#unit-core-linear-structures$/)
  await expect(page.locator('.unit-register > [aria-current]')).toHaveCount(0)
  await expect(page.locator('.unit-register > [data-record="done"]')).toHaveCount(0)
  await expect(page.getByText('停在这里')).toHaveCount(0)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  // Reading the record must never create the units page's database.
  if (await page.evaluate(() => typeof indexedDB.databases === 'function')) {
    expect(await page.evaluate(async () => (await indexedDB.databases()).map((database) => database.name))).not.toContain('dsa-learning-progress')
  }

  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  await page.locator('#unit-core-sorting').getByRole('button', { name: '开始学习', exact: true }).click()
  await expect(page.locator('#unit-core-sorting .unit-status')).toHaveText('学习中')
  await expect(page.locator('#unit-core-sorting')).toHaveAttribute('aria-current', 'step')

  await page.goto('./')
  await expect(resume).toHaveText('接着学：比较排序与非比较排序')
  const stopped = page.locator('.unit-register > [aria-current]')
  await expect(stopped).toHaveCount(1)
  await expect(stopped).toHaveAttribute('data-unit', 'core-sorting')
  await expect(stopped.getByText('停在这里')).toBeVisible()
  await expect(page.locator('.record-track')).toHaveAttribute('aria-valuenow', '0')
  await expect(page.locator('.record-copy')).toContainText('0 / 22 已验证')
})

test('the command to run shows whole on a desktop and the copy action stays clear of it', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('./')
  const command = page.locator('.desk-run .nb-command')
  await expect(command).toContainText('--filter')
  expect(await command.evaluate((pre) => pre.scrollWidth <= pre.clientWidth)).toBe(true)
  const copy = page.locator('.desk-run').getByRole('button', { name: '复制命令' })
  const [box, action] = [await command.boundingBox(), await copy.boundingBox()]
  expect(action!.y).toBeGreaterThanOrEqual(box!.y + box!.height)
})

test('numbered contents keep the number with its title and never cut a phrase', async ({ page }) => {
  for (const width of [1440, 1280]) {
    await page.setViewportSize({ width, height: 900 })
    await page.goto('综合项目实战学习指导.html')
    const entry = page.locator('.VPDocAsideOutline .outline-link', { hasText: '从最大匹配升级到最小费用最大流' })
    await expect(entry.locator('.outline-number')).toHaveText('3.4')
    const layout = await page.locator('.VPDocAsideOutline .outline-link').evaluateAll((links) => links.map((link) => {
      const text = link.querySelector<HTMLElement>('.outline-text')!
      text.style.overflowWrap = 'normal'
      const fits = text.scrollWidth <= text.clientWidth + 0.5
      text.style.overflowWrap = ''
      const number = link.querySelector('.outline-number')
      const together = !number || Math.abs(number.getBoundingClientRect().top - text.getClientRects()[0].top) < 4
      return fits && together
    }))
    expect(layout.length).toBeGreaterThan(20)
    expect(layout.every(Boolean)).toBe(true)
  }
})

test('day and night follow site-theme and keep VitePress dark state in step', async ({ page }) => {
  await page.addInitScript(() => { if (!sessionStorage.getItem('seeded')) { localStorage.setItem('site-theme', 'dark'); sessionStorage.setItem('seeded', '1') } })
  await page.goto('开始学习.html')
  const root = page.locator('html')
  await expect(root).toHaveAttribute('data-theme', 'dark')
  await expect(root).toHaveClass(/\bdark\b/)
  await expect(page.getByRole('button', { name: '晚上' })).toHaveAttribute('aria-pressed', 'true')

  await page.getByRole('button', { name: '白天' }).click()
  await expect(root).toHaveAttribute('data-theme', 'light')
  await expect(root).not.toHaveClass(/\bdark\b/)
  expect(await page.evaluate(() => localStorage.getItem('site-theme'))).toBe('light')
  await page.reload()
  await expect(root).toHaveAttribute('data-theme', 'light')
  await expect(root).not.toHaveClass(/\bdark\b/)
  await expect(page.locator('meta[name="theme-color"]')).toHaveAttribute('content', '#f4efe4')

  // The other direction: VitePress's own dark class drives data-theme and the pressed word.
  await page.evaluate(() => document.documentElement.classList.add('dark'))
  await expect(root).toHaveAttribute('data-theme', 'dark')
  await expect(page.getByRole('button', { name: '晚上' })).toHaveAttribute('aria-pressed', 'true')
  await expect(page.locator('meta[name="theme-color"]')).toHaveAttribute('content', '#1c1a17')
  const keys = await page.evaluate(() => Object.keys(localStorage))
  expect(keys).not.toContain('vitepress-theme-appearance')
  expect(['light', 'dark']).toContain(await page.evaluate(() => localStorage.getItem('site-theme')))
})

test('hydration is clean and the touching-marks pass changes nothing, across client navigation', async ({ page }) => {
  const messages = await watchRuntime(page)
  await page.goto('./')
  await expect(page.locator('.course-home h1')).toHaveText('算法研习室')
  await page.locator('.VPNavBar').getByRole('link', { name: '学习单元', exact: true }).click()
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  await page.locator('.VPNavBar').getByRole('link', { name: 'Trace 实验室', exact: true }).click()
  await expect(page.locator('.trace-event')).toBeVisible()
  await page.locator('.VPNavBar').getByRole('link', { name: '源码导航', exact: true }).click()
  await expect(page.locator('.vp-doc h1')).toHaveText('文档与源码导航')
  await expect(page.locator('.vp-doc')).toHaveCount(1)
  await page.goBack()
  await expect(page.locator('.trace-event')).toBeVisible()
  await page.goBack()
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  await page.goBack()
  await expect(page.locator('.course-home')).toHaveCount(1)
  await expect(page.locator('h1')).toHaveCount(1)
  await expect(page.locator('.vp-doc, .unit-explorer, .trace-lab')).toHaveCount(0)

  await page.goto('综合项目实战学习指导.html')
  await expect(page.locator('.vp-doc .nb-halt').first()).toBeAttached()
  expect(await looseRuns(page)).toBe(0)
  expect(await replaced(page)).toBe(0)
  expect(messages.filter((message) => /hydration|mismatch/i.test(message))).toEqual([])
  expect(messages).toEqual([])
})
