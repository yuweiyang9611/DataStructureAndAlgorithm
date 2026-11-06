import { test } from 'node:test'
import assert from 'node:assert/strict'
import { IDBFactory } from 'fake-indexeddb'
import { bandDone, nowStep, pendingBand, stepLabels, stepOrder, unitBand } from '../../docs/.vitepress/data/band.ts'
import { demoChecks, demoLabel, demoPath, demoPaths } from '../../docs/.vitepress/data/demos.ts'
import { applyAction, baseRecord, emptyStore, exportBackup, mergeBackup, noteLimit, parseBackup, ProgressRuleError, upgradeStore, validateRecord } from '../../docs/.vitepress/data/progress.ts'
import { ProgressRepository } from '../../docs/.vitepress/data/progressRepository.ts'

const id = 'core-array-search'
const now = 1_700_000_000_000
const expected = { generation: 0, deletedAt: 0 }

test('the band has five steps in the loop order, all pending without a record read', () => {
  assert.deepEqual(stepOrder, ['read', 'code', 'run', 'quiz', 'note'])
  assert.deepEqual(pendingBand().map((step) => [step.id, step.label, step.state]), stepOrder.map((step) => [step, stepLabels[step], 'pending']))
  assert.equal(pendingBand().some((step) => step.state === 'now'), false)
})

test('a record with nothing done stops at 读; each thing the reader does marks its step done and moves 停在这里 on', () => {
  const states = (store: ReturnType<typeof emptyStore>) => unitBand(store.units[id]).map((step) => step.state)
  assert.deepEqual(unitBand(undefined).map((step) => step.state), ['now', 'pending', 'pending', 'pending', 'pending'])

  let store = applyAction(emptyStore(), { type: 'start', id }, now)
  assert.deepEqual(states(store), ['done', 'now', 'pending', 'pending', 'pending'])
  assert.equal(nowStep(store.units[id])?.id, 'code')

  store = applyAction(store, { type: 'open-source', id }, now + 1)
  assert.deepEqual(states(store), ['done', 'done', 'now', 'pending', 'pending'])
  assert.equal(store.units[id].sourceOpenedAt, now + 1)
  // Opening the panel again changes nothing in the record.
  assert.equal(applyAction(store, { type: 'open-source', id }, now + 2), store)

  store = applyAction(store, { type: 'ran', id }, now + 3)
  assert.deepEqual(states(store), ['done', 'done', 'done', 'now', 'pending'])
  assert.equal(store.units[id].ranAt, now + 3)

  store = applyAction(store, { type: 'answer', id, correct: true }, now + 4)
  assert.deepEqual(states(store), ['done', 'done', 'done', 'done', 'now'])
  assert.equal(nowStep(store.units[id])?.label, '记一笔')

  store = applyAction(store, { type: 'note', id, text: '区间都是半开的。' }, now + 5)
  assert.deepEqual(states(store), ['done', 'done', 'done', 'done', 'done'])
  assert.equal(nowStep(store.units[id]), undefined)
  assert.equal(bandDone(store.units[id]), 5)

  // A wrong answer takes the quiz back; a cleared note takes the note back; exactly one 停在这里 returns.
  store = applyAction(store, { type: 'answer', id, correct: false }, now + 6)
  assert.deepEqual(states(store), ['done', 'done', 'done', 'now', 'done'])
  store = applyAction(store, { type: 'note', id, text: '   ' }, now + 7)
  assert.equal(store.units[id].note, undefined)
  assert.deepEqual(states(store), ['done', 'done', 'done', 'now', 'pending'])
  assert.equal(states(store).filter((state) => state === 'now').length, 1)
})

test('the band steps come from the record alone: a panel step on an untouched unit creates its record', () => {
  const store = applyAction(emptyStore(), { type: 'ran', id }, now)
  assert.equal(store.units[id].startedAt, now)
  // 读 is done with the record, 跑 is done, 看代码 is where to continue, and a done step after 停在这里 stays done.
  assert.deepEqual(unitBand(store.units[id]).map((step) => step.state), ['done', 'now', 'done', 'pending', 'pending'])
})

