import { masteryRevision } from './masteryContent.ts'
import type { UnitProgress } from './progress.ts'

/**
 * The loop band of a unit (the kit's `.nb-steps`): five steps in the order the reader takes them,
 * read from the unit's record and never from a tick on the band itself.
 *
 *   read  读      the unit was started (`startedAt`)
 *   code  看代码  the unit's panel showed a file or a tab was clicked (`sourceOpenedAt`)
 *   run   跑      the 试一试 demo finished in the browser without an error (`ranAt`)
 *   quiz  自测    the self-check was answered correctly at the current revision (`selfTestPassedAt`)
 *   note  记一笔  a note was saved on the unit (`note`)
 *
 * A step is `done` when the record says so, exactly one step is `now` (the first one not done, or
 * none when all are done), the rest after it are `pending`. Every unit has all five steps, so no
 * step is `none` on this site. Without script, or before the record is read, every step is pending.
 */
export type StepId = 'read' | 'code' | 'run' | 'quiz' | 'note'
export type StepState = 'done' | 'now' | 'pending' | 'none'

export interface BandStep {
  id: StepId
  label: string
  state: StepState
}

export const stepOrder: readonly StepId[] = ['read', 'code', 'run', 'quiz', 'note']
export const stepLabels: Record<StepId, string> = { read: '读', code: '看代码', run: '跑', quiz: '自测', note: '记一笔' }

/** The steps the record says the reader did. */
export function doneSteps(record: UnitProgress | undefined): Set<StepId> {
  const done = new Set<StepId>()
  if (!record) return done
  if (record.startedAt) done.add('read')
  if (record.sourceOpenedAt) done.add('code')
  if (record.ranAt) done.add('run')
  if (record.selfTestPassedAt && record.selfTestRevision === masteryRevision) done.add('quiz')
  if (record.note?.trim()) done.add('note')
  return done
}

/** The band of one unit from its record. */
export function unitBand(record: UnitProgress | undefined): BandStep[] {
  const done = doneSteps(record)
  let stopped = false
  return stepOrder.map((id) => {
    let state: StepState
    if (done.has(id)) state = 'done'
    else if (!stopped) { state = 'now'; stopped = true }
    else state = 'pending'
    return { id, label: stepLabels[id], state }
  })
}

/** The band before any record is read: the server render and a page without script. */
export function pendingBand(): BandStep[] {
  return stepOrder.map((id) => ({ id, label: stepLabels[id], state: 'pending' as const }))
}

/** The step marked 停在这里, or undefined when every step is done. */
export function nowStep(record: UnitProgress | undefined): BandStep | undefined {
  return unitBand(record).find((step) => step.state === 'now')
}

/** How many of the five steps are done, for a count beside the band. */
export function bandDone(record: UnitProgress | undefined): number {
  return doneSteps(record).size
}
