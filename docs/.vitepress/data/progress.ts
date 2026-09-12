import { masteryEvidenceIds, masteryRevision, type MasteryEvidenceId } from './masteryContent.ts'
import { learningUnits } from './learningUnits.ts'

export type UnitStatus = 'not-started' | 'learning' | 'verified' | 'review'
export interface UnitProgress {
  evidenceIds: MasteryEvidenceId[]
  evidenceRevision?: number
  selfTestPassedAt?: number
  selfTestRevision?: number
  verifiedAt?: number
  nextReviewAt?: number
  lastReviewedAt?: number
  reviewRound: number
  verifiedRevision?: number
  legacyCompleted?: true
  startedAt: number
  updatedAt: number
}
export interface ProgressStore {
  schemaVersion: 3
  generation: number
  revision: number
  units: Record<string, UnitProgress>
  deleted: Record<string, number>
}
export const reviewIntervals = [7, 30, 90].map(days => days * 86_400_000)
export const validIds = new Set(learningUnits.map(unit => unit.id))
export const emptyStore = (): ProgressStore => ({ schemaVersion: 3, generation: 0, revision: 0, units: {}, deleted: {} })
export const baseRecord = (now: number): UnitProgress => ({ evidenceIds: [], reviewRound: 0, startedAt: now, updatedAt: now })
export function evidenceComplete(record?: UnitProgress) {
  return record?.evidenceRevision === masteryRevision && masteryEvidenceIds.every(id => record.evidenceIds.includes(id))
}
export function statusOf(record: UnitProgress | undefined, now: number): UnitStatus {
  if (!record) return 'not-started'
  if (!record.verifiedAt || record.verifiedRevision !== masteryRevision || !evidenceComplete(record)) return 'learning'
  return record.nextReviewAt && record.nextReviewAt <= now ? 'review' : 'verified'
}
export function canVerify(store: ProgressStore, id: string, now: number) {
  const record = store.units[id]
  return Boolean(record && evidenceComplete(record) && record.selfTestPassedAt && record.selfTestRevision === masteryRevision &&
    learningUnits.find(unit => unit.id === id)?.prerequisites.every(key => ['verified', 'review'].includes(statusOf(store.units[key], now))))
}
export function canReview(record: UnitProgress | undefined, now: number) {
  return Boolean(record && statusOf(record, now) === 'review' && record.selfTestRevision === masteryRevision &&
    record.selfTestPassedAt && record.nextReviewAt && record.selfTestPassedAt >= record.nextReviewAt &&
    record.selfTestPassedAt > Math.max(record.lastReviewedAt ?? 0, record.verifiedAt ?? 0))
}
export type ProgressAction =
  | { type: 'start' | 'clear-answer' | 'verify' | 'review' | 'reset'; id: string }
  | { type: 'answer'; id: string; correct: boolean }
  | { type: 'evidence'; id: string; evidenceId: MasteryEvidenceId; checked: boolean }
  | { type: 'reset-all' }

/** Apply an intent to the newest transactional state, never a stale UI snapshot. */
export function applyAction(store: ProgressStore, action: ProgressAction, now: number): ProgressStore {
  const next = structuredClone(store)
  next.revision++
  if (action.type === 'reset-all') {
    for (const id of validIds) next.deleted[id] = Math.max(now, (next.units[id]?.updatedAt ?? 0) + 1, (next.deleted[id] ?? 0) + 1)
    next.units = {}
    next.generation++
    return next
  }
  if (!validIds.has(action.id)) throw new Error('未知学习单元。')
  const id = action.id
  const timestamp = Math.max(now, (store.units[id]?.updatedAt ?? 0) + 1, (store.deleted[id] ?? 0) + 1)
  if (action.type === 'reset') {
    delete next.units[id]
    next.deleted[id] = timestamp
    return next
  }
  const record = next.units[id] ?? baseRecord(timestamp)
  switch (action.type) {
    case 'start': if (next.units[id]) return store; break
    case 'evidence': {
      const evidence = new Set(record.evidenceRevision === masteryRevision ? record.evidenceIds : [])
      if (action.checked) evidence.add(action.evidenceId)
      else evidence.delete(action.evidenceId)
      record.evidenceIds = [...evidence]
      record.evidenceRevision = masteryRevision
      break
    }
    case 'answer':
      if (action.correct) { record.selfTestPassedAt = now; record.selfTestRevision = masteryRevision; break }
      // An unsuccessful attempt invalidates this attempt, not historical mastery.
      delete record.selfTestPassedAt; delete record.selfTestRevision
      break
    case 'clear-answer': delete record.selfTestPassedAt; delete record.selfTestRevision; break
    case 'verify':
      if (!canVerify(store, id, now)) return store
      record.verifiedAt = now; record.verifiedRevision = masteryRevision
      record.nextReviewAt = now + reviewIntervals[0]; record.reviewRound = 0
      delete record.legacyCompleted
      break
    case 'review':
      if (!canReview(record, now)) return store
      record.reviewRound = Math.min(reviewIntervals.length - 1, record.reviewRound + 1)
      record.lastReviewedAt = now; record.nextReviewAt = now + reviewIntervals[record.reviewRound]
      break
  }
  record.updatedAt = timestamp
  next.units[id] = record
  return next
}

