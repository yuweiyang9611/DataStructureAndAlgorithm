<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { withBase } from 'vitepress'

interface TraceEvent {
  step: number
  algorithm: string
  operation: string
  description: string
  state: Record<string, string>
  schemaVersion: number
}

interface TraceDefinition {
  id: string
  title: string
  category: string
  level: string
  summary: string
  eventCount: number
  algorithm: string
  source: string
  command: string
  stateHints: { sequence: string[]; range: string[]; graph: string[] }
  inputMetadata?: Record<string, unknown>
  file: string
}

interface TraceManifest {
  schemaVersion: number
  traces: TraceDefinition[]
}

interface TracePayload {
  demo?: string
  scenario?: string
  result: unknown
  trace: TraceEvent[]
}

const repository = 'https://github.com/yuweiyang9611/DataStructureAndAlgorithm'
const manifest = ref<TraceManifest | null>(null)
const payload = ref<TracePayload | null>(null)
const selectedId = ref('')
const selectedOperation = ref('all')
const eventIndex = ref(0)
const loading = ref(true)
const error = ref('')
const playing = ref(false)
const copyStatus = ref<'idle' | 'success' | 'error'>('idle')
let playTimer: number | undefined
let traceRequest: AbortController | undefined
let manifestRequest: AbortController | undefined
let requestSequence = 0
let disposed = false

const selectedTrace = computed(() => manifest.value?.traces.find((trace) => trace.id === selectedId.value) ?? null)
const operationOptions = computed(() => [...new Set((payload.value?.trace ?? []).map((event) => event.operation))])
const visibleEvents = computed(() => {
  const events = payload.value?.trace ?? []
  return selectedOperation.value === 'all'
    ? events
    : events.filter((event) => event.operation === selectedOperation.value)
})
const currentEvent = computed(() => visibleEvents.value[eventIndex.value] ?? null)
const previousEvent = computed(() => {
  const event = currentEvent.value
  if (!event || !payload.value || event.step <= 1) return null
  return payload.value.trace[event.step - 2] ?? null
})
const progressLabel = computed(() => visibleEvents.value.length
  ? `${eventIndex.value + 1} / ${visibleEvents.value.length}`
  : '0 / 0')

const stateRows = computed(() => {
  const current = currentEvent.value?.state ?? {}
  const previous = previousEvent.value?.state ?? {}
  const keys = [...new Set([...Object.keys(current), ...Object.keys(previous)])].sort()

  return keys.map((key) => {
    const hasCurrent = Object.hasOwn(current, key)
    const hasPrevious = Object.hasOwn(previous, key)
    const currentValue = hasCurrent ? current[key] : ''
    const previousValue = hasPrevious ? previous[key] : ''
    let status: 'added' | 'changed' | 'unchanged' | 'not-provided'
    if (!hasCurrent) status = 'not-provided'
    else if (!hasPrevious) status = 'added'
    else if (currentValue !== previousValue) status = 'changed'
    else status = 'unchanged'
    return { key, currentValue, previousValue, status }
  })
})

function isSequence(key: string) {
  return selectedTrace.value?.stateHints.sequence.includes(key) ?? false
}

function splitSequence(key: string, value: string) {
  if (!value) return []
  return key === 'values'
    ? value.split(',').map((item) => item.trim())
    : value.split(/\s*->\s*/).filter(Boolean)
}

function itemChanged(key: string, value: string, previousValue: string, index: number) {
  const currentItems = splitSequence(key, value)
  const previousItems = splitSequence(key, previousValue)
  return previousItems[index] !== currentItems[index]
}

function sourceUrl(path: string) {
  return `${repository}/blob/main/${path.split('/').map(encodeURIComponent).join('/')}`
}

function stop() {
  playing.value = false
  if (playTimer !== undefined) window.clearInterval(playTimer)
  playTimer = undefined
}

function previous() {
  stop()
  eventIndex.value = Math.max(0, eventIndex.value - 1)
}

function next() {
  stop()
  eventIndex.value = Math.min(Math.max(0, visibleEvents.value.length - 1), eventIndex.value + 1)
}

