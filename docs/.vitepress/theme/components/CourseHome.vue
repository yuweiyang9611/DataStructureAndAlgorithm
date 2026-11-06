<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue'
import { withBase } from 'vitepress'
import { masteryEvidenceIds, masteryRevision } from '../../data/masteryContent.ts'
import { emptyStore, statusOf, type ProgressStore, type UnitStatus } from '../../data/progress.ts'
import { daysAgo, isMastered, orderedUnits, readCourse, stepsDone, unitSteps } from '../../data/record.ts'
import type { LearningUnit } from '../../data/learningUnits.ts'
import { hueOf, stageWords, trackHues, trackOf, trackOrder, trackWords, type Track } from '../hues.ts'
import { readRecord } from '../readRecord.ts'
import NbText from './NbText'

const store = shallowRef<ProgressStore>(emptyStore())
const now = ref(0)
const ready = ref(false)
const copyState = ref<'idle' | 'copied' | 'failed'>('idle')
let copyTimer: number | undefined

onMounted(async () => {
  const record = await readRecord()
  now.value = Date.now()
  store.value = record
  ready.value = true
})
onBeforeUnmount(() => { if (copyTimer !== undefined) window.clearTimeout(copyTimer) })

const course = computed(() => readCourse(store.value, now.value))
const runUnit = computed(() => course.value.unit)
const stoppedId = computed(() => course.value.hasRecord && course.value.kind !== 'complete' ? course.value.unit?.id : undefined)
const lastRecord = computed(() => course.value.lastUpdatedAt ? daysAgo(course.value.lastUpdatedAt, now.value) : '还没有')

const unitHref = (unit: LearningUnit) => withBase(`/学习单元.html#unit-${unit.id}`)
const continueHref = computed(() => runUnit.value ? unitHref(runUnit.value) : withBase('/学习单元.html'))
const continueLead = computed(() => ({
  empty: '从第一个单元开始',
  learning: '接着学',
  review: '复习到期',
  next: '下一个单元',
  complete: ''
})[course.value.kind])
const continueWord = computed(() => course.value.kind === 'empty' ? '开始学习' : course.value.kind === 'complete' ? '打开学习单元' : '继续学习')

const fullTestCommand = 'dotnet test DataStructureAndAlgorithm.Test/DataStructureAndAlgorithm.Test.csproj -c Release'
const command = computed(() => runUnit.value?.testCommand ?? fullTestCommand)
// The commands run 161 to 285 characters, wider than any desktop page, so the display continues the
// line PowerShell-style before --filter, and every line holds whole tokens. A filter that names two
// test classes is still one token, so on its own line it can scroll. 复制命令 copies the repository's
// one-line command.
const commandDisplay = computed(() => {
  const at = command.value.indexOf(' --filter ')
  return at < 0 ? command.value : `${command.value.slice(0, at)} \`\n  ${command.value.slice(at + 1)}`
})

async function copyCommand() {
  try {
    await navigator.clipboard.writeText(command.value)
    copyState.value = 'copied'
  } catch {
    copyState.value = 'failed'
  }
  if (copyTimer !== undefined) window.clearTimeout(copyTimer)
  copyTimer = window.setTimeout(() => { copyState.value = 'idle' }, 1600)
}

// The chips filter the register by track; the server render shows every card.
const filter = ref<'all' | Track>('all')
const trackCounts = computed(() => Object.fromEntries(trackOrder.map((track) => [track, orderedUnits.filter((unit) => trackOf(unit) === track).length])) as Record<Track, number>)
const statusWords: Record<UnitStatus, string> = { 'not-started': '未开始', learning: '学习中', verified: '已验证', review: '待复习' }

function unitStatus(unit: LearningUnit) {
  return statusOf(store.value.units[unit.id], now.value)
}

