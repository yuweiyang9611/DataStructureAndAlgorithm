<script setup lang="ts">
import { computed, onMounted, ref, shallowRef } from 'vue'
import { useData } from 'vitepress'
import { emptyStore, type ProgressStore } from '../../data/progress.ts'
import { isMastered, orderedUnits, stepsDone, unitSteps } from '../../data/record.ts'
import { statusOf } from '../../data/progress.ts'
import { hueOf, stageWords, trackOf, trackWords } from '../hues.ts'
import { readRecord } from '../readRecord.ts'

/**
 * The thin filled band at the top of a guide page that teaches one or more learning units: the
 * track's hue, an eyebrow naming the stage and the units, and a bar of how far those units have
 * gone on this computer's record. A page that teaches no unit gets no band.
 */
const { page } = useData()
const route = computed(() => `/${page.value.relativePath.replace(/\.md$/, '').replace(/\/index$/, '/')}`)
const units = computed(() => orderedUnits.filter((unit) => unit.guide === route.value))
const track = computed(() => units.value[0] ? trackOf(units.value[0]) : undefined)
const hue = computed(() => units.value[0] ? hueOf(units.value[0]) : 'ink')

const store = shallowRef<ProgressStore>(emptyStore())
const now = ref(0)
const ready = ref(false)
onMounted(async () => {
  if (!units.value.length) return
  const record = await readRecord()
  now.value = Date.now()
  store.value = record
  ready.value = true
})

const stages = computed(() => [...new Set(units.value.map((unit) => stageWords[unit.stage] ?? unit.stage))].join('、'))
const done = computed(() => units.value.reduce((sum, unit) => sum + stepsDone(store.value.units[unit.id], now.value), 0))
const total = computed(() => units.value.length * unitSteps)
const verified = computed(() => units.value.filter((unit) => isMastered(statusOf(store.value.units[unit.id], now.value))).length)
const fraction = computed(() => total.value ? done.value / total.value : 0)
const percent = computed(() => Math.round(fraction.value * 100))
</script>

<template>
  <div v-if="units.length" class="guide-band" :data-hue="hue" :data-ready="ready">
    <p class="guide-band-eyebrow">{{ track ? trackWords[track] : '' }} · {{ stages }} · {{ units.length }} 个学习单元</p>
    <div class="guide-band-progress">
      <p>已验证 <strong>{{ verified }} / {{ units.length }}</strong> · 完成 {{ percent }}%</p>
      <div class="nb-bar nb-bar--on-fill" role="progressbar" aria-label="本页学习单元的进度" aria-valuemin="0" :aria-valuemax="total" :aria-valuenow="done" :style="{ '--nb-fraction': fraction }"><span /></div>
    </div>
  </div>
</template>
