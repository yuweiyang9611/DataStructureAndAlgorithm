<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, toRaw } from 'vue'
import { withBase } from 'vitepress'
import {
  masteryByUnitId,
  masteryEvidenceIds,
  masteryRevision,
  type MasteryEvidenceId
} from '../../data/masteryContent'
import { learningUnits, stageLabels, type LearningUnit } from '../../data/learningUnits'
import { sourceHref } from '../sourceBundle'
import SourcePanel, { type SourcePanelFile } from './SourcePanel.vue'

import { emptyStore, applyAction, statusOf as progressStatus, canVerify as progressCanVerify, canReview as progressCanReview, migrateLegacy, parseBackup, exportBackup, mergeBackup, type ProgressAction, type ImportData } from '../../data/progress.ts'
import { parseBackupJson } from '../../data/backupJson.ts'
import { ProgressRepository, StaleProgressError } from '../../data/progressRepository.ts'
import { orderedUnits, readCourse, stepsDone, unitSteps } from '../../data/record.ts'
import { hueOf } from '../hues.ts'
import NbText from './NbText'
type QuizFeedback = 'correct' | 'incorrect' | 'missing'
const unitById = new Map(learningUnits.map((unit) => [unit.id, unit]))
const query = ref('')
const stage = ref('all')
const difficulty = ref('all')
const statusFilter = ref('all')
const progressStore = ref(emptyStore())
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

let repositoryStore: ProgressRepository | undefined
let channel: BroadcastChannel | undefined
let disposed = false
let queue = Promise.resolve()
let queuedOperations = 0
const pending = ref(false)
const notice = ref('')
const importData = ref<ImportData | null>(null)
const importPreview = computed(() => importData.value ? mergeBackup(toRaw(progressStore.value), toRaw(importData.value)).counts : null)
let previewRevision = 0

function accept(store: ReturnType<typeof emptyStore>) {
  if (!disposed && store.revision >= progressStore.value.revision) progressStore.value = store
}
async function refresh() {
  if (!repositoryStore || storageError.value || pending.value) return
  try { accept(await repositoryStore.read()) } catch { storageError.value = true }
}
function dispatch(action: ProgressAction) {
  const expected = { generation: progressStore.value.generation, deletedAt: 'id' in action ? progressStore.value.deleted[action.id] ?? 0 : 0 }
  queuedOperations++
  pending.value = true
  queue = queue.then(async () => {
    if (!hydrated.value || disposed) return
    try {
      const store = repositoryStore && !storageError.value
        ? await repositoryStore.dispatch(action, expected, Date.now())
        : applyAction(toRaw(progressStore.value), action, Date.now())
      accept(store)
      channel?.postMessage('changed')
      now.value = Date.now()
    } catch (error) {
      if (error instanceof StaleProgressError) {
        notice.value = error.message
        if (repositoryStore) accept(await repositoryStore.read())
      } else {
        storageError.value = true
        accept(applyAction(toRaw(progressStore.value), action, Date.now()))
      }
    }
  }).catch(error => { notice.value = String(error) }).finally(() => { queuedOperations--; pending.value = queuedOperations > 0; if (!pending.value) void refresh() })
}
function statusOf(id: string) { return progressStatus(progressStore.value.units[id], now.value) }