test('all notes together stay under 20 KB, in the record and in a backup', () => {
  const long = 'x'.repeat(noteLimit - 10)
  let store = applyAction(emptyStore(), { type: 'note', id, text: long }, now)
  assert.equal(store.units[id].note, long)
  assert.throws(() => applyAction(store, { type: 'note', id: 'core-sorting', text: '再多一点点就超了' }, now + 1), ProgressRuleError)
  // The failed save left the store as it was.
  assert.equal(store.units['core-sorting'], undefined)
  store = applyAction(store, { type: 'note', id, text: '短一点' }, now + 2)
  store = applyAction(store, { type: 'note', id: 'core-sorting', text: '现在放得下' }, now + 3)
  assert.equal(store.units['core-sorting'].note, '现在放得下')

  assert.throws(() => validateRecord({ ...baseRecord(now), note: 'x'.repeat(noteLimit + 1) }), /笔记/)
  assert.throws(() => validateRecord({ ...baseRecord(now), note: 42 }), /笔记/)
  const backup = { schemaVersion: 4, records: [{ id, record: { ...baseRecord(now + 10), note: long } }, { id: 'core-sorting', record: { ...baseRecord(now + 10), note: '多出来的' } }] }
  assert.throws(() => mergeBackup(emptyStore(), parseBackup(backup)), ProgressRuleError)
})

test('a backup carries the band fields and the record the live site writes today still loads', () => {
  let store = applyAction(emptyStore(), { type: 'start', id }, now)
  store = applyAction(store, { type: 'open-source', id }, now + 1)
  store = applyAction(store, { type: 'ran', id }, now + 2)
  store = applyAction(store, { type: 'note', id, text: '笔记' }, now + 3)
  const backup = exportBackup(store)
  assert.equal(backup.schemaVersion, 4)
  const merged = mergeBackup(emptyStore(), parseBackup(backup)).store
  assert.deepEqual(merged.units[id], store.units[id])
  assert.equal(merged.units[id].sourceOpenedAt, now + 1)
  assert.equal(merged.units[id].ranAt, now + 2)
  assert.equal(merged.units[id].note, '笔记')

  // A schema 3 backup (no band fields) and a schema 3 store are read as schema 4 with every step from the fields they have.
  const v3Backup = { schemaVersion: 3, records: [{ id, record: baseRecord(now) }] }
  assert.equal(mergeBackup(emptyStore(), parseBackup(v3Backup)).counts.added, 1)
  const v3Store = { schemaVersion: 3, generation: 2, revision: 5, units: { [id]: { ...baseRecord(now), selfTestPassedAt: now, selfTestRevision: 1 } }, deleted: { 'core-sorting': now } }
  const upgraded = upgradeStore(v3Store)
  assert.equal(upgraded.schemaVersion, 4)
  assert.deepEqual([upgraded.generation, upgraded.revision, upgraded.deleted], [2, 5, { 'core-sorting': now }])
  assert.deepEqual(unitBand(upgraded.units[id]).map((step) => step.state), ['done', 'now', 'pending', 'done', 'pending'])
  assert.equal(upgradeStore({ schemaVersion: 2, units: {} }).schemaVersion, 4)
  assert.deepEqual(upgradeStore({ schemaVersion: 2, units: {} }).units, {})
  assert.deepEqual(upgradeStore(null), emptyStore())
})

test('the repository upgrades a stored schema 3 record in place and keeps it', async () => {
  const factory = new IDBFactory()
  const v3Store = { schemaVersion: 3, generation: 0, revision: 1, units: { [id]: baseRecord(now) }, deleted: {} }
  await new Promise<void>((resolve, reject) => {
    const request = factory.open('dsa-learning-progress', 1)
    request.onupgradeneeded = () => request.result.createObjectStore('progress')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const transaction = request.result.transaction('progress', 'readwrite')
      transaction.objectStore('progress').put(v3Store, 'current')
      transaction.oncomplete = () => { request.result.close(); resolve() }
      transaction.onerror = () => reject(transaction.error)
    }
  })
  const repository = await ProgressRepository.open(factory)
  try {
    const initial = await repository.initialize(emptyStore())
    assert.equal(initial.schemaVersion, 4)
    assert.equal(initial.units[id].startedAt, now)
    const next = await repository.dispatch({ type: 'ran', id }, expected, now + 1)
    assert.equal(next.schemaVersion, 4)
    assert.equal(next.units[id].ranAt, now + 1)
    assert.equal((await repository.read()).units[id].ranAt, now + 1)
  } finally { repository.close() }
})

test('every unit has a demo at a fixed path, and the checks tree supplies the entry the engine looks for', () => {
  assert.equal(demoPaths.length, 22)
  assert.equal(demoPath('core-sorting'), 'docs/demos/core-sorting.cs')
  assert.equal(demoLabel, '试一试')
  assert.match(demoChecks, /public static class ExerciseChecks/)
  assert.match(demoChecks, /public static string\[\] Run\(\)/)
  assert.match(demoChecks, /Demo\.Run\(\);/)
  assert.match(demoChecks, /^global using System;/m)
})
