import { test } from 'node:test'
import assert from 'node:assert/strict'
import { IDBFactory } from 'fake-indexeddb'
import { applyAction, baseRecord, emptyStore, statusOf, canReview, exportBackup, parseBackup, mergeBackup, migrateLegacy, reviewIntervals } from '../../docs/.vitepress/data/progress.ts'
import { ProgressRepository, StaleProgressError } from '../../docs/.vitepress/data/progressRepository.ts'
const id = 'core-array-search'
const secondId = 'core-linear-structures'
const now = 1_700_000_000_000
const expected = { generation: 0, deletedAt: 0 }

test('mastery, fresh review qualification and 7/30/90 day schedule', () => {
  assert.deepEqual(reviewIntervals, [7, 30, 90].map(days => days * 86_400_000))
  let store = emptyStore()
  store = applyAction(store, { type: 'start', id }, now)
  store = applyAction(store, { type: 'verify', id }, now)
  assert.equal(statusOf(store.units[id], now), 'learning')
  for (const evidenceId of ['invariant','test','transfer'] as const) store = applyAction(store,{type:'evidence',id,evidenceId,checked:true},now)
  store = applyAction(store,{type:'answer',id,correct:true},now)
  store = applyAction(store,{type:'verify',id},now)
  assert.equal(statusOf(store.units[id],now),'verified')
  let clock = now
  for (const interval of reviewIntervals) {
    clock += interval
    assert.equal(statusOf(store.units[id],clock),'review')
    assert.equal(canReview(store.units[id],clock),false)
    store = applyAction(store,{type:'clear-answer',id},clock)
    assert.equal(statusOf(store.units[id],clock),'review')
    store = applyAction(store,{type:'answer',id,correct:false},clock)
    assert.equal(canReview(store.units[id],clock),false)
    store = applyAction(store,{type:'answer',id,correct:true},clock)
    assert.equal(canReview(store.units[id],clock),true)
    store = applyAction(store,{type:'review',id},clock)
    assert.equal(statusOf(store.units[id],clock),'verified')
  }
})
test('two connections preserve independent and same-unit transactional edits', async () => {
  const factory = new IDBFactory()
  const a = await ProgressRepository.open(factory), b = await ProgressRepository.open(factory)
  try {
    await a.initialize(emptyStore())
    await Promise.all([a.dispatch({type:'start',id},expected,now),b.dispatch({type:'start',id:secondId},expected,now)])
    assert.equal(Object.keys((await a.read()).units).length,2)
    await Promise.all([a.dispatch({type:'evidence',id,evidenceId:'invariant',checked:true},expected,now),b.dispatch({type:'evidence',id,evidenceId:'test',checked:true},expected,now)])
    assert.deepEqual(new Set((await a.read()).units[id].evidenceIds),new Set(['invariant','test']))
    await b.dispatch({type:'reset',id},expected,now+1)
    await assert.rejects(a.dispatch({type:'start',id},expected,now+2),StaleProgressError)
    await b.dispatch({type:'reset-all'},expected,now+3)
    await assert.rejects(a.dispatch({type:'start',id:secondId},expected,now+4),StaleProgressError)
    assert.deepEqual((await a.read()).units,{})
    await a.initialize(migrateLegacy({getItem:()=>JSON.stringify([id])},now))
    assert.deepEqual((await a.read()).units,{})
  } finally { a.close();b.close() }
})
test('backup uses newer timestamps, local ties and tombstones without mutating input', () => {
  const store = applyAction(emptyStore(),{type:'start',id},now)
  const backup = exportBackup(store)
  assert.equal(mergeBackup(store,parseBackup(backup)).counts.ignored,1)
  const removed = applyAction(store,{type:'reset',id},now+1)
  assert.equal(mergeBackup(removed,parseBackup(backup)).counts.ignored,1)
  const newer = {schemaVersion:3,records:[{id,record:baseRecord(now+2)}]}
  assert.equal(mergeBackup(removed,parseBackup(newer)).counts.added,1)
  assert.equal(removed.units[id],undefined)
  assert.throws(()=>parseBackup({schemaVersion:3,records:[...backup.records,...backup.records]}),/重复/)
  assert.throws(()=>parseBackup({schemaVersion:3,records:[{id,record:{...baseRecord(now),updatedAt:Infinity}}]}),/时间/)
  assert.throws(()=>parseBackup({schemaVersion:99,records:[]}),/版本/)
  assert.equal(parseBackup({schemaVersion:3,records:[{id:'future-unit',deletedAt:now}]}).unknown,1)
})
test('legacy v1/v2 preserve evidence and old mastery requires revalidation', () => {
  const old = migrateLegacy({getItem:key=>key.endsWith('v1')?JSON.stringify([id]):null},now)
  assert.equal(old.units[id].legacyCompleted,true)
  assert.equal(statusOf(old.units[id],now),'learning')
  const v2 = {schemaVersion:2,units:{[id]:baseRecord(now)}}
  assert.deepEqual(migrateLegacy({getItem:key=>key.endsWith('v2')?JSON.stringify(v2):null},now).units,v2.units)
})
test('import preview revision protects against changes from another tab', async () => {
  const repo = await ProgressRepository.open(new IDBFactory())
  try {
    await repo.initialize(emptyStore())
    await repo.dispatch({type:'start',id},expected,now)
    await assert.rejects(repo.import({records:[],unknown:0},0),StaleProgressError)
    assert.ok((await repo.read()).units[id])
  } finally { repo.close() }
})
