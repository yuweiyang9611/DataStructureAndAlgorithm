import type { LearningUnit } from '../data/learningUnits.ts'

/**
 * The one hue each track takes on this site (the family's One Hue Group Rule). A unit's card head,
 * its pressed filter chip and the band at the top of its guide page all fill with the hue of the
 * track the unit's stage belongs to. Red is kept for wrong and failed, so the four tracks take the
 * other four hues, and anything outside a track takes ink.
 *
 *   主线 (S02–S06)      cyan    the course that is run and traced
 *   进阶 (A01, A02)     blue    the next place to go
 *   题库 (P03, PRACTICE) purple  problems and the code that solves them
 *   综合项目 (S07)       green   the four projects that close the course
 */
export type Hue = 'green' | 'cyan' | 'blue' | 'purple' | 'ink'
export type Track = 'main' | 'advanced' | 'practice' | 'project'

export const trackHues: Record<Track, Hue> = { main: 'cyan', advanced: 'blue', practice: 'purple', project: 'green' }
export const trackWords: Record<Track, string> = { main: '主线', advanced: '进阶', practice: '题库', project: '项目' }
export const trackOrder: Track[] = ['main', 'advanced', 'practice', 'project']

const stageTracks: Record<string, Track> = {
  S02: 'main', S03: 'main', S04: 'main', S05: 'main', S06: 'main',
  A01: 'advanced', A02: 'advanced',
  P03: 'practice', PRACTICE: 'practice',
  S07: 'project'
}

/** The short stage word for a card eyebrow, beside the level: 阶段三 · 入门. */
export const stageWords: Record<string, string> = {
  S02: '阶段二', S03: '阶段三', S04: '阶段四', S05: '阶段五', S06: '阶段六', S07: '阶段七',
  A01: '进阶', A02: '进阶', P03: 'P3', PRACTICE: '题库'
}

export function trackOf(unit: Pick<LearningUnit, 'stage'>): Track | undefined {
  return stageTracks[unit.stage]
}

export function hueOf(unit: Pick<LearningUnit, 'stage'>): Hue {
  const track = trackOf(unit)
  return track ? trackHues[track] : 'ink'
}
