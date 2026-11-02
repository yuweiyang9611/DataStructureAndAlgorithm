import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { applyAction, emptyStore, reviewIntervals, type ProgressStore } from '../../docs/.vitepress/data/progress.ts'
import { daysAgo, orderedUnits, readCourse, stepsDone, unitSteps } from '../../docs/.vitepress/data/record.ts'
import { countLoose, haltHtml, haltParts, looseMarks } from '../../docs/.vitepress/data/halt.ts'

const now = 1_700_000_000_000
const day = 86_400_000

function master(store: ProgressStore, id: string, at: number) {
  let next = applyAction(store, { type: 'start', id }, at)
  for (const evidenceId of ['invariant', 'test', 'transfer'] as const) next = applyAction(next, { type: 'evidence', id, evidenceId, checked: true }, at)
  next = applyAction(next, { type: 'answer', id, correct: true }, at)
  return applyAction(next, { type: 'verify', id }, at)
}

test('an empty record offers the first unit and marks nothing', () => {
  const course = readCourse(emptyStore(), now)
  assert.equal(course.hasRecord, false)
  assert.equal(course.kind, 'empty')
  assert.equal(course.unit?.id, orderedUnits[0].id)
  assert.equal(course.unit?.prerequisites.length, 0)
  assert.equal(course.verified, 0)
  assert.equal(course.total, 22)
})

test('the stopped place is the unfinished unit touched last, then a due review, then the next unit', () => {
  let store = master(emptyStore(), 'core-linear-structures', now - 9 * day)
  store = applyAction(store, { type: 'start', id: 'core-array-search' }, now - 3 * day)
  store = applyAction(store, { type: 'start', id: 'core-sorting' }, now - 4 * day)
  let course = readCourse(store, now)
  assert.equal(course.kind, 'learning')
  assert.equal(course.unit?.id, 'core-array-search')
  // A review has come due as well, but the unit in progress is where the owner stopped.
  assert.deepEqual([course.verified, course.learning, course.due], [1, 2, 1])
  assert.equal(course.lastUpdatedAt, now - 3 * day)

  store = applyAction(store, { type: 'reset', id: 'core-array-search' }, now - 2 * day)
  store = applyAction(store, { type: 'reset', id: 'core-sorting' }, now - 2 * day)
  course = readCourse(store, now - 2 * day - 1)
  assert.equal(course.kind, 'next')
  assert.equal(course.unit?.id, orderedUnits.find((unit) => unit.id !== 'core-linear-structures' && unit.prerequisites.every((id) => id === 'core-linear-structures'))?.id)

  course = readCourse(store, now - 9 * day + reviewIntervals[0] + 1)
  assert.equal(course.kind, 'review')
  assert.equal(course.unit?.id, 'core-linear-structures')
  assert.equal(course.due, 1)
})

test('every unit verified leaves no stopped place', () => {
  let store = emptyStore()
  for (const unit of orderedUnits) store = master(store, unit.id, now)
  const course = readCourse(store, now)
  assert.equal(course.kind, 'complete')
  assert.equal(course.unit, undefined)
  assert.equal(course.verified, 22)
})

test('the record line reaches as far as the unit has gone, and is solid only once verified', () => {
  const id = 'core-array-search'
  let store = applyAction(emptyStore(), { type: 'start', id }, now)
  assert.equal(stepsDone(store.units[id], now), 0)
  store = applyAction(store, { type: 'answer', id, correct: true }, now)
  store = applyAction(store, { type: 'evidence', id, evidenceId: 'test', checked: true }, now)
  assert.equal(stepsDone(store.units[id], now), 2)
  store = master(store, id, now)
  assert.equal(stepsDone(store.units[id], now), unitSteps)
  assert.equal(stepsDone(undefined, now), 0)
})

test('days since the last record read as words', () => {
  const noon = new Date(2026, 10, 3, 12).getTime()
  assert.equal(daysAgo(noon - 3_600_000, noon), '今天')
  assert.equal(daysAgo(noon - day, noon), '昨天')
  assert.equal(daysAgo(noon - 5 * day, noon), '5 天前')
})

test('touching marks are squeezed exactly as kit/v1/theme.js squeezes them', () => {
  const kit = readFileSync(new URL('../../docs/public/kit/theme.js', import.meta.url), 'utf8')
  assert.equal(kit.match(/const looseMarks = \/(.+)\/g;/)?.[1], looseMarks.source)
  assert.deepEqual(haltParts('标记为“已验证”。'), [
    { text: '标记为“已验证', halt: false },
    { text: '”', halt: true },
    { text: '。', halt: false }
  ])
  assert.deepEqual(haltParts('（“对”）'), [{ text: '（“对”）', halt: false }])
  assert.equal(haltHtml('甲。“乙”，丙'), '甲<span class="nb-halt">。</span>“乙<span class="nb-halt">”</span>，丙')
  assert.equal(countLoose('a”，b，“c'), 2)
})
