<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue'
import { withBase } from 'vitepress'
import { masteryEvidenceIds, masteryRevision } from '../../data/masteryContent.ts'
import { emptyStore, migrateLegacy, statusOf, type ProgressStore, type UnitStatus } from '../../data/progress.ts'
import { ProgressRepository } from '../../data/progressRepository.ts'
import { daysAgo, isMastered, orderedUnits, readCourse, stepsDone, unitSteps } from '../../data/record.ts'
import type { LearningUnit } from '../../data/learningUnits.ts'
import NbText from './NbText'

const databaseName = 'dsa-learning-progress'

const store = shallowRef<ProgressStore>(emptyStore())
const now = ref(0)
const ready = ref(false)
const copyState = ref<'idle' | 'copied' | 'failed'>('idle')
let copyTimer: number | undefined

/**
 * Reads the record the units page keeps, without ever writing it. A database opened here by name
 * alone would be created without its `progress` store and break the units page, so it is opened
 * only when it already exists, or through ProgressRepository, which creates it the same way.
 */
async function readRecord(): Promise<ProgressStore> {
  let existed = true
  try {
    if (typeof indexedDB.databases === 'function') {
      existed = (await indexedDB.databases()).some((database) => database.name === databaseName)
    }
    if (existed) {
      const repository = await ProgressRepository.open()
      try {
        const current = await repository.read()
        if (current.revision > 0 || current.generation > 0) return current
      } finally {
        repository.close()
      }
    }
  } catch {
    // Storage can be disabled; the legacy record below is still worth showing.
  }
  try {
    return migrateLegacy(localStorage, Date.now())
  } catch {
    return emptyStore()
  }
}

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

const unitHref = (unit: LearningUnit) => withBase(`/学习单元.html#unit-${unit.id}`)
const continueHref = computed(() => runUnit.value ? unitHref(runUnit.value) : withBase('/学习单元.html'))
const continueLead = computed(() => ({
  empty: '从第一个单元开始',
  learning: '接着学',
  review: '复习到期',
  next: '下一个单元',
  complete: ''
})[course.value.kind])

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

const stageKeys: Record<string, string> = {
  S02: '阶段二', S03: '阶段三', S04: '阶段四', S05: '阶段五', S06: '阶段六', S07: '阶段七',
  A01: '进阶', A02: '进阶', P03: 'P3', PRACTICE: '题库'
}
const statusWords: Record<UnitStatus, string> = { 'not-started': '未开始', learning: '学习中', verified: '已验证', review: '待复习' }

function unitStatus(unit: LearningUnit) {
  return statusOf(store.value.units[unit.id], now.value)
}

function unitDetail(unit: LearningUnit) {
  const record = store.value.units[unit.id]
  const status = unitStatus(unit)
  if (!record || status === 'not-started') return ''
  if (status === 'review') return '复习已到期'
  if (status === 'verified') {
    return record.nextReviewAt
      ? `下次复习 ${new Intl.DateTimeFormat('zh-CN', { month: 'long', day: 'numeric' }).format(record.nextReviewAt)}`
      : ''
  }
  const evidence = record.evidenceRevision === masteryRevision ? record.evidenceIds.length : 0
  const selfCheck = record.selfTestPassedAt && record.selfTestRevision === masteryRevision ? '自测已过' : '自测未过'
  return `${selfCheck} · 证据 ${evidence} / ${masteryEvidenceIds.length}`
}

function unitFraction(unit: LearningUnit) {
  return stepsDone(store.value.units[unit.id], now.value) / unitSteps
}

