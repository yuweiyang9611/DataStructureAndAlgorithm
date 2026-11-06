<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { withBase } from 'vitepress'
import { blobBase as repositoryBlobBase } from '../../data/repository'
import { highlightLines, languageFor, type CodeToken } from '../codeColors'
import { runDemo, type RunHandle } from '../demoRunner'
import { blobUrl, fileName, loadSourceText, type LineRange } from '../sourceBundle'
import CodeLines from './CodeLines'

/**
 * The source panel: a bundled file read in place in the kit's `.nb-code` form. A port of the family's
 * source-panel.js. The tabs render on the server; the text is fetched on the client once the panel
 * is shown (`deferred` holds it back until, say, a details element opens). A `<noscript>` inside
 * links the same files on GitHub.
 *
 * With `demo` (the bundle path of one of the files), that tab can be run in the browser: 运行
 * compiles and executes it on the course's .NET runtime, 改一改再跑 swaps the view for a plain
 * textarea (`.nb-code-editor`) holding a copy the reader can change, 恢复原文 puts the file back,
 * and the output sits under the actions as `.nb-code-output`. The draft lives only on the page.
 */
export interface SourcePanelFile {
  path: string
  label?: string
}

const props = withDefaults(defineProps<{
  files: SourcePanelFile[]
  active?: string
  marked?: LineRange[]
  deferred?: boolean
  anchors?: boolean
  blobBase?: string
  demo?: string
}>(), {
  active: undefined,
  marked: () => [],
  deferred: false,
  anchors: false,
  blobBase: repositoryBlobBase,
  demo: undefined
})

const emit = defineEmits<{ select: [path: string, text: string]; ran: [path: string] }>()

const current = ref(props.active ?? props.files[0]?.path ?? '')
const lines = ref<CodeToken[][]>([])
const note = ref('')
const status = ref('')
const mounted = ref(false)
const view = ref<HTMLElement>()
let shown = ''
let sequence = 0

const githubUrl = computed(() => current.value ? blobUrl(props.blobBase, current.value) : '')
const labelOf = (file: SourcePanelFile) => file.label ?? fileName(file.path)
const escape = (value: string) => value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
const noscriptHtml = computed(() =>
  `<p class="nb-code-noscript">没有脚本时在 GitHub 打开：${props.files.map((file) => `<a href="${escape(blobUrl(props.blobBase, file.path))}">${escape(labelOf(file))}</a>`).join('、')}</p>`)

// The runnable tab.
const onDemo = computed(() => Boolean(props.demo) && current.value === props.demo)
const editing = ref(false)
const draft = ref('')
const running = ref(false)
const runState = ref<'' | 'running' | 'done' | 'wrong'>('')
const runLabel = ref('')
const runText = ref('')
let original: string | null = null
let handle: RunHandle | null = null

async function show(path: string) {
  if (!path) return
  const request = ++sequence
  status.value = ''
  try {
    const text = await loadSourceText(withBase('/source/'), path)
    if (request !== sequence) return
    lines.value = highlightLines(text, languageFor(path))
    note.value = `${lines.value.length} 行`
    shown = path
    emit('select', path, text)
  } catch (error) {
    if (request !== sequence) return
    lines.value = []
    note.value = ''
    shown = ''
    status.value = `${error instanceof Error ? error.message : String(error)}。可以在 GitHub 打开这个文件。`
  }
}

function select(path: string) {
  if (props.files.some((file) => file.path === path)) current.value = path
}

async function copy() {
  if (!current.value) return
  try {
    const text = onDemo.value && editing.value ? draft.value : await loadSourceText(withBase('/source/'), current.value)
    await navigator.clipboard.writeText(text)
    status.value = `已复制 ${fileName(current.value)}`
  } catch {
    status.value = '无法访问剪贴板，请手动选中复制。'
  }
}

/** Scrolls the view, not the page, so the heading above the panel stays where the reader left it. */
function scrollToLine(line: number) {
  const pre = view.value
  const target = pre?.querySelector<HTMLElement>(`[data-line="${line}"]`)
  if (!pre || !target) return
  const offset = target.getBoundingClientRect().top - pre.getBoundingClientRect().top + pre.scrollTop
  pre.scrollTop = Math.max(0, offset - pre.clientHeight / 2 + target.offsetHeight / 2)
}

async function demoText() {
  if (!props.demo) return ''
  original ??= await loadSourceText(withBase('/source/'), props.demo)
  return original
}

