/**
 * The touching-marks pass of kit/v1/theme.js, done before Vue sees the text.
 *
 * theme.js wraps the first of two touching full-width marks in `.nb-halt` by replacing text nodes.
 * On this VitePress site that would change server-rendered text before hydration and leave Vue
 * holding detached nodes afterwards, so Markdown and components emit the same spans themselves.
 * The runtime pass then finds nothing to change. Keep the pattern identical to theme.js.
 */
export const looseMarks = /[”’](?=[，。、；：？！])|[，。、；：？！](?=[“‘])/g

export interface TextPart {
  text: string
  halt: boolean
}

export function haltParts(text: string): TextPart[] {
  const parts: TextPart[] = []
  let start = 0
  for (const match of text.matchAll(looseMarks)) {
    if (match.index > start) parts.push({ text: text.slice(start, match.index), halt: false })
    parts.push({ text: match[0], halt: true })
    start = match.index + 1
  }
  if (start < text.length) parts.push({ text: text.slice(start), halt: false })
  return parts
}

/** For HTML that is text only, such as the output of markdown-it's `text` rule. */
export function haltHtml(html: string): string {
  return html.replace(looseMarks, (mark) => `<span class="nb-halt">${mark}</span>`)
}

/** Counts pairs the runtime pass would still find in one string. */
export function countLoose(text: string): number {
  return [...text.matchAll(looseMarks)].length
}