function togglePlay() {
  if (playing.value) {
    stop()
    return
  }

  if (eventIndex.value >= visibleEvents.value.length - 1) eventIndex.value = 0
  playing.value = true
  playTimer = window.setInterval(() => {
    if (eventIndex.value >= visibleEvents.value.length - 1) {
      stop()
    } else {
      eventIndex.value += 1
    }
  }, 1250)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.target !== event.currentTarget) return
  if (event.key === 'ArrowLeft') {
    event.preventDefault()
    previous()
  } else if (event.key === 'ArrowRight') {
    event.preventDefault()
    next()
  } else if (event.key === ' ') {
    event.preventDefault()
    togglePlay()
  }
}

async function copyCommand() {
  if (!selectedTrace.value) return
  copyStatus.value = 'idle'
  try {
    await navigator.clipboard.writeText(selectedTrace.value.command)
    copyStatus.value = 'success'
  } catch {
    copyStatus.value = 'error'
  }
  window.setTimeout(() => { copyStatus.value = 'idle' }, 1600)
}

async function loadTrace(id: string) {
  const definition = manifest.value?.traces.find((trace) => trace.id === id)
  if (!definition) return
  const sequence = ++requestSequence
  traceRequest?.abort()
  const controller = new AbortController()
  traceRequest = controller
  stop()
  loading.value = true
  error.value = ''
  payload.value = null
  copyStatus.value = 'idle'
  selectedOperation.value = 'all'
  eventIndex.value = 0

  try {
    const response = await fetch(withBase(`/traces/${definition.file}`), { signal: controller.signal })
    if (!response.ok) throw new Error(`HTTP ${response.status}`)
    const nextPayload = await response.json() as TracePayload
    if (!Array.isArray(nextPayload.trace) || nextPayload.trace.some((event) => event.schemaVersion !== 1)) {
      throw new Error('此页面只支持 Trace schemaVersion 1。')
    }
    if (disposed || sequence !== requestSequence) return
    payload.value = nextPayload
    const url = new URL(window.location.href)
    url.searchParams.set('trace', id)
    window.history.replaceState({}, '', url)
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') return
    if (disposed || sequence !== requestSequence) return
    payload.value = null
    error.value = cause instanceof Error ? `Trace 加载失败：${cause.message}` : 'Trace 加载失败。'
  } finally {
    if (!disposed && sequence === requestSequence) loading.value = false
  }
}

onMounted(async () => {
  manifestRequest = new AbortController()
  try {
    const response = await fetch(withBase('/traces/manifest.json'), { signal: manifestRequest.signal })
    if (!response.ok) throw new Error(`HTTP ${response.status}`)
    const nextManifest = await response.json() as TraceManifest
    if (nextManifest.schemaVersion !== 1 || !Array.isArray(nextManifest.traces)) {
      throw new Error('不支持的 Trace manifest。')
    }
    if (disposed) return
    manifest.value = nextManifest
    const requested = new URLSearchParams(window.location.search).get('trace')
    selectedId.value = nextManifest.traces.some((trace) => trace.id === requested)
      ? requested!
      : nextManifest.traces[0]?.id ?? ''
    await loadTrace(selectedId.value)
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') return
    if (disposed) return
    error.value = cause instanceof Error ? `Trace 清单加载失败：${cause.message}` : 'Trace 清单加载失败。'
    loading.value = false
  }
})

watch(selectedId, (id, oldId) => {
  if (oldId && id !== oldId) void loadTrace(id)
})

watch(selectedOperation, () => {
  stop()
  eventIndex.value = 0
})

onBeforeUnmount(() => {
  disposed = true
  requestSequence += 1
  manifestRequest?.abort()
  traceRequest?.abort()
  stop()
})
</script>

