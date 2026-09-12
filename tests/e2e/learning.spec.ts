import { learningUnits } from '../../docs/.vitepress/data/learningUnits.ts'
import { masteryByUnitId } from '../../docs/.vitepress/data/masteryContent.ts'
import { test, expect, type Page } from '@playwright/test'
const id = 'core-array-search'
async function open(page: Page) {
  await page.goto('学习单元.html')
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready','true')
}
test('two tabs save, synchronize and reset progress', async ({page,context}) => {
  const other = await context.newPage()
  await open(page);await open(other)
  await Promise.all([
    page.locator('#unit-'+id).getByRole('button',{name:'开始学习',exact:true}).click(),
    other.locator('#unit-core-linear-structures').getByRole('button',{name:'开始学习',exact:true}).click()
  ])
  for (const tab of [page,other]) {
    await tab.reload();await expect(tab.locator('.unit-explorer')).toHaveAttribute('data-ready','true')
    await expect(tab.locator('#unit-'+id+' .unit-status')).toHaveText('学习中')
    await expect(tab.locator('#unit-core-linear-structures .unit-status')).toHaveText('学习中')
  }
  await page.getByRole('button',{name:'清空本地进度',exact:true}).click()
  await page.getByRole('button',{name:'确认清空全部进度',exact:true}).click()
  await expect(other.locator('#unit-'+id+' .unit-status')).toHaveText('未开始')
})
test('self-check, evidence, review and export/import roundtrip', async ({page}) => {
  await page.clock.install({time:new Date('2026-08-01T12:00:00Z')})
  await open(page)
  const unit = page.locator('#unit-'+id)
  await unit.locator('.unit-mastery summary').click()
  await unit.getByRole('radio').nth(1).check()
  await unit.getByRole('button',{name:'提交自测'}).click()
  for (const box of await unit.getByRole('checkbox').all()) await box.check()
  await unit.getByRole('button',{name:'验证掌握',exact:true}).click()
  await expect(unit.locator('.unit-status')).toHaveText('已验证')
  await page.clock.fastForward(7*86_400_000+60_000)
  await expect(unit.locator('.unit-status')).toHaveText('待复习')
  await unit.getByRole('radio').nth(0).check()
  await expect(unit.locator('.unit-status')).toHaveText('待复习')
  await unit.getByRole('radio').nth(1).check()
  await unit.getByRole('button',{name:'提交自测'}).click()
  await unit.getByRole('button',{name:'重新答对自测后完成复习'}).click()
  await expect(unit.locator('.unit-status')).toHaveText('已验证')
  const downloaded = page.waitForEvent('download')
  await page.getByRole('button',{name:'导出进度备份'}).click()
  const download = await downloaded
  expect(download.suggestedFilename()).toBe('learning-progress-v3.json')
  await page.getByLabel('导入进度备份').setInputFiles((await download.path())!)
  await expect(page.getByText('新增 0 · 更新 0 · 删除 0 · 忽略 1')).toBeVisible()
  await page.getByRole('button',{name:'确认合并备份'}).click()
  await expect(page.getByText('备份已合并并保存。')).toBeVisible()
})
test('legacy migration and unavailable storage remain usable on mobile', async ({page}) => {
  await page.setViewportSize({width:390,height:844})
  await page.addInitScript(() => {
    localStorage.setItem('dsa-learning-progress-v1',JSON.stringify(['core-array-search']))
    Object.defineProperty(window,'indexedDB',{get(){throw new Error('disabled')}})
  })
  await open(page)
  await expect(page.getByText(/尚未持久化，请导出备份/)).toBeVisible()
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('学习中')
  await expect(page.getByRole('button',{name:'导出进度备份'})).toBeEnabled()
  expect(await page.evaluate(()=>document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})
test('invalid backup leaves progress intact and keyboard navigation works', async ({page}) => {
  await open(page)
  await page.locator('#unit-'+id).getByRole('button',{name:'开始学习',exact:true}).click()
  await page.getByLabel('导入进度备份').setInputFiles({name:'bad.json',mimeType:'application/json',buffer:Buffer.from('{"schemaVersion":99}')})
  await expect(page.getByText('不支持的备份版本。')).toBeVisible()
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('学习中')
  await page.getByRole('searchbox').focus();await page.keyboard.type('不存在的单元')
  await expect(page.getByText('没有匹配的学习单元')).toBeVisible()
})

test('a failed write is shown as unsaved and can be exported', async ({page}) => {
  await open(page)
  await page.evaluate(() => {
    IDBObjectStore.prototype.put = function () { throw new DOMException('quota', 'QuotaExceededError') }
  })
  await page.locator('#unit-'+id).getByRole('button',{name:'开始学习',exact:true}).click()
  await expect(page.getByText(/尚未持久化，请导出备份/)).toBeVisible()
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('学习中')
  await expect(page.getByRole('button',{name:'导出进度备份'})).toBeEnabled()
  await page.reload()
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready','true')
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('未开始')
})
test('migration occurs once and a reset is not resurrected from legacy bytes', async ({page}) => {
  await page.addInitScript(() => localStorage.setItem('dsa-learning-progress-v1', JSON.stringify(['core-array-search'])))
  await open(page)
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('学习中')
  await page.getByRole('button',{name:'清空本地进度',exact:true}).click()
  await page.getByRole('button',{name:'确认清空全部进度',exact:true}).click()
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('未开始')
  await page.reload()
  await expect(page.locator('.unit-explorer')).toHaveAttribute('data-ready','true')
  await expect(page.locator('#unit-'+id+' .unit-status')).toHaveText('未开始')
})
test('prerequisites gate verification and newer backup records merge atomically', async ({page}) => {
  await open(page)
  const dependent = learningUnits.find(unit => unit.prerequisites.includes(id))!
  const unit = page.locator('#unit-'+dependent.id)
  await unit.locator('.unit-mastery summary').click()
  await unit.getByRole('radio').nth(masteryByUnitId[dependent.id].selfCheck.correctIndex).check()
  await unit.getByRole('button',{name:'提交自测'}).click()
  for (const box of await unit.getByRole('checkbox').all()) await box.check()
  await expect(unit.getByRole('button',{name:'验证掌握',exact:true})).toBeDisabled()
  const now = Date.now()
  const records = dependent.prerequisites.map(key=>({
    id:key,record:{evidenceIds:['invariant','test','transfer'],evidenceRevision:1,
      selfTestPassedAt:now,selfTestRevision:1,verifiedAt:now,verifiedRevision:1,
      reviewRound:0,nextReviewAt:now+7*86400000,startedAt:now,updatedAt:now}
  }))
  await page.getByLabel('导入进度备份').setInputFiles({
    name:'progress.json',mimeType:'application/json',buffer:Buffer.from(JSON.stringify({schemaVersion:3,records}))
  })
  await page.getByRole('button',{name:'确认合并备份'}).click()
  await expect(unit.getByRole('button',{name:'验证掌握',exact:true})).toBeEnabled()
  await unit.getByRole('button',{name:'验证掌握',exact:true}).click()
  await expect(unit.locator('.unit-status')).toHaveText('已验证')
})

test('a suspended tab cannot resurrect a unit or an older storage generation', async ({page,context}) => {
  await open(page)
  const primary = page.locator('#unit-'+id)
  await primary.getByRole('button',{name:'开始学习',exact:true}).click()
  await expect(primary.locator('.unit-status')).toHaveText('学习中')
  const stale = await context.newPage()
  await stale.addInitScript(() => {
    // Simulate a background tab that has not received either refresh signal.
    Object.defineProperty(window, 'BroadcastChannel', { value: undefined })
    window.addEventListener('focus', event => event.stopImmediatePropagation(), true)
  })
  await open(stale)
  const old = stale.locator('#unit-'+id)
  await old.locator('.unit-mastery summary').click()
  await primary.locator('.unit-mastery summary').click()
  await primary.getByRole('button',{name:'重置此单元',exact:true}).click()
  await expect(primary.locator('.unit-status')).toHaveText('未开始')
  await expect(old.locator('.unit-status')).toHaveText('学习中')
  await old.getByRole('checkbox').first().check()
  await expect(stale.getByText('进度已在其他页面重置，请根据最新状态重新操作。')).toBeVisible()
  await expect(old.locator('.unit-status')).toHaveText('未开始')

  await primary.getByRole('button',{name:'开始学习',exact:true}).click()
  await expect(primary.locator('.unit-status')).toHaveText('学习中')
  await page.getByRole('button',{name:'清空本地进度',exact:true}).click()
  await page.getByRole('button',{name:'确认清空全部进度',exact:true}).click()
  await expect(primary.locator('.unit-status')).toHaveText('未开始')
  await old.getByRole('button',{name:'开始学习',exact:true}).click()
  await expect(old.locator('.unit-status')).toHaveText('未开始')
  await stale.reload()
  await expect(stale.locator('.unit-explorer')).toHaveAttribute('data-ready','true')
  await expect(old.locator('.unit-status')).toHaveText('未开始')
})
