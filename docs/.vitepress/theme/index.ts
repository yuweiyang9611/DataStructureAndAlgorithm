import DefaultTheme from 'vitepress/theme'
import LearningUnitExplorer from './components/LearningUnitExplorer.vue'
import TracePlayer from './components/TracePlayer.vue'
import './custom.css'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('LearningUnitExplorer', LearningUnitExplorer)
    app.component('TracePlayer', TracePlayer)
  }
}
