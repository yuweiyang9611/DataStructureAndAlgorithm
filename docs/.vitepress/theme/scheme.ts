import { ref, watch, type Ref } from 'vue'

/**
 * Day and night for the whole family.
 *
 * kit/v1/theme.js owns the choice: it reads `site-theme` before the first paint and sets
 * `data-theme` on <html>. VitePress's own appearance switch is turned off in config.mts, so it
 * never writes a key of its own. Its dark state (the `.dark` class and `useData().isDark`) follows
 * `data-theme`: the inline head script in config.mts keeps `.dark` in step before Vue loads, and
 * `bridgeScheme` keeps `isDark` in step after it, in both directions.
 */
export type Scheme = 'light' | 'dark'

export const storageKey = 'site-theme'
const paper: Record<Scheme, string> = { light: '#f4efe4', dark: '#1c1a17' }

/** Null until the app has mounted, so the server render and hydration agree. */
export const currentScheme = ref<Scheme | null>(null)

function readScheme(): Scheme {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light'
}

export function applyScheme(scheme: Scheme, remember: boolean) {
  const root = document.documentElement
  root.dataset.theme = scheme
  root.classList.toggle('dark', scheme === 'dark')
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', paper[scheme])
  currentScheme.value = scheme
  if (!remember) return
  try {
    localStorage.setItem(storageKey, scheme)
  } catch {
    // The scheme still applies for this visit if storage is unavailable.
  }
}

let bridged = false

export function bridgeScheme(isDark: Ref<boolean>) {
  if (bridged) return
  bridged = true
  const follow = () => {
    const scheme = readScheme()
    currentScheme.value = scheme
    if (isDark.value !== (scheme === 'dark')) isDark.value = scheme === 'dark'
  }
  follow()
  new MutationObserver(follow).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] })
  watch(isDark, (dark) => {
    const scheme: Scheme = dark ? 'dark' : 'light'
    if (readScheme() !== scheme) applyScheme(scheme, false)
  })
}
