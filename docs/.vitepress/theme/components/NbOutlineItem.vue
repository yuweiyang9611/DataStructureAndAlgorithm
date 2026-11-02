<script setup lang="ts">
import NbText from './NbText'

// Replaces VitePress's VPDocOutlineItem (see the alias in config.mts) for the page contents.
// A section number such as "3.4" or "1." gets its own column, so the title wraps under the title
// and the number is never left alone on a line. Titles also get the squeezed marks, so the
// kit's runtime pass has nothing to replace in this client-rendered list.
defineOptions({ name: 'NbOutlineItem' })

interface OutlineHeader {
  title: string
  link: string
  children?: OutlineHeader[]
}

defineProps<{ headers: OutlineHeader[]; root?: boolean }>()

const numbered = /^(\d+\.(?:\d+\.?)*)\s+(\S[\s\S]*)$/

function parts(title: string) {
  const match = title.match(numbered)
  return match ? { number: match[1], text: match[2] } : { number: '', text: title }
}
</script>

<template>
  <ul class="VPDocOutlineItem" :class="root ? 'root' : 'nested'">
    <li v-for="{ children, link, title } in headers" :key="link">
      <a class="outline-link" :class="{ numbered: parts(title).number }" :href="link" :title="title">
        <span v-if="parts(title).number" class="outline-number">{{ parts(title).number }}</span>
        <span class="outline-text"><NbText :text="parts(title).text" /></span>
      </a>
      <NbOutlineItem v-if="children?.length" :headers="children" />
    </li>
  </ul>
</template>
