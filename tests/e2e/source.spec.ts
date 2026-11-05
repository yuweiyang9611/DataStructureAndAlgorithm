import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { test, expect } from '@playwright/test'
import { learningUnits } from '../../docs/.vitepress/data/learningUnits.ts'

// Source read in place: a unit shows its first implementation and test file in the kit's code panel,
// the source page shows any bundled file or directory, and nothing sends the reader to GitHub for a
// file the site carries.

const distRoot = join('docs', '.vitepress', 'dist')
const bundle = new Set((JSON.parse(readFileSync(join(distRoot, 'source', 'index.json'), 'utf8')) as { files: { path: string }[] }).files.map((file) => file.path))
const firstUnit = learningUnits[0]

function htmlFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name)
    return statSync(path).isDirectory() ? htmlFiles(path) : name.endsWith('.html') ? [path] : []
  })
}

test('a unit opens its source and test in the panel, coloured and numbered, and links GitHub only from the panel', async ({ page }) => {
  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  const unit = page.locator(`#unit-${firstUnit.id}`)
  const panel = unit.locator('.source-panel')
  const requests: string[] = []
  page.on('request', (request) => { if (request.url().includes('/source/')) requests.push(request.url()) })
  // Closed details fetch nothing.
  await expect(panel.locator('.nb-code-tab')).toHaveCount(2)
  expect(requests).toEqual([])
  await unit.locator('.unit-resources > summary').click()
  await expect(panel.locator('.nb-code-line').first()).toBeVisible()
  const lineCount = await panel.locator('.nb-code-line').count()
  const text = readFileSync(firstUnit.sources[0], 'utf8').replace(/\r\n?/g, '\n')
  expect(lineCount).toBe(text.split('\n').length - (text.endsWith('\n') ? 1 : 0))
  await expect(panel.locator('.nb-code-line').first()).toHaveAttribute('data-line', '1')
  await expect(panel.locator('.nb-code-note')).toHaveText(`${lineCount} 行`)
  expect(await panel.locator('.nb-code-line .nb-code-text .tok-keyword').count()).toBeGreaterThan(0)
  expect(await panel.locator('.nb-code-line .nb-code-text .tok-type').count()).toBeGreaterThan(0)
  // One text span per line, so the grid stays two columns.
  expect(await panel.locator('.nb-code-line').first().evaluate((line) => line.children.length)).toBe(1)
  await expect(panel.locator('.nb-code-link')).toHaveAttribute('href', `https://github.com/yuweiyang9611/DataStructureAndAlgorithm/blob/main/${firstUnit.sources[0]}`)
  await expect(panel.locator('.nb-code-tab[aria-selected="true"]')).toHaveText(firstUnit.sources[0].split('/').pop()!)

  await panel.locator('.nb-code-tab').nth(1).click()
  await expect(panel.locator('.nb-code-tab[aria-selected="true"]')).toHaveText(firstUnit.tests[0].split('/').pop()!)
  await expect(panel.locator('.nb-code-link')).toHaveAttribute('href', `https://github.com/yuweiyang9611/DataStructureAndAlgorithm/blob/main/${firstUnit.tests[0]}`)
  await expect(panel.locator('.nb-code-line').first()).toBeVisible()
  expect(requests.length).toBe(2)

  // The listed files open on the site, not on GitHub.
  for (const link of await unit.locator('.unit-resource-grid a').all()) {
    expect(await link.getAttribute('href')).toMatch(/^\/DataStructureAndAlgorithm\/源码\.html\?path=/)
  }
})