<template>
  <section class="trace-lab" tabindex="0" aria-label="算法 Trace 播放器" @keydown="handleKeydown">
    <div class="trace-selector">
      <label>
        <span>选择场景</span>
        <select v-model="selectedId" :disabled="!manifest">
          <option v-for="trace in manifest?.traces ?? []" :key="trace.id" :value="trace.id">
            {{ trace.title }} · {{ trace.eventCount }} 步
          </option>
        </select>
      </label>
      <label>
        <span>阶段筛选</span>
        <select v-model="selectedOperation" :disabled="loading || !payload">
          <option value="all">全部操作</option>
          <option v-for="operation in operationOptions" :key="operation" :value="operation">{{ operation }}</option>
        </select>
      </label>
    </div>

    <div v-if="selectedTrace" class="trace-overview">
      <div>
        <p class="trace-eyebrow">{{ selectedTrace.category }} · {{ selectedTrace.level }} · SCHEMA V1</p>
        <h2>{{ selectedTrace.title }}</h2>
        <p>{{ selectedTrace.summary }}</p>
      </div>
      <div class="trace-overview-links">
        <a :href="sourceUrl(selectedTrace.source)">查看实现</a>
        <button type="button" @click="copyCommand">{{ copyStatus === 'success' ? '命令已复制' : copyStatus === 'error' ? '复制失败' : '复制运行命令' }}</button>
      </div>
    </div>

    <p v-if="loading" class="trace-message" aria-live="polite">正在加载 Trace…</p>
    <p v-else-if="error" class="trace-message trace-error" role="alert">{{ error }}</p>

    <template v-else-if="currentEvent">
      <div class="trace-controls">
        <button type="button" :disabled="eventIndex === 0" aria-label="上一步" @click="previous">← 上一步</button>
        <button type="button" class="trace-play" @click="togglePlay">{{ playing ? '暂停' : '播放' }}</button>
        <button type="button" :disabled="eventIndex >= visibleEvents.length - 1" aria-label="下一步" @click="next">下一步 →</button>
        <label class="trace-scrubber">
          <span>{{ progressLabel }}</span>
          <input v-model.number="eventIndex" type="range" min="0" :max="Math.max(0, visibleEvents.length - 1)" step="1" aria-label="选择 Trace 步骤" @input="stop" />
        </label>
      </div>

      <article class="trace-event">
        <p class="sr-only" aria-live="polite" aria-atomic="true">第 {{ currentEvent.step }} 步：{{ currentEvent.description }}</p>
        <header>
          <span class="trace-step">STEP {{ currentEvent.step }}</span>
          <span class="trace-operation">{{ currentEvent.operation }}</span>
          <h3>{{ currentEvent.description }}</h3>
        </header>

        <div v-if="stateRows.length" class="trace-state">
          <div v-for="row in stateRows" :key="row.key" class="trace-state-row" :class="`is-${row.status}`">
            <div class="trace-state-key">
              <code>{{ row.key }}</code>
              <span>{{ row.status === 'added' ? '新增' : row.status === 'changed' ? '改变' : row.status === 'unchanged' ? '未变' : '本步未提供' }}</span>
            </div>
            <ol v-if="isSequence(row.key) && row.currentValue" class="trace-sequence">
              <li v-for="(item, index) in splitSequence(row.key, row.currentValue)" :key="`${item}-${index}`" :class="{ changed: itemChanged(row.key, row.currentValue, row.previousValue, index) }">{{ item }}</li>
            </ol>
            <div v-else class="trace-state-value">
              <strong>{{ row.currentValue || '—' }}</strong>
              <small v-if="row.status === 'changed'">上一步：{{ row.previousValue }}</small>
            </div>
          </div>
        </div>
        <div v-else class="trace-empty-state">此事件没有状态字段；这不是加载错误。</div>
      </article>

      <p class="trace-contract-note">状态是每一步的局部快照；某个键缺失表示“本步未提供”，不表示该状态已删除。</p>

      <details class="trace-raw">
        <summary>查看当前事件原始 JSON</summary>
        <pre><code>{{ JSON.stringify(currentEvent, null, 2) }}</code></pre>
      </details>
      <details class="trace-raw">
        <summary>查看场景结果 JSON</summary>
        <pre><code>{{ JSON.stringify(payload?.result, null, 2) }}</code></pre>
      </details>
    </template>

    <div v-else-if="!loading && !error" class="trace-message">当前筛选没有事件。</div>
  </section>
</template>