// Section ids keep the anchors of the previous home page (path, topics, projects, evidence, cta).
interface Row { key: string; title: string; href: string; note: string; state: string; id?: string }
const tracks: Row[] = [
  { key: '主线', title: '主线路线总览', href: '/主线/', note: '从线性结构、排序和树出发，掌握字符串、图、动态规划与回溯，最后用测试证据验收。', state: '8 个阶段' },
  { key: '进阶', title: '进阶路线总览', href: '/进阶/', note: '哈希、并查集、缓存、区间结构与高级图算法，以及比较器、不变量和性能证据。', state: '18 周计划' },
  { key: '题库', title: '经典 19 题', href: '/LeetCode经典题目学习指导.html', note: '入门题型：每题先说清选择理由和不变量，再写 C# 实现。', state: '19 题' },
  { key: '题库', title: '完整 100 题', href: '/LeetCode100题完整学习指导.html', note: '按题型组织的 C# 14 主线，逐题解释选择理由、核心不变量和常见错误。', state: '100 题 · 16 周' },
  { key: '题库', title: 'P3 高级专题', href: '/P3数论位运算与高级算法学习指导.html', note: '数论、位运算、网络流、高级字符串与 DP。', state: '14 题' },
  { key: '项目', title: '综合项目总览', href: '/综合项目/', note: '配送、搜索、调度和存储：在目标函数、容量和恢复约束里组合多个结构。', state: '4 个项目', id: 'projects-title' }
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
      <div class="desk-record">
        <h1 id="course-title">算法研习室</h1>
        <a id="cta-title" class="continue-learning" :href="continueHref" :data-kind="course.kind">
          <template v-if="course.kind === 'complete'">22 个单元都已验证：打开学习单元</template>
          <template v-else>{{ continueLead }}：<NbText :text="runUnit?.title" /></template>
        </a>
        <div class="record-panel" :data-ready="ready">
          <div
            class="record-track"
            role="progressbar"
            aria-label="已验证的学习单元"
            aria-valuemin="0"
            :aria-valuemax="course.total"
            :aria-valuenow="course.verified"
          ><span :style="{ transform: `scaleX(${course.verified / course.total})` }" /></div>
          <p v-if="course.hasRecord" class="record-copy">
            <strong>{{ course.verified }} / {{ course.total }}</strong> 已验证
            · {{ course.learning }} 个学习中<template v-if="course.due"> · {{ course.due }} 个待复习</template>
            <template v-if="course.lastUpdatedAt"> · 上次学习：{{ daysAgo(course.lastUpdatedAt, now) }}</template>
          </p>
          <p v-else class="record-copy"><strong>0 / {{ course.total }}</strong> 已验证 · 这台电脑上还没有学习记录</p>
        </div>
      </div>

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
        <p>按前置依赖排序。自测答对并完成三类证据后才算已验证，记录只保存在这台电脑的浏览器里。</p>
      </div>
      <ol class="rows unit-register">
        <li
          v-for="unit in orderedUnits"
          :key="unit.id"
          class="row unit-row nb-entry"
          :data-unit="unit.id"
          :data-record="isMastered(unitStatus(unit)) ? 'done' : undefined"
          :aria-current="stoppedId === unit.id ? 'step' : undefined"
          :style="{ '--nb-fraction': unitFraction(unit) }"
        >
          <span class="row-key">{{ stageKeys[unit.stage] ?? unit.stage }}</span>
          <div class="row-name">
            <h3><a :href="unitHref(unit)"><NbText :text="unit.title" /></a></h3>
            <p v-if="stoppedId === unit.id" class="nb-stopped">停在这里</p>
          </div>
          <p class="row-note"><NbText :text="unit.category" /> · {{ unit.difficulty }}</p>
          <p class="row-state" :data-status="unitStatus(unit)">
            <span class="row-state-word">{{ statusWords[unitStatus(unit)] }}</span>
            <span v-if="unitDetail(unit)" class="row-state-detail">{{ unitDetail(unit) }}</span>
          </p>
        </li>
      </ol>
    </section>

    <section class="home-section" aria-labelledby="topics-title">
      <div class="section-head">
        <h2 id="topics-title">课程与题库</h2>
      </div>
      <ul class="rows">
        <li v-for="row in tracks" :key="row.href" class="row">
          <span class="row-key">{{ row.key }}</span>
          <div class="row-name"><h3 :id="row.id"><a :href="withBase(row.href)">{{ row.title }}</a></h3></div>
          <p class="row-note"><NbText :text="row.note" /></p>
          <p class="row-state"><span class="row-state-word">{{ row.state }}</span></p>
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
