<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { withBase } from 'vitepress'
import {
  masteryByUnitId,
  masteryEvidenceIds,
  masteryRevision,
  type MasteryEvidenceId
} from '../../data/masteryContent'
import { learningUnits, repository, stageLabels, type LearningUnit } from '../../data/learningUnits'

type UnitStatus = 'not-started' | 'learning' | 'verified' | 'review'
type QuizFeedback = 'correct' | 'incorrect' | 'missing'

interface UnitProgress {
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

interface ProgressStore {
  schemaVersion: 2
  migratedFromV1: boolean
  units: Record<string, UnitProgress>
}

const legacyStorageKey = 'dsa-learning-progress-v1'
const storageKey = 'dsa-learning-progress-v2'
const reviewIntervals = [7, 30, 90].map((days) => days * 24 * 60 * 60 * 1000)
const stageOrder = new Map(
  ['S02', 'S03', 'S04', 'A01', 'S05', 'S06', 'A02', 'P03', 'PRACTICE', 'S07']
    .map((key, index) => [key, index])
)
const originalOrder = new Map(learningUnits.map((unit, index) => [unit.id, index]))
const unitById = new Map(learningUnits.map((unit) => [unit.id, unit]))
const validIds = new Set(unitById.keys())

function compareUnits(left: LearningUnit, right: LearningUnit) {
  const stageDifference = (stageOrder.get(left.stage) ?? 99) - (stageOrder.get(right.stage) ?? 99)
  return stageDifference || (originalOrder.get(left.id) ?? 0) - (originalOrder.get(right.id) ?? 0)
}

function topologicalOrder(units: LearningUnit[]) {
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

const orderedUnits = topologicalOrder(learningUnits)
const query = ref('')
const stage = ref('all')
const difficulty = ref('all')
const statusFilter = ref('all')
const progressStore = ref<ProgressStore>({ schemaVersion: 2, migratedFromV1: false, units: {} })
const hydrated = ref(false)
const now = ref(Date.now())
const copiedId = ref('')
const copyFailedId = ref('')
const quizAnswers = ref<Record<string, number | undefined>>({})
const quizFeedback = ref<Record<string, QuizFeedback | undefined>>({})
const migrationNotice = ref(false)
const storageError = ref(false)
const resetArmed = ref(false)
let clockTimer: number | undefined
let resetTimer: number | undefined
let copyTimer: number | undefined

function isTimestamp(value: unknown): value is number {
  return typeof value === 'number' &&
    Number.isFinite(value) &&
    value > 0 &&
    value <= 8_640_000_000_000_000 &&
    Number.isFinite(new Date(value).getTime())
}

function normalizeRecord(value: unknown): UnitProgress | null {
  if (!value || typeof value !== 'object') return null
  const candidate = value as Partial<UnitProgress>
  const evidenceIds = Array.isArray(candidate.evidenceIds)
    ? [...new Set(candidate.evidenceIds.filter(
      (id): id is MasteryEvidenceId =>
        typeof id === 'string' && (masteryEvidenceIds as readonly string[]).includes(id)
    ))]
    : []
  const timestamp = Date.now()
  const record: UnitProgress = {
    evidenceIds,
    reviewRound: Number.isInteger(candidate.reviewRound)
      ? Math.min(reviewIntervals.length - 1, Math.max(0, Number(candidate.reviewRound)))
      : 0,
    startedAt: isTimestamp(candidate.startedAt) ? candidate.startedAt : timestamp,
    updatedAt: isTimestamp(candidate.updatedAt) ? candidate.updatedAt : timestamp
  }
  if (isTimestamp(candidate.selfTestPassedAt)) record.selfTestPassedAt = candidate.selfTestPassedAt
  if (Number.isInteger(candidate.selfTestRevision) && Number(candidate.selfTestRevision) > 0) {
    record.selfTestRevision = Number(candidate.selfTestRevision)
  }
  if (Number.isInteger(candidate.evidenceRevision) && Number(candidate.evidenceRevision) > 0) {
    record.evidenceRevision = Number(candidate.evidenceRevision)
  }
  if (isTimestamp(candidate.verifiedAt)) record.verifiedAt = candidate.verifiedAt
  if (isTimestamp(candidate.nextReviewAt)) record.nextReviewAt = candidate.nextReviewAt
  if (isTimestamp(candidate.lastReviewedAt)) record.lastReviewedAt = candidate.lastReviewedAt
  if (Number.isInteger(candidate.verifiedRevision) && Number(candidate.verifiedRevision) > 0) {
    record.verifiedRevision = Number(candidate.verifiedRevision)
  }
  if (candidate.legacyCompleted === true) record.legacyCompleted = true
  return record
}

function normalizeStore(value: unknown): ProgressStore | null {
  if (!value || typeof value !== 'object') return null
  const candidate = value as Partial<ProgressStore>
  if (candidate.schemaVersion !== 2 || !candidate.units || typeof candidate.units !== 'object') return null
  const units: Record<string, UnitProgress> = {}
  for (const [id, value] of Object.entries(candidate.units)) {
    if (!validIds.has(id)) continue
    const record = normalizeRecord(value)
    if (record) units[id] = record
  }
  return {
    schemaVersion: 2,
    migratedFromV1: candidate.migratedFromV1 === true,
    units
  }
}

function migrateLegacy(): ProgressStore {
  const units: Record<string, UnitProgress> = {}
  let migrated = false
  try {
    const raw = localStorage.getItem(legacyStorageKey)
    if (raw !== null) {
      migrated = true
      const ids = JSON.parse(raw)
      if (Array.isArray(ids)) {
        const timestamp = Date.now()
        for (const id of new Set(ids.filter((value): value is string => typeof value === 'string' && validIds.has(value)))) {
          units[id] = {
            evidenceIds: [],
            reviewRound: 0,
            legacyCompleted: true,
            startedAt: timestamp,
            updatedAt: timestamp
          }
        }
        migrationNotice.value = Object.keys(units).length > 0
      }
    }
  } catch {
    // 旧数据损坏时从空的 v2 状态开始；页面仍应可用。
  }
  return { schemaVersion: 2, migratedFromV1: migrated, units }
}

function persistProgress() {
  if (!hydrated.value) return
  try {
    localStorage.setItem(storageKey, JSON.stringify(progressStore.value))
    storageError.value = false
  } catch {
    storageError.value = true
  }
}

function baseRecord(existing?: UnitProgress): UnitProgress {
  const timestamp = Date.now()
  return existing
    ? { ...existing, evidenceIds: [...existing.evidenceIds] }
    : { evidenceIds: [], reviewRound: 0, startedAt: timestamp, updatedAt: timestamp }
}

function updateUnit(id: string, update: (record: UnitProgress) => UnitProgress) {
  const nextRecord = update(baseRecord(progressStore.value.units[id]))
  progressStore.value = {
    ...progressStore.value,
    units: { ...progressStore.value.units, [id]: nextRecord }
  }
}

function statusOf(id: string): UnitStatus {
  const record = progressStore.value.units[id]
  if (!record) return 'not-started'
  if (
    !record.verifiedAt ||
    record.verifiedRevision !== masteryRevision ||
    record.selfTestRevision !== masteryRevision ||
    !record.selfTestPassedAt ||
    !evidenceComplete(id)
  ) return 'learning'
  if (record.nextReviewAt && record.nextReviewAt <= now.value) return 'review'
  return 'verified'
}

function statusLabel(id: string) {
  return {
    'not-started': '未开始',
    learning: '学习中',
    verified: '已验证',
    review: '待复习'
  }[statusOf(id)]
}

function isMastered(id: string) {
  const value = statusOf(id)
  return value === 'verified' || value === 'review'
}

function prerequisitesSatisfied(unit: LearningUnit) {
  return unit.prerequisites.every(isMastered)
}

const verifiedCount = computed(() => orderedUnits.filter((unit) => isMastered(unit.id)).length)
const dueCount = computed(() => orderedUnits.filter((unit) => statusOf(unit.id) === 'review').length)
const progressPercent = computed(() => Math.round((verifiedCount.value / learningUnits.length) * 100))
const hasProgress = computed(() => Object.keys(progressStore.value.units).length > 0)

const filteredUnits = computed(() => {
  const needle = query.value.trim().toLocaleLowerCase('zh-CN')
  return orderedUnits.filter((unit) => {
    const haystack = `${unit.title} ${unit.category} ${unit.invariant} ${unit.complexity}`.toLocaleLowerCase('zh-CN')
    const matchesQuery = !needle || haystack.includes(needle)
    const matchesStage = stage.value === 'all' || unit.stage === stage.value
    const matchesDifficulty = difficulty.value === 'all' || unit.difficulty === difficulty.value
    const matchesStatus = statusFilter.value === 'all' || statusOf(unit.id) === statusFilter.value
    return matchesQuery && matchesStage && matchesDifficulty && matchesStatus
  })
})

const recommendations = computed(() => {
  const due = orderedUnits
    .filter((unit) => statusOf(unit.id) === 'review')
    .sort((left, right) =>
      (progressStore.value.units[left.id]?.nextReviewAt ?? 0) -
      (progressStore.value.units[right.id]?.nextReviewAt ?? 0)
    )
    .map((unit) => ({ unit, reason: '复习已到期' }))
  const learning = orderedUnits
    .filter((unit) => statusOf(unit.id) === 'learning' && prerequisitesSatisfied(unit))
    .map((unit) => ({ unit, reason: progressStore.value.units[unit.id]?.legacyCompleted ? '补充旧版验证证据' : '继续已开始的单元' }))
  const available = orderedUnits
    .filter((unit) => statusOf(unit.id) === 'not-started' && prerequisitesSatisfied(unit))
    .map((unit) => ({ unit, reason: unit.prerequisites.length ? '前置已经满足' : '可直接开始' }))
  return [...due, ...learning, ...available].slice(0, 3)
})

function mastery(unit: LearningUnit) {
  return masteryByUnitId[unit.id]
}

function prerequisiteTitle(id: string) {
  return unitById.get(id)?.title ?? id
}

function evidenceChecked(id: string, evidenceId: string) {
  const record = progressStore.value.units[id]
  return record?.evidenceRevision === masteryRevision &&
    record.evidenceIds.includes(evidenceId as MasteryEvidenceId)
}

function evidenceComplete(id: string) {
  return masteryEvidenceIds.every((evidenceId) => evidenceChecked(id, evidenceId))
}

function startUnit(id: string) {
  if (progressStore.value.units[id]) return
  updateUnit(id, (record) => record)
}

function handleMasteryToggle(id: string, event: Event) {
  if ((event.target as HTMLDetailsElement).open) startUnit(id)
}

function toggleEvidence(id: string, evidenceId: string, event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  if (!(masteryEvidenceIds as readonly string[]).includes(evidenceId)) return
  updateUnit(id, (record) => {
    const evidence = new Set(record.evidenceRevision === masteryRevision ? record.evidenceIds : [])
    if (checked) evidence.add(evidenceId as MasteryEvidenceId)
    else evidence.delete(evidenceId as MasteryEvidenceId)
    return {
      ...record,
      evidenceIds: [...evidence],
      evidenceRevision: masteryRevision,
      updatedAt: Date.now()
    }
  })
}

function clearSelfTestQualification(id: string) {
  updateUnit(id, (record) => {
    const { selfTestPassedAt: _passedAt, selfTestRevision: _revision, ...rest } = record
    return { ...rest, updatedAt: Date.now() }
  })
}

function handleQuizChoice(id: string) {
  quizFeedback.value = { ...quizFeedback.value, [id]: undefined }
  const status = statusOf(id)
  if (status === 'learning' || status === 'review') clearSelfTestQualification(id)
}

function submitSelfCheck(unit: LearningUnit) {
  const selected = quizAnswers.value[unit.id]
  if (selected === undefined) {
    quizFeedback.value = { ...quizFeedback.value, [unit.id]: 'missing' }
    return
  }
  startUnit(unit.id)
  if (selected === mastery(unit).selfCheck.correctIndex) {
    const timestamp = Date.now()
    updateUnit(unit.id, (record) => ({
      ...record,
      selfTestPassedAt: timestamp,
      selfTestRevision: masteryRevision,
      updatedAt: timestamp
    }))
    quizFeedback.value = { ...quizFeedback.value, [unit.id]: 'correct' }
  } else {
    if (statusOf(unit.id) === 'learning' || statusOf(unit.id) === 'review') {
      clearSelfTestQualification(unit.id)
    }
    quizFeedback.value = { ...quizFeedback.value, [unit.id]: 'incorrect' }
  }
}

function canVerify(id: string) {
  const record = progressStore.value.units[id]
  const unit = unitById.get(id)
  return Boolean(
    record &&
    unit &&
    prerequisitesSatisfied(unit) &&
    evidenceComplete(id) &&
    record.selfTestPassedAt &&
    record.selfTestRevision === masteryRevision
  )
}

function verifyUnit(id: string) {
  if (!canVerify(id)) return
  const timestamp = Date.now()
  updateUnit(id, (record) => ({
    ...record,
    verifiedAt: timestamp,
    nextReviewAt: timestamp + reviewIntervals[0],
    reviewRound: 0,
    verifiedRevision: masteryRevision,
    legacyCompleted: undefined,
    updatedAt: timestamp
  }))
  now.value = timestamp
}

function canReview(id: string) {
  const record = progressStore.value.units[id]
  if (
    !record ||
    statusOf(id) !== 'review' ||
    !record.selfTestPassedAt ||
    record.selfTestRevision !== masteryRevision ||
    !record.nextReviewAt ||
    !evidenceComplete(id)
  ) return false
  return record.selfTestPassedAt >= record.nextReviewAt &&
    record.selfTestPassedAt > Math.max(record.lastReviewedAt ?? 0, record.verifiedAt ?? 0)
}

function completeReview(id: string) {
  if (!canReview(id)) return
  const timestamp = Date.now()
  updateUnit(id, (record) => {
    const nextRound = Math.min(reviewIntervals.length - 1, record.reviewRound + 1)
    return {
      ...record,
      reviewRound: nextRound,
      lastReviewedAt: timestamp,
      nextReviewAt: timestamp + reviewIntervals[nextRound],
      updatedAt: timestamp
    }
  })
  now.value = timestamp
}

function resetUnit(id: string) {
  const units = { ...progressStore.value.units }
  delete units[id]
  progressStore.value = { ...progressStore.value, units }
  const answers = { ...quizAnswers.value }
  const feedback = { ...quizFeedback.value }
  delete answers[id]
  delete feedback[id]
  quizAnswers.value = answers
  quizFeedback.value = feedback
}

function requestReset() {
  if (!resetArmed.value) {
    resetArmed.value = true
    if (resetTimer !== undefined) window.clearTimeout(resetTimer)
    resetTimer = window.setTimeout(() => { resetArmed.value = false }, 5000)
    return
  }
  progressStore.value = { schemaVersion: 2, migratedFromV1: true, units: {} }
  quizAnswers.value = {}
  quizFeedback.value = {}
  migrationNotice.value = false
  resetArmed.value = false
}

function cancelReset() {
  resetArmed.value = false
  if (resetTimer !== undefined) window.clearTimeout(resetTimer)
  resetTimer = undefined
}

function formatDate(timestamp?: number) {
  if (!timestamp) return ''
  return new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: 'short', day: 'numeric' }).format(timestamp)
}

