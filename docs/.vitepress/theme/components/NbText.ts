import { h, type FunctionalComponent } from 'vue'
import { haltParts } from '../../data/halt.ts'

/**
 * Renders a string with touching marks already squeezed, so kit/v1/theme.js never has to replace
 * a text node that Vue owns. Use it for any text from data that could hold ”， or ，“.
 */
const NbText: FunctionalComponent<{ text?: string | number | null }> = (props) =>
  haltParts(String(props.text ?? '')).map((part) => part.halt ? h('span', { class: 'nb-halt' }, part.text) : part.text)

NbText.props = { text: { type: [String, Number], required: false } }
NbText.displayName = 'NbText'

export default NbText