async function edit() {
  if (!props.demo) return
  const text = await demoText()
  if (!editing.value) draft.value = text
  select(props.demo)
  editing.value = true
  status.value = '这是副本，改完点运行；原文件不会变。'
}

async function reset() {
  draft.value = await demoText()
  editing.value = false
  if (props.demo) select(props.demo)
  status.value = '已恢复原文。'
}

function showOutput(state: 'running' | 'done' | 'wrong', label: string, text: string) {
  runState.value = state
  runLabel.value = label
  runText.value = text
}

async function run() {
  if (!props.demo || handle) return
  const source = editing.value ? draft.value : await demoText()
  running.value = true
  showOutput('running', '运行中', '')
  handle = runDemo({
    source,
    workerUrl: withBase('/compiler-worker.js'),
    onStage: (_name, label) => { status.value = `${label}中…`; runLabel.value = `${label}中` }
  })
  const result = await handle.done
  handle = null
  running.value = false
  const diagnostics = result.diagnostics.length ? result.diagnostics.join('\n') : ''
  if (result.ok) {
    showOutput('done', `运行结果 · 浏览器内 .NET${editing.value ? ' · 已改动的副本' : ''}`, result.output.trimEnd() || '（没有输出）')
    status.value = '运行完成。'
    emit('ran', props.demo)
  } else {
    const label = result.timedOut ? '运行超时' : result.stopped ? '已停止' : result.stage === 'compile' ? '编译失败' : '运行失败'
    showOutput('wrong', label, [result.output.trimEnd(), diagnostics, result.error].filter(Boolean).join('\n'))
    status.value = result.timedOut ? '超时了。可以改代码后重新运行。' : result.stage === 'compile' ? '编译失败，看诊断里的行号。' : result.error ?? ''
  }
}

function stop() { handle?.stop() }

watch(() => props.active, (active) => { if (active) select(active) })
watch(() => props.files, (files) => { if (!files.some((file) => file.path === current.value)) current.value = files[0]?.path ?? '' })
watch(current, (path) => { if (mounted.value && !props.deferred) void show(path) })
watch(() => props.deferred, (deferred) => { if (mounted.value && !deferred && shown !== current.value) void show(current.value) })

onMounted(() => {
  mounted.value = true
  if (!props.deferred) void show(current.value)
  window.addEventListener('pagehide', stop)
})
onBeforeUnmount(() => {
  window.removeEventListener('pagehide', stop)
  stop()
})

defineExpose({ select, scrollToLine, run })
</script>

<template>
  <div class="nb-code source-panel" :data-shown="mounted && note ? '' : undefined" :data-demo="demo ? '' : undefined">
    <div class="nb-code-tabs" role="tablist" aria-label="文件">
      <button
        v-for="file in files"
        :key="file.path"
        type="button"
        class="nb-code-tab"
        role="tab"
        :aria-selected="file.path === current ? 'true' : 'false'"
        :data-path="file.path"
        :title="file.path"
        @click="select(file.path)"
      >{{ labelOf(file) }}</button>
      <span class="nb-code-note">{{ note }}</span>
    </div>
    <pre v-show="!(onDemo && editing)" ref="view" class="nb-code-view" tabindex="0" role="tabpanel" :aria-label="current ? fileName(current) : '源码'"><CodeLines :lines="lines" :marked="marked" :anchors="anchors" /></pre>
    <textarea
      v-if="demo"
      v-show="onDemo && editing"
      v-model="draft"
      class="nb-code-editor"
      spellcheck="false"
      :aria-label="`${fileName(demo)} 的可编辑副本`"
    ></textarea>
    <div class="nb-code-actions">
      <template v-if="onDemo">
        <button type="button" class="nb-button nb-button--solid" :disabled="running" @click="run">运行</button>
        <button v-if="!editing" type="button" class="nb-button" @click="edit">改一改再跑</button>
        <button v-else type="button" class="nb-button" @click="reset">恢复原文</button>
        <button v-if="running" type="button" class="nb-button" @click="stop">停止</button>
      </template>
      <button type="button" class="nb-button" @click="copy">复制</button>
      <a v-if="mounted && githubUrl" class="nb-code-link" :href="githubUrl">在 GitHub 打开</a>
      <p class="nb-code-status" role="status" aria-live="polite">{{ status }}</p>
    </div>
    <div v-if="runState" class="nb-code-output" :data-state="runState">
      <p class="nb-code-output-label">{{ runLabel }}</p>
      <pre tabindex="0">{{ runText }}</pre>
    </div>
    <noscript v-html="noscriptHtml"></noscript>
  </div>
</template>