/** The state figure: 未开始, n / 3 类验证 while learning, then the verified word and its review date. */
function unitState(unit: LearningUnit) {
  const record = store.value.units[unit.id]
  const status = unitStatus(unit)
  if (!record || status === 'not-started') return { word: statusWords[status], detail: '' }
  if (status === 'review') return { word: statusWords[status], detail: '复习已到期' }
  if (status === 'verified') {
    const detail = record.nextReviewAt
      ? `下次复习 ${new Intl.DateTimeFormat('zh-CN', { month: 'long', day: 'numeric' }).format(record.nextReviewAt)}`
      : ''
    return { word: statusWords[status], detail }
  }
  const evidence = record.evidenceRevision === masteryRevision ? record.evidenceIds.length : 0
  const selfCheck = record.selfTestPassedAt && record.selfTestRevision === masteryRevision ? '自测已过' : ''
  return { word: `${evidence} / ${masteryEvidenceIds.length} 类验证`, detail: selfCheck }
}

function unitFraction(unit: LearningUnit) {
  return stepsDone(store.value.units[unit.id], now.value) / unitSteps
}
function unitPercent(unit: LearningUnit) {
  return Math.round(unitFraction(unit) * 100)
}

/** Where the unit is taught: the guide page, named as a place in the book. */
const guidePlaces: Record<string, string> = {
  '/LeetCode100题完整学习指导': '题库 · 完整 100 题',
  '/P3数论位运算与高级算法学习指导': '题库 · P3 高级专题'
}
function unitPlace(unit: LearningUnit) {
  return guidePlaces[unit.guide] ?? unit.guide.split('/').filter(Boolean).join(' · ')
}

// Section ids keep the anchors of the previous home page (path, topics, projects, evidence, cta).
interface Row { key: string; title: string; href: string; note: string; state: string; id?: string; hue?: string }
const tracks: Row[] = [
  { key: trackWords.main, hue: trackHues.main, title: '主线路线总览', href: '/主线/', note: '从线性结构、排序和树出发，掌握字符串、图、动态规划与回溯，最后用测试证据验收。', state: '8 个阶段' },
  { key: trackWords.advanced, hue: trackHues.advanced, title: '进阶路线总览', href: '/进阶/', note: '哈希、并查集、缓存、区间结构与高级图算法，以及比较器、不变量和性能证据。', state: '18 周计划' },
  { key: trackWords.practice, hue: trackHues.practice, title: '经典 19 题', href: '/LeetCode经典题目学习指导.html', note: '入门题型：每题先说清选择理由和不变量，再写 C# 实现。', state: '19 题' },
  { key: trackWords.practice, hue: trackHues.practice, title: '完整 100 题', href: '/LeetCode100题完整学习指导.html', note: '按题型组织的 C# 14 主线，逐题解释选择理由、核心不变量和常见错误。', state: '100 题 · 16 周' },
  { key: trackWords.practice, hue: trackHues.practice, title: 'P3 高级专题', href: '/P3数论位运算与高级算法学习指导.html', note: '数论、位运算、网络流、高级字符串与 DP。', state: '14 题' },
  { key: trackWords.project, hue: trackHues.project, title: '综合项目总览', href: '/综合项目/', note: '配送、搜索、调度和存储：在目标函数、容量和恢复约束里组合多个结构。', state: '4 个项目', id: 'projects-title' }
]
const tools: Row[] = [
  { key: '追踪', title: '算法 Trace 实验室', href: '/追踪实验室.html', note: 'GCD、基数排序、Dijkstra、矩阵链、红黑树和项目调度，逐步播放 C# Demo 产生的真实事件。', state: '6 个场景' },
  { key: '源码', title: '文档与源码导航', href: '/源码导航.html', note: '从学习文档直接找到实现、测试、Demo 与综合项目。', state: '' },
  { key: '约定', title: 'API 与比较器约定', href: '/API设计与比较器约定.html', note: '半开区间、比较器、重复值与数值契约。', state: '' },
  { key: '入门', title: '从这里开始', href: '/开始学习.html', note: '10 分钟快速开始，以及按目标选择第一站。', state: '' }
]
const singlePages: Row[] = [
  { key: '', title: '完整主线单页', href: '/CSharp数据结构与算法学习指导.html', note: '', state: '' },
  { key: '', title: '完整进阶单页', href: '/CSharp数据结构与算法进阶学习指导.html', note: '', state: '' },
  { key: '', title: '完整项目单页', href: '/综合项目实战学习指导.html', note: '', state: '' }
]
</script>

