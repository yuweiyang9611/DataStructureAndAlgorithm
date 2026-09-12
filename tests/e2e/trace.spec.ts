import { readFileSync } from 'node:fs'
import { test, expect } from '@playwright/test'

test('failed manifest retries, playback advances and pause freezes the current step', async ({page}) => {
  await page.clock.install({time:new Date('2026-09-12T00:00:00Z')})
  let failed = false
  await page.route('**/traces/manifest.json',async route=>{
    if (!failed) { failed=true;await route.fulfill({status:503,body:'unavailable'}) }
    else await route.continue()
  })
  await page.goto('追踪实验室.html')
  await expect(page.getByRole('alert')).toContainText('503')
  await page.getByRole('button',{name:'重新加载'}).click()
  await expect(page.locator('.trace-step')).toHaveText('STEP 1')
  await page.clock.pauseAt(new Date('2026-09-12T00:01:00Z'))
  await page.locator('.trace-lab').focus();await page.keyboard.press('ArrowRight')
  await expect(page.locator('.trace-step')).toHaveText('STEP 2')
  await page.getByRole('button',{name:'播放',exact:true}).click()
  await page.clock.runFor(1250)
  await expect(page.locator('.trace-step')).toHaveText('STEP 3')
  await page.getByRole('button',{name:'暂停',exact:true}).click()
  await page.clock.runFor(5000)
  await expect(page.locator('.trace-step')).toHaveText('STEP 3')
  await page.locator('.trace-lab').focus();await page.keyboard.press('ArrowLeft');await page.keyboard.press('Space')
  await page.clock.runFor(1250)
  await expect(page.locator('.trace-step')).toHaveText('STEP 3')
  await page.keyboard.press('Space')
  await page.clock.runFor(5000)
  await expect(page.locator('.trace-step')).toHaveText('STEP 3')
  const filter = page.getByLabel('阶段筛选')
  await filter.selectOption({index:1})
  await expect(page.locator('.trace-operation')).toHaveText((await filter.locator('option:checked').textContent())!)
})

test('a late response cannot replace the selected trace; failed payload retries', async ({page}) => {
  // Exercise sequence protection even if the transport cannot cancel an in-flight response.
  await page.addInitScript(() => {
    const fetch = window.fetch.bind(window)
    window.fetch = (input, init) => fetch(input, {...init, signal:undefined})
  })
  await page.goto('追踪实验室.html')
  await expect(page.locator('.trace-event')).toBeVisible()
  const select=page.getByLabel('选择场景')
  const ids=await select.locator('option').evaluateAll(nodes=>nodes.map(node=>(node as HTMLOptionElement).value))
  const expected=JSON.parse(readFileSync('docs/public/traces/'+ids[2]+'.json','utf8')).trace[0]
  let release!:()=>void, started!:()=>void
  const held=new Promise<void>(resolve=>{release=resolve})
  const received=new Promise<void>(resolve=>{started=resolve})
  await page.route('**/traces/'+ids[1]+'.json',async route=>{
    const response=await route.fetch()
    started()
    await held
    await route.fulfill({response})
  })
  await select.selectOption(ids[1]);await received
  await select.selectOption(ids[2])
  await expect(page.locator('.trace-event h3')).toHaveText(expected.description)
  const late=page.waitForResponse('**/traces/'+ids[1]+'.json')
  release()
  await (await late).finished()
  await page.evaluate(()=>new Promise(requestAnimationFrame))
  await expect(page).toHaveURL(new RegExp('trace='+ids[2]))
  await expect(page.locator('.trace-event h3')).toHaveText(expected.description)
  const raw=await page.locator('.trace-raw code').first().textContent()
  expect(JSON.parse(raw!)).toEqual(expected)
  await page.route('**/traces/'+ids[0]+'.json',route=>route.fulfill({status:500,body:'failed'}),{times:1})
  await select.selectOption(ids[0])
  await expect(page.getByRole('alert')).toContainText('500')
  await page.getByRole('button',{name:'重新加载'}).click()
  await expect(page).toHaveURL(new RegExp('trace='+ids[0]))
  await expect(page.locator('.trace-step')).toHaveText('STEP 1')
})