async function focusUnit(id: string) {
  query.value = ''
  stage.value = 'all'
  difficulty.value = 'all'
  statusFilter.value = 'all'
  await nextTick()
  const item = document.getElementById(`unit-${id}`)
  item?.scrollIntoView({ block: 'start' })
  document.getElementById(`unit-title-${id}`)?.focus({ preventScroll: true })
}

function sourceUrl(path: string) {
  const encoded = path.split('/').map(encodeURIComponent).join('/')
  return `${repository}/blob/main/${encoded}`
}

async function copyCommand(unit: LearningUnit) {
  copiedId.value = ''
  copyFailedId.value = ''
  try {
    await navigator.clipboard.writeText(unit.testCommand)
    copiedId.value = unit.id
  } catch {
    copyFailedId.value = unit.id
  }
  if (copyTimer !== undefined) window.clearTimeout(copyTimer)
  copyTimer = window.setTimeout(() => {
    if (copiedId.value === unit.id) copiedId.value = ''
    if (copyFailedId.value === unit.id) copyFailedId.value = ''
  }, 1600)
}

onMounted(() => {
  try {
    const stored = localStorage.getItem(storageKey)
    const normalized = stored === null ? null : normalizeStore(JSON.parse(stored))
    progressStore.value = normalized ?? migrateLegacy()
  } catch {
    progressStore.value = migrateLegacy()
  } finally {
    hydrated.value = true
    persistProgress()
  }
  clockTimer = window.setInterval(() => { now.value = Date.now() }, 60_000)
})

