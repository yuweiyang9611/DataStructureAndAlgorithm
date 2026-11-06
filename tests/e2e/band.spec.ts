import { test, expect, type Page } from '@playwright/test'
import { learningUnits } from '../../docs/.vitepress/data/learningUnits.ts'
import { masteryByUnitId } from '../../docs/.vitepress/data/masteryContent.ts'

// The loop band: five steps on every unit, read from the record and never from a tick on the band,
// the one 停在这里 moving on as the reader does things, and the same reading on the opening screen.

const steps = ['read', 'code', 'run', 'quiz', 'note']
const id = 'core-linear-structures'

async function open(page: Page) {
  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
}

function states(page: Page, unitId: string) {
  return page.locator(`#unit-${unitId} .unit-steps li`).evaluateAll((items) => items.map((item) => item.getAttribute('data-state')))
}

test('every unit carries the five steps, all pending before script and 停在这里 at 读 after it', async ({ page }) => {
  const response = await page.request.get('学习单元.html')
  const html = await response.text()
  const serverBands = html.match(/class="nb-steps unit-steps"/g) ?? []
  expect(serverBands.length).toBe(learningUnits.length)
  expect(html.match(/data-state="pending"/g)?.length).toBe(learningUnits.length * steps.length)
  expect(html).not.toContain('data-state="now"')

  await open(page)
  await expect(page.locator('.unit-steps')).toHaveCount(learningUnits.length)
  for (const unit of learningUnits.slice(0, 3)) {
    const band = page.locator(`#unit-${unit.id} .unit-steps`)
    await expect(band.locator('li')).toHaveText(['读', '看代码', '跑', '自测', '记一笔'])
    await expect.poll(() => states(page, unit.id)).toEqual(['now', 'pending', 'pending', 'pending', 'pending'])
  }
  // Each label links to the section of the unit that does the step.
  const hrefs = await page.locator(`#unit-${id} .unit-steps a`).evaluateAll((links) => links.map((link) => link.getAttribute('href')))
  expect(hrefs).toEqual([`#unit-title-${id}`, `#unit-source-${id}`, `#unit-run-${id}`, `#unit-quiz-${id}`, `#unit-note-${id}`])
  for (const href of hrefs) await expect(page.locator(href!)).toHaveCount(1)
})

test('each thing the reader does turns its step done and moves 停在这里 to the next, and the index continues there', async ({ page }) => {
  await open(page)
  const unit = page.locator(`#unit-${id}`)
  await unit.getByRole('button', { name: '开始学习', exact: true }).click()
  await expect.poll(() => states(page, id)).toEqual(['done', 'now', 'pending', 'pending', 'pending'])
  await expect(page.locator('.unit-continue')).toContainText('接着做：链表、栈、队列与双端队列 · 停在「看代码」')

  // 看代码: the panel shows a file once the details opens.
  await unit.locator('.unit-resources > summary').click()
  await expect(unit.locator('.source-panel .nb-code-line').first()).toBeVisible()
  await expect.poll(() => states(page, id)).toEqual(['done', 'done', 'now', 'pending', 'pending'])

  // 自测 is done by a correct answer even while 跑 is still where to continue.
  await unit.locator('.unit-mastery > summary').click()
  await unit.getByRole('radio').nth(masteryByUnitId[id].selfCheck.correctIndex).check()
  await unit.getByRole('button', { name: '提交自测' }).click()
  await expect.poll(() => states(page, id)).toEqual(['done', 'done', 'now', 'done', 'pending'])

  // 记一笔: a saved note is in the record; an empty one is not.
  const note = unit.locator('.unit-note-input')
  await note.fill('双端队列两头都能进出。')
  await unit.getByRole('button', { name: '保存笔记' }).click()
  await expect(unit.locator('.unit-note-status')).toHaveText('已保存')
  await expect.poll(() => states(page, id)).toEqual(['done', 'done', 'now', 'done', 'done'])
  await page.reload()
  await open(page)
  await expect(page.locator(`#unit-${id} .unit-note-input`)).toHaveValue('双端队列两头都能进出。')
  await expect.poll(() => states(page, id)).toEqual(['done', 'done', 'now', 'done', 'done'])

  // The opening screen reads the same record: this unit, at this step.
  await page.goto('./')
  await expect(page.locator('.continue-learning')).toHaveText('接着学：链表、栈、队列与双端队列')
  await expect(page.locator('.record-copy')).toContainText('停在「跑」')
  await expect(page.locator('.unit-register > [aria-current]')).toHaveAttribute('data-unit', id)

  // A second unit started later comes after in course order, so the continue reading stays on the first.
  await open(page)
  await page.locator('#unit-core-sorting').getByRole('button', { name: '开始学习', exact: true }).click()
  await expect.poll(() => states(page, 'core-sorting')).toEqual(['done', 'now', 'pending', 'pending', 'pending'])
  await expect(page.locator('.unit-continue')).toContainText('接着做：链表、栈、队列与双端队列')
  await expect(page.locator(`#unit-${id}`)).toHaveAttribute('aria-current', 'step')
  await expect(page.locator('#unit-core-sorting')).not.toHaveAttribute('aria-current', 'step')
})

test('the band wraps on a phone and the page never scrolls sideways', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await open(page)
  const band = page.locator(`#unit-${id} .unit-steps`)
  await expect(band).toBeVisible()
  const boxes = await band.locator('li').evaluateAll((items) => items.map((item) => item.getBoundingClientRect().top))
  expect(new Set(boxes.map(Math.round)).size).toBeGreaterThan(1)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= Math.min(window.innerWidth, document.documentElement.clientWidth))).toBe(true)
  await page.locator(`#unit-${id} .unit-resources > summary`).click()
  await expect(page.locator(`#unit-${id} .source-panel .nb-code-line`).first()).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= Math.min(window.innerWidth, document.documentElement.clientWidth))).toBe(true)
})