function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('备份格式无效。')
  return value as Record<string, unknown>
}
function timestamp(value: unknown): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || value <= 0 || value > 8_640_000_000_000_000) throw new Error('备份包含非法时间。')
  return value
}
export function validateRecord(value: unknown): UnitProgress {
  const raw = object(value)
  if (!Array.isArray(raw.evidenceIds) || raw.evidenceIds.some(id => !masteryEvidenceIds.includes(id)) ||
      new Set(raw.evidenceIds).size !== raw.evidenceIds.length || !Number.isInteger(raw.reviewRound) || Number(raw.reviewRound) < 0 || Number(raw.reviewRound) > 2) throw new Error('备份包含非法掌握记录。')
  const record = baseRecord(timestamp(raw.startedAt))
  record.updatedAt = timestamp(raw.updatedAt)
  record.evidenceIds = [...raw.evidenceIds]; record.reviewRound = Number(raw.reviewRound)
  for (const key of ['selfTestPassedAt', 'verifiedAt', 'nextReviewAt', 'lastReviewedAt'] as const) {
    if (raw[key] !== undefined) record[key] = timestamp(raw[key])
  }
  for (const key of ['selfTestRevision', 'evidenceRevision', 'verifiedRevision'] as const) {
    if (raw[key] !== undefined) {
      if (!Number.isSafeInteger(raw[key]) || Number(raw[key]) <= 0) throw new Error('备份包含非法内容版本。')
      record[key] = Number(raw[key])
    }
  }
  if (raw.legacyCompleted === true) record.legacyCompleted = true
  return record
}
export interface ImportData { records: { id: string; record?: UnitProgress; deletedAt?: number }[]; unknown: number }
export function parseBackup(value: unknown): ImportData {
  const raw = object(value)
  let entries: unknown[]
  if (raw.schemaVersion === 2) entries = Object.entries(object(raw.units)).map(([id, record]) => ({ id, record }))
  else if (raw.schemaVersion === 3 && Array.isArray(raw.records)) entries = raw.records
  else throw new Error('不支持的备份版本。')
  const result: ImportData = { records: [], unknown: 0 }
  const seen = new Set<string>()
  for (const entry of entries) {
    const item = object(entry)
    if (typeof item.id !== 'string' || seen.has(item.id)) throw new Error('备份包含非法或重复单元 ID。')
    seen.add(item.id)
    if ((item.record !== undefined) === (item.deletedAt !== undefined)) throw new Error('备份记录必须为进度或删除标记。')
    const parsed = item.record !== undefined
      ? { id: item.id, record: validateRecord(item.record) }
      : { id: item.id, deletedAt: timestamp(item.deletedAt) }
    if (!validIds.has(item.id)) { result.unknown++; continue }
    result.records.push(parsed)
  }
  return result
}
export function exportBackup(store: ProgressStore) {
  return { schemaVersion: 3, records: [...validIds].flatMap(id => {
    const record = store.units[id]
    if (record) return [{ id, record }] as ImportData['records']
    return store.deleted[id] ? [{ id, deletedAt: store.deleted[id] }] : []
  }) }
}
export function mergeBackup(store: ProgressStore, data: ImportData) {
  const next = structuredClone(store)
  const counts = { added: 0, updated: 0, deleted: 0, ignored: data.unknown }
  for (const { id, record, deletedAt } of data.records) {
    const incoming = record?.updatedAt ?? deletedAt!
    const existing = Math.max(next.units[id]?.updatedAt ?? 0, next.deleted[id] ?? 0)
    if (incoming <= existing) { counts.ignored++; continue }
    if (record) { next.units[id] ? counts.updated++ : counts.added++; next.units[id] = structuredClone(record) }
    else { counts.deleted++; delete next.units[id]; next.deleted[id] = incoming }
  }
  next.revision++
  return { store: next, counts }
}
export function migrateLegacy(storage: Pick<Storage, 'getItem'>, now: number): ProgressStore {
  const store = emptyStore()
  try {
    const v2 = storage.getItem('dsa-learning-progress-v2')
    if (v2 !== null) return mergeBackup(store, parseBackup(JSON.parse(v2))).store
    const ids: unknown = JSON.parse(storage.getItem('dsa-learning-progress-v1') ?? '[]')
    if (Array.isArray(ids)) for (const id of ids) {
      if (typeof id === 'string' && validIds.has(id)) store.units[id] = { ...baseRecord(now), legacyCompleted: true }
    }
  } catch { /* Preserve malformed legacy bytes; start a usable session. */ }
  return store
}