watch(progressStore, persistProgress, { deep: true })

onBeforeUnmount(() => {
  if (clockTimer !== undefined) window.clearInterval(clockTimer)
  if (resetTimer !== undefined) window.clearTimeout(resetTimer)
  if (copyTimer !== undefined) window.clearTimeout(copyTimer)
})
</script>

<template>
  <section class="unit-explorer" aria-labelledby="unit-explorer-heading">
    <div class="unit-progress">
      <div>
        <p class="unit-eyebrow">VERIFIED MASTERY · LOCAL ONLY</p>
        <h2 id="unit-explorer-heading">22 个可验证学习单元</h2>
        <p>进度只保存在当前浏览器。自测答对并完成不变量、测试和迁移三类证据后，单元才会标记为“已验证”。</p>
      </div>
      <div class="unit-progress-meter" aria-live="polite">
        <strong>{{ verifiedCount }} / {{ learningUnits.length }}</strong>
        <span>已验证过 · {{ progressPercent }}%<template v-if="dueCount"> · {{ dueCount }} 个待复习</template></span>
        <progress :value="verifiedCount" :max="learningUnits.length" aria-label="已验证学习单元">
          {{ progressPercent }}%
        </progress>
        <div v-if="hasProgress" class="unit-reset-actions">
          <button type="button" class="unit-reset" @click="requestReset">{{ resetArmed ? '确认清空全部进度' : '清空本地进度' }}</button>
          <button v-if="resetArmed" type="button" class="unit-reset unit-reset-cancel" @click="cancelReset">取消</button>
        </div>
      </div>
    </div>

    <p v-if="migrationNotice" class="unit-notice" role="status">
      旧版完成记录已迁移为“学习中”。请补答自测并完成证据清单，不会丢失原有单元位置。
    </p>
    <p v-if="storageError" class="unit-notice unit-notice--warning" role="status">
      浏览器当前无法保存进度；本次页面内操作仍然有效。
    </p>

    <section class="unit-recommendations" aria-labelledby="unit-recommendations-heading">
      <div>
        <p class="unit-eyebrow">NEXT BEST STEP</p>
        <h3 id="unit-recommendations-heading">根据依赖与复习时间推荐下一步</h3>
      </div>
      <ol v-if="recommendations.length">
        <li v-for="recommendation in recommendations" :key="recommendation.unit.id">
          <button type="button" @click="focusUnit(recommendation.unit.id)">
            <strong>{{ recommendation.unit.title }}</strong>
            <span>{{ recommendation.reason }}</span>
          </button>
        </li>
      </ol>
      <p v-else>所有学习单元都已验证，且当前没有到期复习。</p>
    </section>

    <div class="unit-filters" aria-label="学习单元筛选">
      <label class="unit-search">
        <span>搜索</span>
        <input v-model="query" type="search" placeholder="标题、分类、不变量或复杂度" />
      </label>
      <label>
        <span>阶段</span>
        <select v-model="stage">
          <option value="all">全部阶段</option>
          <option v-for="(label, key) in stageLabels" :key="key" :value="key">{{ label }}</option>
        </select>
      </label>
      <label>
        <span>难度</span>
        <select v-model="difficulty">
          <option value="all">全部难度</option>
          <option value="入门">入门</option>
          <option value="进阶">进阶</option>
          <option value="高级">高级</option>
          <option value="混合">混合</option>
        </select>
      </label>
      <label>
        <span>掌握状态</span>
        <select v-model="statusFilter">
          <option value="all">全部状态</option>
          <option value="not-started">未开始</option>
          <option value="learning">学习中</option>
          <option value="verified">已验证</option>
          <option value="review">待复习</option>
        </select>
      </label>
    </div>

    <p class="unit-result-count" aria-live="polite">显示 {{ filteredUnits.length }} 个单元；顺序已按前置依赖校正。</p>

    <ol v-if="filteredUnits.length" class="unit-list">
      <li v-for="unit in filteredUnits" :id="`unit-${unit.id}`" :key="unit.id" class="unit-item">
        <article>
          <div class="unit-heading-row">
            <div>
              <p class="unit-meta">{{ unit.stageLabel }} · {{ unit.category }} · {{ unit.difficulty }}</p>
              <h3 :id="`unit-title-${unit.id}`" tabindex="-1">{{ unit.title }}</h3>
            </div>
            <div class="unit-status-actions">
              <span class="unit-status" :class="`is-${statusOf(unit.id)}`">{{ statusLabel(unit.id) }}</span>
              <button v-if="statusOf(unit.id) === 'not-started'" type="button" @click="startUnit(unit.id)">开始学习</button>
            </div>
          </div>

          <dl class="unit-facts">
            <div>
              <dt>核心不变量</dt>
              <dd>{{ unit.invariant }}</dd>
            </div>
            <div>
              <dt>复杂度边界</dt>
              <dd>{{ unit.complexity }}</dd>
            </div>
          </dl>

          <p v-if="unit.prerequisites.length" class="unit-prerequisites">
            <strong>前置：</strong>
            <template v-for="(id, index) in unit.prerequisites" :key="id">
              <button type="button" class="unit-inline-link" @click="focusUnit(id)">{{ prerequisiteTitle(id) }}</button><span v-if="index < unit.prerequisites.length - 1">、</span>
            </template>
            <span v-if="!prerequisitesSatisfied(unit)" class="unit-prerequisite-warning"> · 尚未全部验证</span>
          </p>
          <p v-else class="unit-prerequisites"><strong>前置：</strong>可直接开始</p>

          <details class="unit-mastery" @toggle="handleMasteryToggle(unit.id, $event)">
            <summary>掌握验证：自测 + 三类证据</summary>
            <div class="unit-mastery-body">
              <p class="unit-mastery-note">这是本机自证流程，站点不会替你执行 C# 测试。请在仓库中完成任务后如实勾选。</p>

              <fieldset class="unit-quiz">
                <legend>{{ mastery(unit).selfCheck.question }}</legend>
                <label v-for="(option, index) in mastery(unit).selfCheck.options" :key="option">
                  <input v-model="quizAnswers[unit.id]" type="radio" :name="`quiz-${unit.id}`" :value="index" @change="handleQuizChoice(unit.id)" />
                  <span>{{ option }}</span>
                </label>
                <button type="button" @click="submitSelfCheck(unit)">提交自测</button>
                <p v-if="quizFeedback[unit.id]" class="unit-quiz-feedback" :class="`is-${quizFeedback[unit.id]}`" role="status" aria-live="polite">
                  <template v-if="quizFeedback[unit.id] === 'missing'">请先选择一个答案。</template>
                  <template v-else-if="quizFeedback[unit.id] === 'correct'">回答正确。{{ mastery(unit).selfCheck.explanation }}</template>
                  <template v-else>回答不正确。{{ mastery(unit).selfCheck.explanation }}</template>
                </p>
              </fieldset>

              <fieldset class="unit-evidence">
                <legend>掌握证据清单</legend>
                <label v-for="evidenceId in masteryEvidenceIds" :key="evidenceId">
                  <input
                    type="checkbox"
                    :checked="evidenceChecked(unit.id, evidenceId)"
                    @change="toggleEvidence(unit.id, evidenceId, $event)"
                  />
                  <span>
                    <strong>{{ evidenceId === 'invariant' ? '解释不变量' : evidenceId === 'test' ? '运行与扩展测试' : '迁移或反例' }}</strong>
                    {{ mastery(unit).evidence[evidenceId] }}
                  </span>
                </label>
              </fieldset>

              <div class="unit-mastery-actions">
                <button
                  v-if="statusOf(unit.id) !== 'verified' && statusOf(unit.id) !== 'review'"
                  type="button"
                  class="unit-verify"
                  :disabled="!canVerify(unit.id)"
                  @click="verifyUnit(unit.id)"
                >
                  验证掌握
                </button>
                <button
                  v-else-if="statusOf(unit.id) === 'review'"
                  type="button"
                  class="unit-verify"
                  :disabled="!canReview(unit.id)"
                  @click="completeReview(unit.id)"
                >
                  重新答对自测后完成复习
                </button>
                <p v-if="statusOf(unit.id) === 'verified' && progressStore.units[unit.id]?.nextReviewAt">
                  下次复习：<time :datetime="new Date(progressStore.units[unit.id].nextReviewAt!).toISOString()">{{ formatDate(progressStore.units[unit.id].nextReviewAt) }}</time>
                </p>
                <button v-if="progressStore.units[unit.id]" type="button" class="unit-reset-one" @click="resetUnit(unit.id)">重置此单元</button>
              </div>
            </div>
          </details>

          <details class="unit-resources">
            <summary>源码、测试与运行命令</summary>
            <div class="unit-resource-grid">
              <div>
                <h4>实现</h4>
                <ul>
                  <li v-for="path in unit.sources" :key="path"><a :href="sourceUrl(path)">{{ path }}</a></li>
                </ul>
              </div>
              <div>
                <h4>测试</h4>
                <ul>
                  <li v-for="path in unit.tests" :key="path"><a :href="sourceUrl(path)">{{ path }}</a></li>
                </ul>
              </div>
            </div>
            <div class="unit-command">
              <code>{{ unit.testCommand }}</code>
              <button type="button" @click="copyCommand(unit)">{{ copiedId === unit.id ? '已复制' : copyFailedId === unit.id ? '复制失败' : '复制命令' }}</button>
            </div>
          </details>

          <div class="unit-links">
            <a :href="withBase(unit.guide)">阅读指南</a>
            <a v-if="unit.trace" :href="withBase(`/追踪实验室?trace=${unit.trace}`)">打开 Trace</a>
          </div>
        </article>
      </li>
    </ol>

    <div v-else class="unit-empty">
      <strong>没有匹配的学习单元</strong>
      <p>调整关键词或筛选条件后再试。</p>
    </div>
  </section>
</template>
