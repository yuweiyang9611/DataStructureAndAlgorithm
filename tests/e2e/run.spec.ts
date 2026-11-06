import { test, expect, type Page } from '@playwright/test'

// Run in the browser: a unit's 试一试 demo compiles and runs on the course's Roslyn + .NET
// WebAssembly runtime (built locally by docs:runtime), the output lands under the panel, the
// band's 跑 step turns done, and an edited copy runs too. The first run loads the runtime, so the
// waits allow its 60 s load timeout on top of compile and execute.

const runTimeout = 150_000

async function openUnit(page: Page, id: string) {
  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  const unit = page.locator(`#unit-${id}`)
  await unit.locator('.unit-resources > summary').click()
  const panel = unit.locator('.source-panel')
  await expect(panel.locator('.nb-code-tab[aria-selected="true"]')).toHaveText('试一试')
  await expect(panel.locator('.nb-code-line').first()).toBeVisible()
  return { unit, panel }
}

test('the 试一试 demo runs in the browser and prints what the unit is about', async ({ page }) => {
  test.setTimeout(runTimeout + 60_000)
  const { unit, panel } = await openUnit(page, 'core-sorting')
  await expect(panel.locator('.nb-code-line .nb-code-text .tok-keyword').first()).toBeVisible()
  await expect(unit.locator('.unit-steps [data-step="run"]')).toHaveAttribute('data-state', 'now')

  await panel.getByRole('button', { name: '运行', exact: true }).click()
  const output = panel.locator('.nb-code-output')
  await expect(output).toHaveAttribute('data-state', 'running')
  // The stage words in order: the runtime loads, then the demo compiles, then it executes.
  await expect(panel.locator('.nb-code-output-label')).toHaveText(/加载运行时中|编译中|执行中|运行中/)
  await expect(output).toHaveAttribute('data-state', 'done', { timeout: runTimeout })
  await expect(panel.locator('.nb-code-output-label')).toHaveText('运行结果 · 浏览器内 .NET')
  const text = await output.locator('pre').textContent()
  expect(text).toContain('输入：38, 27, 43, 3, 9, 82, 10, 27')
  expect(text).toContain('快速排序：3, 9, 10, 27, 27, 38, 43, 82')
  expect(text).toContain('基数排序（含负数）：-90, -45, 2, 24, 66, 75, 170, 802')

  // The run is in the record: 跑 is done and 自测 is where to continue, here and after a reload.
  await expect(unit.locator('.unit-steps [data-step="run"]')).toHaveAttribute('data-state', 'done')
  await expect(unit.locator('.unit-steps [data-step="quiz"]')).toHaveAttribute('data-state', 'now')
  await page.reload()
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  await expect(page.locator('#unit-core-sorting .unit-steps [data-step="run"]')).toHaveAttribute('data-state', 'done')
})

test('an edited copy runs and a copy that does not compile says so with the line', async ({ page }) => {
  test.setTimeout(runTimeout * 2 + 60_000)
  const { panel } = await openUnit(page, 'advanced-disjoint-set')
  await panel.getByRole('button', { name: '改一改再跑', exact: true }).click()
  const editor = panel.locator('.nb-code-editor')
  await expect(editor).toBeVisible()
  await expect(panel.locator('.nb-code-view')).toBeHidden()
  await expect(panel.locator('.nb-code-status')).toHaveText('这是副本，改完点运行；原文件不会变。')
  const original = await editor.inputValue()
  expect(original).toContain('六座城市')
  await editor.fill(original.replace('六座城市，起初', '改过的副本：六座城市，起初'))
  await panel.getByRole('button', { name: '运行', exact: true }).click()
  const output = panel.locator('.nb-code-output')
  await expect(output).toHaveAttribute('data-state', 'done', { timeout: runTimeout })
  await expect(panel.locator('.nb-code-output-label')).toHaveText('运行结果 · 浏览器内 .NET · 已改动的副本')
  await expect(output.locator('pre')).toContainText('改过的副本：六座城市，起初 6 个连通块')

  await editor.fill('public static class Demo { public static void Run() { Console.WriteLine(missing); } }')
  await panel.getByRole('button', { name: '运行', exact: true }).click()
  await expect(output).toHaveAttribute('data-state', 'wrong', { timeout: runTimeout })
  await expect(panel.locator('.nb-code-output-label')).toHaveText('编译失败')
  await expect(output.locator('pre')).toContainText(/Error \(1,\d+\)/)

  // 恢复原文 puts the file back and the tabs stay honest about what is shown.
  await panel.getByRole('button', { name: '恢复原文', exact: true }).click()
  await expect(editor).toBeHidden()
  await expect(panel.locator('.nb-code-view')).toBeVisible()
  await expect(panel.locator('.nb-code-status')).toHaveText('已恢复原文。')
  await panel.locator('.nb-code-tab').nth(1).click()
  await expect(panel.locator('.nb-code-tab[aria-selected="true"]')).toHaveText('DisjointSet.cs')
  await expect(panel.getByRole('button', { name: '运行', exact: true })).toHaveCount(0)
})
