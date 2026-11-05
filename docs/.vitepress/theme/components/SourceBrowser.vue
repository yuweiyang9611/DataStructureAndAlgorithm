<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useRoute, withBase } from 'vitepress'
import { repository } from '../../data/repository'
import {
  describeEntry, directoryHref, fileName, listDirectory, loadSourceIndex, parseRange, siblingsOf, sourceHref,
  type LineRange, type SourceIndex
} from '../sourceBundle'
import SourcePanel from './SourcePanel.vue'

/**
 * The source page: one bundled file in the panel with its siblings under it, or a directory listing,
 * chosen by `?path=` or `?dir=` on the client. The server render is the root listing's frame; the
 * query is read after mount, and again whenever the router changes it.
 */
const route = useRoute()
const index = ref<SourceIndex | null>(null)
const ready = ref(false)
const error = ref('')
const requestedPath = ref<string | null>(null)
const requestedDir = ref('')
const range = ref<LineRange | null>(null)
const panel = ref<InstanceType<typeof SourcePanel>>()

interface Crumb { text: string; href?: string }

const file = computed(() => requestedPath.value && index.value
  ? index.value.files.find((entry) => entry.path === requestedPath.value) ?? null
  : null)
const missing = computed(() => ready.value && !!index.value && !!requestedPath.value && !file.value)
const listedDir = computed(() => missing.value || file.value ? '' : requestedDir.value)
const entries = computed(() => index.value && !file.value ? listDirectory(index.value.files, listedDir.value) : [])
const siblings = computed(() => file.value && index.value ? siblingsOf(index.value.files, file.value.path) : [])
const revision = computed(() => index.value ? index.value.revision.slice(0, 7) : '')

const crumbs = computed<Crumb[]>(() => {
  const trail: Crumb[] = [{ text: '源码', href: directoryHref('') }]
  const path = file.value ? file.value.path : listedDir.value
  const parts = path.split('/').filter(Boolean)
  parts.forEach((part, position) => {
    const last = position === parts.length - 1
    trail.push(last && file.value ? { text: part } : { text: part, href: directoryHref(parts.slice(0, position + 1).join('/')) })
  })
  return trail
})

const title = computed(() => {
  if (file.value) return fileName(file.value.path)
  if (missing.value) return '没有这个文件'
  if (listedDir.value) return `${fileName(listedDir.value)}/`
  return '源码'
})

const note = computed(() => {
  if (file.value) return `${file.value.path} · ${file.value.lines} 行 · 仓库提交 ${revision.value}`
  if (missing.value) return `${requestedPath.value} 不在站内的源码包里，可能不是课程用到的文件。下面是全部目录。`
  if (listedDir.value) return `${listedDir.value} · 仓库提交 ${revision.value}`
  return '仓库里课程会用到的文件，和这个站点出自同一个提交。从学习单元、Trace 实验室、源码导航或下面的目录进入。'
})

const noscriptHtml = `<p class="source-note">浏览源码需要 JavaScript。也可以在 <a href="${repository}">GitHub</a> 上打开仓库。</p>`

function readLocation() {
  const url = new URL(window.location.href)
  requestedPath.value = url.searchParams.get('path')?.replace(/^\/+/, '') ?? null
  requestedDir.value = (url.searchParams.get('dir') ?? '').replace(/^\/+|\/+$/g, '')
  range.value = parseRange(url.hash)
}

function scrollToRange() {
  if (range.value) {
    const line = range.value[0]
    void nextTick(() => panel.value?.scrollToLine(line))
  }
}

onMounted(async () => {
  readLocation()
  try {
    index.value = await loadSourceIndex(withBase('/source/'))
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : String(cause)
  } finally {
    ready.value = true
  }
})

// The router keeps query and hash in step on same-page links and on back and forward.
watch(() => [route.query, route.hash], () => { if (ready.value) readLocation() })
watch(title, (value) => { if (ready.value) document.title = `${value} · 源码 · 算法研习室` }, { flush: 'post' })
</script>

<template>
  <section class="source-browser" :data-ready="ready ? 'true' : 'false'" aria-labelledby="source-title">
    <p class="source-crumbs" aria-label="位置">
      <template v-for="(crumb, position) in crumbs" :key="position">
        <span v-if="position" class="source-crumb-rule" aria-hidden="true">/</span>
        <a v-if="crumb.href" :href="withBase(crumb.href)">{{ crumb.text }}</a>
        <span v-else class="source-crumb-here">{{ crumb.text }}</span>
      </template>
    </p>
    <h1 id="source-title" class="source-title">{{ title }}</h1>
    <p class="source-note">{{ note }}</p>
    <p v-if="error" class="source-error" role="alert">{{ error }}</p>

    <section v-if="!file" class="source-tree" aria-label="目录">
      <ol v-if="entries.length" class="source-rows">
        <li v-for="entry in entries" :key="entry.path" class="nb-entry source-row">
          <a :href="withBase(entry.directory ? directoryHref(entry.path) : sourceHref(entry.path))">{{ entry.name }}{{ entry.directory ? '/' : '' }}</a>
          <span>{{ describeEntry(entry) }}</span>
        </li>
      </ol>
      <p v-else-if="ready && index" class="source-empty">没有这个目录。</p>
    </section>

    <SourcePanel
      v-if="file && index"
      ref="panel"
      :key="file.path"
      class="source-page-panel"
      :files="[{ path: file.path }]"
      :marked="range ? [range] : []"
      :blob-base="index.blobBase"
      anchors
      @select="scrollToRange"
    />

    <section v-if="siblings.length" class="source-siblings" aria-label="同目录文件">
      <h2 class="source-siblings-title">同目录</h2>
      <ul>
        <li v-for="sibling in siblings" :key="sibling.path"><a :href="withBase(sourceHref(sibling.path))">{{ fileName(sibling.path) }}</a></li>
      </ul>
    </section>

    <p class="source-foot">文件在构建时从仓库复制，行号与仓库一致；要看历史和 blame，用面板里的 GitHub 链接。</p>
    <noscript v-html="noscriptHtml"></noscript>
  </section>
</template>