test('the source page shows a file with a marked range and its siblings, a directory, and a missing file', async ({ page }) => {
  const path = 'DataStructureAndAlgorithm/Tree/AvlTree.cs'
  await page.goto(`源码.html?path=${encodeURIComponent(path)}#L5-L9`)
  const panel = page.locator('.source-page-panel')
  await expect(page.locator('#source-title')).toHaveText('AvlTree.cs')
  await expect(page.locator('.source-crumbs')).toContainText('源码')
  await expect(page.locator('.source-crumbs a').nth(2)).toHaveAttribute('href', '/DataStructureAndAlgorithm/源码.html?dir=DataStructureAndAlgorithm%2FTree')
  await expect(panel.locator('.nb-code-line[data-marked]')).toHaveCount(5)
  await expect(panel.locator('#L5')).toHaveAttribute('data-marked', '')
  await expect(panel.locator('#L10')).not.toHaveAttribute('data-marked', '')
  expect(await panel.locator('.nb-code-line').count()).toBeGreaterThan(100)
  expect(await page.locator('.source-siblings li').count()).toBeGreaterThan(3)
  await expect(page).toHaveTitle(/^AvlTree\.cs · 源码/)

  // A sibling opens in place: the router keeps the page and the browser reads the new query.
  await page.locator('.source-siblings a', { hasText: 'Trie.cs' }).click()
  await expect(page.locator('#source-title')).toHaveText('Trie.cs')
  await expect(page.locator('.source-page-panel .nb-code-tab[aria-selected="true"]')).toHaveText('Trie.cs')
  await expect(page.locator('.source-page-panel .nb-code-line[data-marked]')).toHaveCount(0)
  await page.goBack()
  await expect(page.locator('#source-title')).toHaveText('AvlTree.cs')

  await page.goto('源码.html?dir=DataStructureAndAlgorithm%2FTree')
  await expect(page.locator('#source-title')).toHaveText('Tree/')
  const rows = page.locator('.source-row')
  expect(await rows.count()).toBeGreaterThan(5)
  await expect(rows.first().locator('span')).toHaveText(/^\d+ 行 · \d+\.\d KB$/)
  await expect(rows.filter({ hasText: 'AvlTree.cs' }).locator('a')).toHaveAttribute('href', `/DataStructureAndAlgorithm/源码.html?path=${encodeURIComponent(path)}`)

  await page.goto('源码.html')
  await expect(page.locator('#source-title')).toHaveText('源码')
  const roots = page.locator('.source-row')
  await expect(roots.filter({ hasText: 'DataStructureAndAlgorithm/' }).first().locator('span')).toHaveText(/^\d+ 个文件 · \d+ 行$/)
  expect(await roots.count()).toBeGreaterThan(8)

  await page.goto('源码.html?path=nowhere%2Fmissing.cs')
  await expect(page.locator('#source-title')).toHaveText('没有这个文件')
  await expect(page.locator('.source-row').first()).toBeVisible()
})

test('the Trace lab links the implementation on the site', async ({ page }) => {
  await page.goto('追踪实验室.html')
  await expect(page.locator('.trace-event')).toBeVisible()
  await expect(page.getByRole('link', { name: '查看实现' })).toHaveAttribute('href', /^\/DataStructureAndAlgorithm\/源码\.html\?path=/)
})

test('the panel scrolls inside itself on a phone and the page never scrolls sideways', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready', 'true')
  const unit = page.locator(`#unit-${firstUnit.id}`)
  await unit.locator('.unit-resources > summary').click()
  const view = unit.locator('.source-panel .nb-code-view')
  await expect(unit.locator('.source-panel .nb-code-line').first()).toBeVisible()
  expect(await view.evaluate((pre) => pre.scrollWidth > pre.clientWidth)).toBe(true)
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)

  await page.goto('源码.html?path=DataStructureAndAlgorithm%2FTree%2FAvlTree.cs')
  await expect(page.locator('.source-page-panel .nb-code-line').first()).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.goto('源码.html?dir=DataStructureAndAlgorithm.Test%2FIntegratedProjectsTest')
  await expect(page.locator('.source-row').first()).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

test('no built page sends the reader to GitHub for a bundled file or folder', () => {
  expect(bundle.size).toBeGreaterThanOrEqual(200)
  for (const unit of learningUnits) for (const path of [...unit.sources, ...unit.tests]) expect(bundle.has(path), path).toBe(true)
  const pattern = /href="https:\/\/github\.com\/yuweiyang9611\/DataStructureAndAlgorithm\/(blob|tree)\/[^/"]+\/([^"#?]+)/g
  for (const file of htmlFiles(distRoot)) {
    const html = readFileSync(file, 'utf8').replace(/<noscript>[\s\S]*?<\/noscript>/g, '')
    for (const match of html.matchAll(pattern)) {
      const target = decodeURIComponent(match[2]).replace(/\/+$/, '')
      const bundled = match[1] === 'blob' ? bundle.has(target) : [...bundle].some((path) => path.startsWith(`${target}/`))
      expect(bundled, `${file} -> ${target}`).toBe(false)
    }
  }
  const navigation = readFileSync(join(distRoot, '源码导航.html'), 'utf8')
  expect(navigation).toContain('/DataStructureAndAlgorithm/源码.html?dir=DataStructureAndAlgorithm/Linear')
})
