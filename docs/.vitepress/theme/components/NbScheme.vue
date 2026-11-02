<script setup lang="ts">
import { applyScheme, currentScheme, type Scheme } from '../scheme'

// The same two words and markup that kit/v1/theme.js builds after `.nb-nav`. Vue renders them
// here because theme.js would insert its own group into the server-rendered head before hydration.
const schemes: [Scheme, string][] = [['light', '白天'], ['dark', '晚上']]
</script>

<template>
  <div class="nb-theme" role="group" aria-label="配色方案">
    <button
      v-for="[scheme, word] in schemes"
      :key="scheme"
      type="button"
      :data-scheme="scheme"
      :aria-pressed="currentScheme === scheme ? 'true' : 'false'"
      @click="applyScheme(scheme, true)"
    >{{ word }}</button>
  </div>
</template>
