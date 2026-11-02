import { learningUnits, type LearningUnit } from './learningUnits.ts'
import { masteryEvidenceIds, masteryRevision } from './masteryContent.ts'
import { statusOf, type ProgressStore, type UnitProgress, type UnitStatus } from './progress.ts'

const stageOrder = new Map(
  ['S02', 'S03', 'S04', 'A01', 'S05', 'S06', 'A02', 'P03', 'PRACTICE', 'S07']
    .map((key, index) => [key, index])
)
const originalOrder = new Map(learningUnits.map((unit, index) => [unit.id, index]))
const unitById = new Map(learningUnits.map((unit) => [unit.id, unit]))

function compareUnits(left: LearningUnit, right: LearningUnit) {
  const stageDifference = (stageOrder.get(left.stage) ?? 99) - (stageOrder.get(right.stage) ?? 99)
  return stageDifference || (originalOrder.get(left.id) ?? 0) - (originalOrder.get(right.id) ?? 0)
}

/** Stage order, corrected so that no unit comes before one of its prerequisites. */
export function topologicalOrder(units: LearningUnit[]) {
  const indegree = new Map(units.map((unit) => [unit.id, unit.prerequisites.length]))
  const dependents = new Map(units.map((unit) => [unit.id, [] as string[]]))
  for (const unit of units) {
    for (const prerequisite of unit.prerequisites) dependents.get(prerequisite)?.push(unit.id)
  }

  const ready = units.filter((unit) => indegree.get(unit.id) === 0).sort(compareUnits)
  const ordered: LearningUnit[] = []
  while (ready.length) {
    const unit = ready.shift()!
    ordered.push(unit)
    for (const dependentId of dependents.get(unit.id) ?? []) {
      const nextDegree = (indegree.get(dependentId) ?? 1) - 1
      indegree.set(dependentId, nextDegree)
      if (nextDegree === 0) {
        const dependent = unitById.get(dependentId)
        if (dependent) {
          ready.push(dependent)
          ready.sort(compareUnits)
        }
      }
    }
  }
  return ordered.length === units.length ? ordered : [...units].sort(compareUnits)
}

export const orderedUnits = topologicalOrder(learningUnits)

/** A unit is mastered once verified, including when its review has come due. */
export function isMastered(status: UnitStatus) {
  return status === 'verified' || status === 'review'
}

/**
 * The record line reaches as far as the unit has gone: the self-check, the three pieces of
 * evidence, then verification. Only a verified unit is solid to the end.
 */
export const unitSteps = 2 + masteryEvidenceIds.length

export function stepsDone(record: UnitProgress | undefined, now: number) {
  if (!record) return 0
  if (isMastered(statusOf(record, now))) return unitSteps
  const current = record.evidenceRevision === masteryRevision
  const evidence = current ? record.evidenceIds.filter((id) => masteryEvidenceIds.includes(id)).length : 0
  const selfCheck = record.selfTestPassedAt && record.selfTestRevision === masteryRevision ? 1 : 0
  return evidence + selfCheck
}

export type StopKind = 'learning' | 'review' | 'next' | 'complete' | 'empty'

export interface CourseRecord {
  /** False until the owner has touched any unit, so the page shows no tick and no solid line. */
  hasRecord: boolean
  total: number
  verified: number
  learning: number
  due: number
  /** Where the continue link points; on 'complete' it is undefined. */
  unit?: LearningUnit
  kind: StopKind
  /** The most recent change in the record, for "上次记录". */
  lastUpdatedAt?: number
}

export function readCourse(store: ProgressStore, now: number): CourseRecord {
  const status = (unit: LearningUnit) => statusOf(store.units[unit.id], now)
  const hasRecord = Object.keys(store.units).length > 0
  const verified = orderedUnits.filter((unit) => isMastered(status(unit))).length
  const inProgress = orderedUnits.filter((unit) => status(unit) === 'learning')
  const dueUnits = orderedUnits.filter((unit) => status(unit) === 'review')
  const lastUpdatedAt = hasRecord
    ? Math.max(...Object.values(store.units).map((record) => record.updatedAt))
    : undefined
  const base = { hasRecord, total: orderedUnits.length, verified, learning: inProgress.length, due: dueUnits.length, lastUpdatedAt }

  const firstAvailable = orderedUnits.find((unit) =>
    status(unit) === 'not-started' && unit.prerequisites.every((id) => isMastered(statusOf(store.units[id], now))))

  if (!hasRecord) return { ...base, kind: 'empty', unit: firstAvailable ?? orderedUnits[0] }

  // The place the owner stopped is the unit they touched last and have not finished.
  const lastLearning = [...inProgress]
    .sort((left, right) => (store.units[right.id]?.updatedAt ?? 0) - (store.units[left.id]?.updatedAt ?? 0))[0]
  if (lastLearning) return { ...base, kind: 'learning', unit: lastLearning }

  const firstDue = [...dueUnits]
    .sort((left, right) => (store.units[left.id]?.nextReviewAt ?? 0) - (store.units[right.id]?.nextReviewAt ?? 0))[0]
  if (firstDue) return { ...base, kind: 'review', unit: firstDue }

  if (firstAvailable) return { ...base, kind: 'next', unit: firstAvailable }
  return { ...base, kind: 'complete' }
}

/** "今天", "昨天" or "N 天前", counted in local calendar days. */
export function daysAgo(timestamp: number, now: number) {
  const day = (value: number) => {
    const date = new Date(value)
    return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) / 86_400_000
  }
  const days = Math.max(0, Math.round(day(now) - day(timestamp)))
  return days === 0 ? '今天' : days === 1 ? '昨天' : `${days} 天前`
}