<template>
  <div class="course-home">
    <section class="desk" aria-labelledby="course-title">
      <div class="masthead">
        <div>
          <h1 id="course-title">算法研习室</h1>
          <p class="masthead-note">22 个学习单元，按前置依赖排序。每个单元答对自测并交齐三类证据后才算已验证，记录只保存在这台电脑的浏览器里。</p>
        </div>
        <div class="stats" aria-label="学习概况" :data-ready="ready">
          <p class="nb-stat" data-hue="green"><b>{{ course.verified }}</b><span>已验证单元</span></p>
          <p class="nb-stat" data-hue="cyan"><b>{{ course.learning }}</b><span>个学习中</span></p>
          <p class="nb-stat" data-hue="blue"><b>{{ lastRecord }}</b><span>上次记录</span></p>
        </div>
      </div>

      <section class="resume nb-banner" aria-labelledby="resume-label" :data-kind="course.kind">
        <div class="resume-copy">
          <p id="resume-label" class="nb-banner-eyebrow">接着做</p>
          <p class="nb-banner-title">
            <a id="cta-title" class="continue-learning" :href="continueHref" :data-kind="course.kind">
              <template v-if="course.kind === 'complete'">22 个单元都已完成：打开学习单元</template>
              <template v-else>{{ continueLead }}：<NbText :text="runUnit?.title" /></template>
            </a>
          </p>
          <p v-if="course.hasRecord" class="nb-banner-note record-copy">
            <template v-if="course.step">停在「{{ course.step.label }}」 · </template><strong>{{ course.verified }} / {{ course.total }}</strong> 已验证
            · {{ course.learning }} 个学习中<template v-if="course.due"> · {{ course.due }} 个待复习</template>
            <template v-if="course.lastUpdatedAt"> · 上次学习：{{ daysAgo(course.lastUpdatedAt, now) }}</template>
          </p>
          <p v-else class="nb-banner-note record-copy"><strong>0 / {{ course.total }}</strong> 已验证 · 这台电脑上还没有学习记录</p>
          <div
            class="record-track nb-bar nb-bar--on-fill"
            role="progressbar"
            aria-label="已验证的学习单元"
            aria-valuemin="0"
            :aria-valuemax="course.total"
            :aria-valuenow="course.verified"
            :style="{ '--nb-fraction': course.verified / course.total }"
          ><span /></div>
        </div>
        <a class="nb-button nb-button--solid resume-go" :href="continueHref">{{ continueWord }}</a>
      </section>

      <div class="desk-run" role="group" aria-labelledby="run-title">
        <h2 id="run-title">{{ runUnit ? '运行这个单元的测试' : '运行全部测试' }}</h2>
        <pre class="nb-command" tabindex="0" aria-label="测试命令"><code>{{ commandDisplay }}</code></pre>
        <p class="run-actions">
          <button type="button" class="text-action" @click="copyCommand">{{ copyState === 'copied' ? '已复制' : copyState === 'failed' ? '复制失败' : '复制命令' }}</button>
          <span class="run-note">在仓库根目录运行，需要 .NET 10 SDK。</span>
          <span v-if="runUnit" class="run-links"><a :href="withBase(`${runUnit.guide}.html`)">阅读指南</a><template v-if="runUnit.trace"> · <a :href="withBase(`/追踪实验室.html?trace=${runUnit.trace}`)">打开 Trace</a></template></span>
        </p>
      </div>
    </section>

    <section class="home-section" aria-labelledby="path-title">
      <div class="section-head">
        <h2 id="path-title">22 个学习单元</h2>
        <p>卡片头的颜色是单元所属的课程，环里是这个单元走到了第几步。</p>
      </div>
      <div class="filters" role="group" aria-label="按课程筛选">
        <button type="button" class="nb-chip" data-hue="ink" :aria-pressed="filter === 'all' ? 'true' : 'false'" @click="filter = 'all'">全部 <span class="nb-chip-count">{{ orderedUnits.length }}</span></button>
        <button
          v-for="track in trackOrder"
          :key="track"
          type="button"
          class="nb-chip"
          :data-hue="trackHues[track]"
          :aria-pressed="filter === track ? 'true' : 'false'"
          @click="filter = track"
        >{{ trackWords[track] }} <span class="nb-chip-count">{{ trackCounts[track] }}</span></button>
      </div>
      <ol class="unit-register nb-grid">
        <li
          v-for="unit in orderedUnits"
          :key="unit.id"
          class="unit-card nb-card"
          :data-unit="unit.id"
          :data-hue="hueOf(unit)"
          :data-status="unitStatus(unit)"
          :data-record="isMastered(unitStatus(unit)) ? 'done' : undefined"
          :aria-current="stoppedId === unit.id ? 'step' : undefined"
          :hidden="filter !== 'all' && trackOf(unit) !== filter"
          :style="{ '--nb-fraction': unitFraction(unit) }"
        >
          <div class="nb-card-head">
            <div>
              <p class="nb-card-eyebrow">{{ stageWords[unit.stage] ?? unit.stage }} · {{ unit.difficulty }}</p>
              <h3><a :href="unitHref(unit)"><NbText :text="unit.title" /></a></h3>
            </div>
            <div class="nb-ring" role="img" :aria-label="`完成 ${unitPercent(unit)}%`" :style="{ '--nb-fraction': unitFraction(unit) }"><span>{{ unitPercent(unit) }}%</span></div>
          </div>
          <div class="nb-card-body">
            <p v-if="stoppedId === unit.id" class="nb-stopped">停在这里</p>
            <p class="unit-note"><NbText :text="unit.invariant" /></p>
            <div class="nb-card-meta">
              <span class="unit-state"><b>{{ unitState(unit).word }}</b><template v-if="unitState(unit).detail"> · {{ unitState(unit).detail }}</template></span>
              <span class="unit-place">{{ unitPlace(unit) }}</span>
            </div>
          </div>
        </li>
      </ol>
    </section>

    <section class="home-section" aria-labelledby="topics-title">
      <div class="section-head">
        <h2 id="topics-title">课程与题库</h2>
      </div>
      <ul class="tracks nb-grid">
        <li v-for="row in tracks" :key="row.href" class="track-card nb-card" :data-hue="row.hue">
          <div class="nb-card-head">
            <div>
              <p class="nb-card-eyebrow">{{ row.key }} · {{ row.state }}</p>
              <h3 :id="row.id"><a :href="withBase(row.href)">{{ row.title }}</a></h3>
            </div>
          </div>
          <div class="nb-card-body">
            <p><NbText :text="row.note" /></p>
          </div>
        </li>
      </ul>
    </section>

    <section class="home-section" aria-labelledby="evidence-title">
      <div class="section-head">
        <h2 id="evidence-title">工具与参考</h2>
      </div>
      <ul class="rows">
        <li v-for="row in tools" :key="row.href" class="row">
          <span class="row-key">{{ row.key }}</span>
          <div class="row-name"><h3><a :href="withBase(row.href)">{{ row.title }}</a></h3></div>
          <p class="row-note"><NbText :text="row.note" /></p>
          <p class="row-state"><span class="row-state-word">{{ row.state }}</span></p>
        </li>
        <li class="row">
          <span class="row-key">单页</span>
          <div class="row-name"><h3>连续阅读</h3></div>
          <p class="row-note row-links">
            <a v-for="page in singlePages" :key="page.href" :href="withBase(page.href)">{{ page.title }}</a>
          </p>
          <p class="row-state" />
        </li>
      </ul>
    </section>
  </div>
</template>