function exportProgress() {
  const blob = new Blob([JSON.stringify(exportBackup(toRaw(progressStore.value)), null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url; link.download = 'learning-progress-v3.json'; link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
async function previewImport(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  importData.value = null; notice.value = ''
  if (!file) return
  try {
    if (file.size > 1_000_000) throw new Error('备份文件不能超过 1 MB。')
    importData.value = parseBackup(parseBackupJson(await file.text()))
    previewRevision = progressStore.value.revision
  } catch (error) { notice.value = error instanceof Error ? error.message : '备份读取失败。' }
  input.value = ''
}
async function confirmImport() {
  if (!importData.value || pending.value) return
  pending.value = true
  try {
    if (progressStore.value.revision !== previewRevision) throw new StaleProgressError()
    const data = toRaw(importData.value)
    const merged = repositoryStore && !storageError.value
      ? await repositoryStore.import(data, previewRevision)
      : mergeBackup(toRaw(progressStore.value), data).store
    accept(merged); importData.value = null; channel?.postMessage('changed')
    notice.value = storageError.value ? '已合并到本次页面，尚未持久化，请导出备份。' : '备份已合并并保存。'
  } catch (error) {
    notice.value = error instanceof Error ? error.message : '导入未保存。'
    importData.value = null
    if (error instanceof StaleProgressError && repositoryStore) accept(await repositoryStore.read())
  } finally { pending.value = false }
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
// The one stopped mark in the register, from the same rule as the course's continue link.
const stoppedId = computed(() => {
  if (!hydrated.value) return undefined
  const course = readCourse(progressStore.value, now.value)
  return course.hasRecord && course.kind !== 'complete' ? course.unit?.id : undefined
})
function unitFraction(id: string) { return stepsDone(progressStore.value.units[id], now.value) / unitSteps }
// The band at the top takes the hue of the unit to continue with, so it matches the card on the opening screen.
const bandHue = computed(() => hueOf(readCourse(progressStore.value, now.value).unit ?? orderedUnits[0]))

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

function startUnit(id: string) { dispatch({ type: 'start', id }) }
function handleMasteryToggle(id: string, event: Event) {
  if ((event.target as HTMLDetailsElement).open && !progressStore.value.units[id]) startUnit(id)
}
function toggleEvidence(id: string, evidenceId: MasteryEvidenceId, event: Event) {
  dispatch({ type: 'evidence', id, evidenceId, checked: (event.target as HTMLInputElement).checked })
}
function handleQuizChoice(id: string) {
  quizFeedback.value = { ...quizFeedback.value, [id]: undefined }
  dispatch({ type: 'clear-answer', id })
}
function submitSelfCheck(unit: LearningUnit) {
  const selected = quizAnswers.value[unit.id]
  if (selected === undefined) { quizFeedback.value[unit.id] = 'missing'; return }
  const correct = selected === mastery(unit).selfCheck.correctIndex
  dispatch({ type: 'answer', id: unit.id, correct })
  quizFeedback.value[unit.id] = correct ? 'correct' : 'incorrect'
}
function canVerify(id: string) { return !pending.value && progressCanVerify(progressStore.value, id, now.value) }
function canReview(id: string) { return !pending.value && progressCanReview(progressStore.value.units[id], now.value) }
function verifyUnit(id: string) { dispatch({ type: 'verify', id }) }
function completeReview(id: string) { dispatch({ type: 'review', id }) }
function resetUnit(id: string) {
  dispatch({ type: 'reset', id }); delete quizAnswers.value[id]; delete quizFeedback.value[id]
}

function requestReset() {
  if (!resetArmed.value) {
    resetArmed.value = true
    if (resetTimer !== undefined) window.clearTimeout(resetTimer)
    resetTimer = window.setTimeout(() => { resetArmed.value = false }, 5000)
    return
  }
  dispatch({ type: 'reset-all' })
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

// Source read in place: every file a unit names is in the site's bundle, so the links stay on the site.
function sourceUrl(path: string) {
  return withBase(sourceHref(path))
}

// The panel in a unit shows its first implementation file and its first test file. It fetches nothing
// until the details element opens, so the page does not load 44 files on arrival.
const resourcesOpen = ref<Record<string, boolean>>({})
function handleResourcesToggle(id: string, event: Event) {
  resourcesOpen.value = { ...resourcesOpen.value, [id]: (event.target as HTMLDetailsElement).open }
}
function panelFiles(unit: LearningUnit): SourcePanelFile[] {
  return [unit.sources[0], unit.tests[0]].filter((path): path is string => !!path).map((path) => ({ path }))
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

onMounted(async () => {
  let legacy = emptyStore()
  try { legacy = migrateLegacy(localStorage, Date.now()) } catch { /* Storage can be disabled. */ }
  try {
    repositoryStore = await ProgressRepository.open()
    const initial = await repositoryStore.initialize(legacy)
    if (disposed) { repositoryStore.close(); return }
    accept(initial)
    migrationNotice.value = Object.values(initial.units).some(record => record.legacyCompleted)
  } catch { accept(legacy); storageError.value = true }
  finally { hydrated.value = true }
  if (disposed) return
  if (typeof BroadcastChannel !== 'undefined') {
    channel = new BroadcastChannel('dsa-learning-progress')
    channel.onmessage = () => { void refresh() }
  }
  window.addEventListener('focus', refresh)
  clockTimer = window.setInterval(() => { now.value = Date.now(); void refresh() }, 60_000)
})

onBeforeUnmount(() => {
  disposed = true; channel?.close(); repositoryStore?.close()
  window.removeEventListener('focus', refresh)
  if (clockTimer !== undefined) window.clearInterval(clockTimer)
  if (resetTimer !== undefined) window.clearTimeout(resetTimer)
  if (copyTimer !== undefined) window.clearTimeout(copyTimer)
})
</script>


<template>
  <section class="unit-explorer" :data-ready="hydrated" :inert="!hydrated" aria-labelledby="unit-explorer-heading">
    <div class="unit-progress">
      <div class="unit-band nb-band" :data-hue="bandHue">
        <p class="nb-band-eyebrow unit-progress-copy" aria-live="polite">
          学习单元 · 已验证 <strong>{{ verifiedCount }} / {{ learningUnits.length }}</strong> · {{ progressPercent }}%<template v-if="dueCount"> · {{ dueCount }} 个待复习</template>
        </p>
        <div
          class="record-track nb-bar nb-bar--on-fill"
          role="progressbar"
          aria-label="已验证学习单元"
          aria-valuemin="0"
          :aria-valuemax="learningUnits.length"
          :aria-valuenow="verifiedCount"
          :style="{ '--nb-fraction': verifiedCount / learningUnits.length }"
        ><span /></div>
      </div>
      <h2 id="unit-explorer-heading">22 个可验证学习单元</h2>
      <p class="unit-progress-note">进度只保存在当前浏览器。自测答对并完成不变量、测试和迁移三类证据后，单元才会标记为“已验证<span class="nb-halt">”</span>。</p>
    </div>

    <div class="unit-backup">
      <div class="unit-backup-actions">
        <button type="button" class="text-action" :disabled="!hydrated || pending" @click="exportProgress">导出进度备份</button>
        <label class="text-action text-action--file">导入进度备份<input type="file" accept="application/json,.json" :disabled="!hydrated || pending" @change="previewImport" /></label>
        <template v-if="hasProgress">
          <button type="button" class="text-action text-action--wrong" @click="requestReset">{{ resetArmed ? '确认清空全部进度' : '清空本地进度' }}</button>
          <button v-if="resetArmed" type="button" class="text-action" @click="cancelReset">取消</button>
        </template>
      </div>
      <p class="unit-backup-note">备份按单元更新时间合并，相同时间保留本地记录；跨设备导入前请校准设备时钟。</p>
      <div v-if="importPreview" class="unit-import-preview" role="status">
        新增 {{ importPreview.added }} · 更新 {{ importPreview.updated }} · 删除 {{ importPreview.deleted }} · 忽略 {{ importPreview.ignored }}
        <button type="button" class="nb-button nb-button--solid" :disabled="pending" @click="confirmImport">确认合并备份</button>
        <button type="button" class="text-action" @click="importData = null">取消导入</button>
      </div>
      <p v-if="notice" class="unit-backup-notice" role="status"><NbText :text="notice" /></p>
    </div>
    <p v-if="migrationNotice" class="unit-notice" role="status">
      旧版完成记录已迁移为“学习中<span class="nb-halt">”</span>。请补答自测并完成证据清单，不会丢失原有单元位置。
    </p>
    <p v-if="storageError" class="unit-notice unit-notice--warning" role="status">
      浏览器当前无法保存进度；本次页面内操作仍然有效，但尚未持久化，请导出备份。
    </p>

    <section class="unit-recommendations" aria-labelledby="unit-recommendations-heading">
      <h3 id="unit-recommendations-heading">根据依赖与复习时间推荐下一步</h3>
      <ol v-if="recommendations.length">
        <li v-for="recommendation in recommendations" :key="recommendation.unit.id">
          <button type="button" class="unit-jump" @click="focusUnit(recommendation.unit.id)"><NbText :text="recommendation.unit.title" /></button>
          <span>{{ recommendation.reason }}</span>
        </li>
      </ol>
      <p v-else>所有学习单元都已验证，且当前没有到期复习。</p>
    </section>

    <div class="unit-filters" aria-label="学习单元筛选">
      <label class="unit-search">
        <span>搜索</span>
        <input v-model="query" class="nb-input" type="search" placeholder="标题、分类、不变量或复杂度" />
      </label>
      <label>
        <span>阶段</span>
        <select v-model="stage" class="nb-input">
          <option value="all">全部阶段</option>
          <option v-for="(label, key) in stageLabels" :key="key" :value="key">{{ label }}</option>
        </select>
      </label>
      <label>
        <span>难度</span>
        <select v-model="difficulty" class="nb-input">
          <option value="all">全部难度</option>
          <option value="入门">入门</option>
          <option value="进阶">进阶</option>
          <option value="高级">高级</option>
          <option value="混合">混合</option>
        </select>
      </label>
      <label>
        <span>掌握状态</span>
        <select v-model="statusFilter" class="nb-input">
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
      <li
        v-for="unit in filteredUnits"
        :id="`unit-${unit.id}`"
        :key="unit.id"
        class="unit-item nb-entry"
        :data-record="isMastered(unit.id) ? 'done' : undefined"
        :aria-current="stoppedId === unit.id ? 'step' : undefined"
        :style="{ '--nb-fraction': unitFraction(unit.id) }"
      >
        <article>
          <div class="unit-heading-row">
            <div class="unit-heading">
              <h3 :id="`unit-title-${unit.id}`" tabindex="-1"><NbText :text="unit.title" /></h3>
              <p class="unit-meta">{{ unit.stageLabel }} · <NbText :text="unit.category" /> · {{ unit.difficulty }}</p>
              <p v-if="stoppedId === unit.id" class="nb-stopped">停在这里</p>
            </div>
            <div class="unit-status-actions">
              <span class="unit-status" :data-status="statusOf(unit.id)">{{ statusLabel(unit.id) }}</span>
              <button v-if="statusOf(unit.id) === 'not-started'" type="button" class="nb-button" @click="startUnit(unit.id)">开始学习</button>
            </div>
          </div>

          <dl class="unit-facts">
            <div>
              <dt>核心不变量</dt>
              <dd><NbText :text="unit.invariant" /></dd>
            </div>
            <div>
              <dt>复杂度边界</dt>
              <dd><NbText :text="unit.complexity" /></dd>
            </div>
          </dl>

          <p v-if="unit.prerequisites.length" class="unit-prerequisites">
            <strong>前置：</strong>
            <template v-for="(id, index) in unit.prerequisites" :key="id">
              <button type="button" class="unit-inline-link" @click="focusUnit(id)"><NbText :text="prerequisiteTitle(id)" /></button><span v-if="index < unit.prerequisites.length - 1">、</span>
            </template>
            <span v-if="!prerequisitesSatisfied(unit)" class="unit-prerequisite-warning"> · 尚未全部验证</span>
          </p>
          <p v-else class="unit-prerequisites"><strong>前置：</strong>可直接开始</p>

          <details class="unit-mastery" @toggle="handleMasteryToggle(unit.id, $event)">
            <summary>掌握验证：自测 + 三类证据</summary>
            <div class="unit-mastery-body">
              <p class="unit-mastery-note">这是本机自证流程，站点不会替你执行 C# 测试。请在仓库中完成任务后如实勾选。</p>

              <fieldset class="unit-quiz">
                <legend><NbText :text="mastery(unit).selfCheck.question" /></legend>
                <label v-for="(option, index) in mastery(unit).selfCheck.options" :key="option">
                  <input v-model="quizAnswers[unit.id]" type="radio" :name="`quiz-${unit.id}`" :value="index" @change="handleQuizChoice(unit.id)" />
                  <span><NbText :text="option" /></span>
                </label>
                <button type="button" class="nb-button" @click="submitSelfCheck(unit)">提交自测</button>
                <p v-if="quizFeedback[unit.id]" class="unit-quiz-feedback" :data-verdict="quizFeedback[unit.id]" role="status" aria-live="polite">
                  <template v-if="quizFeedback[unit.id] === 'missing'">请先选择一个答案。</template>
                  <template v-else-if="quizFeedback[unit.id] === 'correct'"><strong>回答正确。</strong><NbText :text="mastery(unit).selfCheck.explanation" /></template>
                  <template v-else><strong>回答不正确。</strong><NbText :text="mastery(unit).selfCheck.explanation" /></template>
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
                    <NbText :text="mastery(unit).evidence[evidenceId]" />
                  </span>
                </label>
              </fieldset>

              <div class="unit-mastery-actions">
                <button
                  v-if="statusOf(unit.id) !== 'verified' && statusOf(unit.id) !== 'review'"
                  type="button"
                  class="nb-button nb-button--solid unit-verify"
                  :disabled="!canVerify(unit.id)"
                  @click="verifyUnit(unit.id)"
                >
                  验证掌握
                </button>
                <button
                  v-else-if="statusOf(unit.id) === 'review'"
                  type="button"
                  class="nb-button nb-button--solid unit-verify"
                  :disabled="!canReview(unit.id)"
                  @click="completeReview(unit.id)"
                >
                  重新答对自测后完成复习
                </button>
                <p v-if="statusOf(unit.id) === 'verified' && progressStore.units[unit.id]?.nextReviewAt">
                  下次复习：<time :datetime="new Date(progressStore.units[unit.id].nextReviewAt!).toISOString()">{{ formatDate(progressStore.units[unit.id].nextReviewAt) }}</time>
                </p>
                <button v-if="progressStore.units[unit.id]" type="button" class="text-action text-action--wrong" @click="resetUnit(unit.id)">重置此单元</button>
              </div>
            </div>
          </details>

          <details class="unit-resources" @toggle="handleResourcesToggle(unit.id, $event)">
            <summary>源码、测试与运行命令</summary>
            <SourcePanel class="unit-source" :files="panelFiles(unit)" :deferred="!resourcesOpen[unit.id]" />
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
              <pre class="nb-command" tabindex="0" aria-label="测试命令"><code>{{ unit.testCommand }}</code></pre>
              <button type="button" class="text-action" @click="copyCommand(unit)">{{ copiedId === unit.id ? '已复制' : copyFailedId === unit.id ? '复制失败' : '复制命令' }}</button>
            </div>
          </details>

          <p class="unit-links">
            <a :href="withBase(unit.guide)">阅读指南</a>
            <a v-if="unit.trace" :href="withBase(`/追踪实验室?trace=${unit.trace}`)">打开 Trace</a>
          </p>
        </article>
      </li>
    </ol>

    <div v-else class="unit-empty">
      <strong>没有匹配的学习单元</strong>
      <p>调整关键词或筛选条件后再试。</p>
    </div>
  </section>
</template>