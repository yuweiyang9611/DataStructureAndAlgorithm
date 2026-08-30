<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { withBase } from 'vitepress'
import { learningUnits, repository, stageLabels, type LearningUnit } from '../../data/learningUnits'

const storageKey = 'dsa-learning-progress-v1'
const query = ref('')
const stage = ref('all')
const difficulty = ref('all')
const status = ref('all')
const completedIds = ref<string[]>([])
const hydrated = ref(false)
const copiedId = ref('')
const copyFailedId = ref('')

const unitById = new Map(learningUnits.map((unit) => [unit.id, unit]))
const validIds = new Set(unitById.keys())
const stageOrder = new Map(['S02', 'S03', 'S04', 'S05', 'S06', 'A01', 'A02', 'P03', 'PRACTICE', 'S07'].map((key, index) => [key, index]))

const completedSet = computed(() => new Set(completedIds.value))
const completedCount = computed(() => completedIds.value.length)
const progressPercent = computed(() => Math.round((completedCount.value / learningUnits.length) * 100))

const filteredUnits = computed(() => {
  const needle = query.value.trim().toLocaleLowerCase('zh-CN')

  return learningUnits.filter((unit) => {
    const haystack = `${unit.title} ${unit.category} ${unit.invariant} ${unit.complexity}`.toLocaleLowerCase('zh-CN')
    const matchesQuery = !needle || haystack.includes(needle)
    const matchesStage = stage.value === 'all' || unit.stage === stage.value
    const matchesDifficulty = difficulty.value === 'all' || unit.difficulty === difficulty.value
    const isComplete = completedSet.value.has(unit.id)
    const matchesStatus = status.value === 'all' || (status.value === 'done' ? isComplete : !isComplete)
    return matchesQuery && matchesStage && matchesDifficulty && matchesStatus
  }).sort((left, right) => (stageOrder.get(left.stage) ?? 99) - (stageOrder.get(right.stage) ?? 99))
})

onMounted(() => {
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) ?? '[]')
    if (Array.isArray(stored)) {
      completedIds.value = [...new Set(stored.filter((id): id is string => typeof id === 'string' && validIds.has(id)))]
    }
  } catch {
    completedIds.value = []
  } finally {
    hydrated.value = true
  }
})

watch(completedIds, (value) => {
  if (!hydrated.value) return
  try {
    localStorage.setItem(storageKey, JSON.stringify([...new Set(value)]))
  } catch {
    // 受限存储模式下保留本次内存状态；页面功能不应因持久化失败而中断。
  }
}, { deep: true })

function sourceUrl(path: string) {
  const encoded = path.split('/').map(encodeURIComponent).join('/')
  return `${repository}/blob/main/${encoded}`
}

function prerequisiteTitle(id: string) {
  return unitById.get(id)?.title ?? id
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
  window.setTimeout(() => {
    if (copiedId.value === unit.id) copiedId.value = ''
    if (copyFailedId.value === unit.id) copyFailedId.value = ''
  }, 1600)
}

function resetProgress() {
  completedIds.value = []
}
</script>

<template>
  <section class="unit-explorer" aria-labelledby="unit-explorer-heading">
    <div class="unit-progress">
      <div>
        <p class="unit-eyebrow">LOCAL PROGRESS</p>
        <h2 id="unit-explorer-heading">22 个可验证学习单元</h2>
        <p>进度只保存在当前浏览器，不上传任何数据。每个单元都直达真实源码、测试与可复现命令。</p>
      </div>
      <div class="unit-progress-meter" aria-live="polite">
        <strong>{{ completedCount }} / {{ learningUnits.length }}</strong>
        <span>已完成 · {{ progressPercent }}%</span>
        <progress :value="completedCount" :max="learningUnits.length" aria-label="已完成学习单元">
          {{ progressPercent }}%
        </progress>
        <button v-if="completedCount" type="button" class="unit-reset" @click="resetProgress">清空本地进度</button>
      </div>
    </div>

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
        <span>进度</span>
        <select v-model="status">
          <option value="all">全部状态</option>
          <option value="pending">待完成</option>
          <option value="done">已完成</option>
        </select>
      </label>
    </div>

    <p class="unit-result-count" aria-live="polite">显示 {{ filteredUnits.length }} 个单元</p>

    <ol v-if="filteredUnits.length" class="unit-list">
      <li v-for="unit in filteredUnits" :id="`unit-${unit.id}`" :key="unit.id" class="unit-item">
        <label class="unit-check">
          <input v-model="completedIds" type="checkbox" :value="unit.id" />
          <span class="unit-check-box" aria-hidden="true"></span>
          <span class="sr-only">标记“{{ unit.title }}”为已完成</span>
        </label>

        <article>
          <div class="unit-heading-row">
            <div>
              <p class="unit-meta">{{ unit.stageLabel }} · {{ unit.category }} · {{ unit.difficulty }}</p>
              <h3>{{ unit.title }}</h3>
            </div>
            <span v-if="completedSet.has(unit.id)" class="unit-done">已完成</span>
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
            <a v-for="(id, index) in unit.prerequisites" :key="id" :href="`#unit-${id}`">
              {{ prerequisiteTitle(id) }}<span v-if="index < unit.prerequisites.length - 1">、</span>
            </a>
          </p>
          <p v-else class="unit-prerequisites"><strong>前置：</strong>可直接开始</p>

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
