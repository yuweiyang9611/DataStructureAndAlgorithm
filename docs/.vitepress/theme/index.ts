import { h, onMounted } from 'vue'
import { useData, type Theme } from 'vitepress'
// The kit's fonts come from the family's /kit/v1/fonts.css, so VitePress's own Inter is left out.
import DefaultTheme from 'vitepress/theme-without-fonts'
import CourseHome from './components/CourseHome.vue'
import LearningUnitExplorer from './components/LearningUnitExplorer.vue'
import NbGuideBand from './components/NbGuideBand.vue'
import NbScheme from './components/NbScheme.vue'
import TracePlayer from './components/TracePlayer.vue'
import { bridgeScheme } from './scheme'
// Kit first, then the site's own sheet, as on every site in the family.
import './notebook.css'
import './custom.css'

export default {
  extends: DefaultTheme,
  Layout: () => h(DefaultTheme.Layout, null, {
    'nav-bar-content-after': () => h(NbScheme),
    // A guide page that teaches learning units opens with a band in its track's hue.
    'doc-before': () => h(NbGuideBand)
  }),
  enhanceApp({ app }) {
    app.component('CourseHome', CourseHome)
    app.component('LearningUnitExplorer', LearningUnitExplorer)
    app.component('TracePlayer', TracePlayer)
  },
  setup() {
    const { isDark } = useData()
    // After hydration, so the server render and the first client render agree.
    onMounted(() => bridgeScheme(isDark))
  }
} satisfies Theme
