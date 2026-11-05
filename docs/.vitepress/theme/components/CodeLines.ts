import { h, type FunctionalComponent } from 'vue'
import type { CodeToken } from '../codeColors'
import { inRange, type LineRange } from '../sourceBundle'

/**
 * The lines of the kit's code panel: one `div.nb-code-line[data-line]` per line with the coloured text
 * in ONE `span.nb-code-text`. The line is a two-column grid (number, text), so the tokens must share
 * that span or each becomes a cell. Rendered from data so no template whitespace can land inside `pre`.
 */
const CodeLines: FunctionalComponent<{ lines: CodeToken[][]; marked?: LineRange[]; anchors?: boolean }> = (props) =>
  h('code', props.lines.map((tokens, index) => {
    const number = index + 1
    return h('div', {
      key: number,
      class: 'nb-code-line',
      'data-line': String(number),
      id: props.anchors ? `L${number}` : undefined,
      'data-marked': props.marked && inRange(number, props.marked) ? '' : undefined
    }, [h('span', { class: 'nb-code-text' }, tokens.map((token) => token.kind ? h('span', { class: `tok-${token.kind}` }, token.text) : token.text))])
  }))

CodeLines.props = {
  lines: { type: Array, required: true },
  marked: { type: Array, required: false },
  anchors: { type: Boolean, required: false }
}
CodeLines.displayName = 'CodeLines'

export default CodeLines
