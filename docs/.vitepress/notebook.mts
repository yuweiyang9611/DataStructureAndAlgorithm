import type { MarkdownOptions, MarkdownRenderer } from 'vitepress'
import { haltHtml } from './data/halt.ts'

/**
 * Engine glue for the family's kit (kit/v1): the head scripts, the Markdown output and the code
 * colours that the kit expects from a page, done in VitePress's own build steps.
 */

const base = '/DataStructureAndAlgorithm/'

/**
 * VitePress's `.dark` class follows the kit's `data-theme`, and `data-theme` follows `.dark` when
 * only the class was changed. data-theme wins when both change together. Runs before the first
 * paint, right after theme.js has applied the saved or system scheme.
 */
const darkBridge = `(() => {
  const root = document.documentElement;
  const paper = { light: "#f4efe4", dark: "#1c1a17" };
  const follow = (records) => {
    const dark = root.dataset.theme === "dark";
    if (!records || records.some((record) => record.attributeName === "data-theme")) {
      if (root.classList.contains("dark") !== dark) root.classList.toggle("dark", dark);
      return;
    }
    const classDark = root.classList.contains("dark");
    if (classDark === dark) return;
    const theme = classDark ? "dark" : "light";
    root.dataset.theme = theme;
    document.querySelector('meta[name="theme-color"]')?.setAttribute("content", paper[theme]);
  };
  follow();
  new MutationObserver(follow).observe(root, { attributes: true, attributeFilter: ["data-theme", "class"] });
})();`

/**
 * These tags sit outside VitePress's head manager on purpose: it re-creates the head tags it
 * knows about on every route change, which would reset theme-color and run the scripts again.
 */
export const kitHead = [
  '<meta name="theme-color" content="#f4efe4">',
  `<script src="${base}kit/theme.js"></script>`,
  `<script>${darkBridge}</script>`
].join('\n    ')

export function injectKitHead(html: string) {
  return html.includes(`${base}kit/theme.js`) ? html : html.replace(/<meta charset="utf-8">/i, (charset) => `${charset}\n    ${kitHead}`)
}

/** Markdown output that matches the kit: squeezed touching marks and scrollable tables. */
export function notebookMarkdown(md: MarkdownRenderer) {
  const text = md.renderer.rules.text!
  // Rendering only: token content, and with it heading ids, page titles and the search index, stays plain.
  md.renderer.rules.text = (tokens, index, options, env, self) => haltHtml(text(tokens, index, options, env, self))

  md.renderer.rules.table_open = (tokens, index, options, _env, self) =>
    `<div class="table-wrap" tabindex="0">${self.renderToken(tokens, index, options)}`
  md.renderer.rules.table_close = (tokens, index, options, _env, self) =>
    `${self.renderToken(tokens, index, options)}</div>`
}

/** The kit's `.tok-*` assignments as a Shiki theme. Colours are kit tokens, so both schemes follow data-theme. */
export const notebookCodeTheme: NonNullable<MarkdownOptions['theme']> = {
  name: 'notebook',
  type: 'light',
  colors: {
    'editor.foreground': 'var(--nb-ink)',
    'editor.background': 'var(--nb-sheet)'
  },
  tokenColors: [
    { settings: { foreground: 'var(--nb-ink)' } },
    { scope: ['comment', 'punctuation.definition.comment'], settings: { foreground: 'var(--nb-ink-soft)' } },
    { scope: ['meta.preprocessor', 'keyword.preprocessor', 'meta.preprocessor string', 'punctuation.separator.hash'], settings: { foreground: 'var(--nb-ink-soft)' } },
    {
      scope: ['keyword', 'storage', 'storage.type', 'storage.modifier', 'keyword.type', 'constant.language', 'variable.language', 'keyword.operator.expression', 'keyword.operator.new'],
      settings: { foreground: 'var(--nb-purple)' }
    },
    { scope: ['keyword.operator', 'punctuation'], settings: { foreground: 'var(--nb-ink)' } },
    { scope: ['constant.numeric'], settings: { foreground: 'var(--nb-purple)' } },
    {
      scope: ['entity.name.type', 'entity.name.class', 'entity.name.interface', 'entity.name.struct', 'entity.other.inherited-class', 'support.type', 'support.class'],
      settings: { foreground: 'var(--nb-cyan)' }
    },
    { scope: ['entity.name.function', 'support.function', 'meta.function-call.generic'], settings: { foreground: 'var(--nb-blue)' } },
    { scope: ['string', 'punctuation.definition.string', 'constant.character.escape'], settings: { foreground: 'var(--nb-green)' } }
  ]
}

// The shell grammars do not know `dotnet` and its switches, so commands are coloured the way
// kit-era code-colors.js does it: the program is the type, switches are keywords.
const shellRules = {
  comment: String.raw`(?<!\S)#[^\n]*`,
  string: String.raw`"[^"\n]*"|'[^'\n]*'`,
  type: String.raw`^[ \t]*(?:[.&][ \t]+)?[A-Za-z_./\\][\w./\\:-]*(?![\w./\\:-]|[ \t]*=)`,
  keyword: String.raw`(?<=\s)--?[A-Za-z][\w-]*`,
  meta: String.raw`\$[A-Za-z_][\w:]*`,
  number: String.raw`(?<![\w.-])\d+(?:\.\d+)?\b`
}
const shellPattern = new RegExp(Object.entries(shellRules).map(([name, source]) => `(?<${name}>${source})`).join('|'), 'g')
const shellColors: Record<string, string> = {
  comment: 'var(--nb-ink-soft)',
  string: 'var(--nb-green)',
  type: 'var(--nb-cyan)',
  keyword: 'var(--nb-purple)',
  meta: 'var(--nb-ink-soft)',
  number: 'var(--nb-purple)'
}
const shellLanguages = new Set(['powershell', 'ps1', 'pwsh', 'bash', 'sh', 'shell', 'bat', 'cmd'])

export const notebookShellColors: NonNullable<MarkdownOptions['codeTransformers']>[number] = {
  name: 'notebook:shell-colors',
  tokens(lines) {
    if (!shellLanguages.has(this.options.lang)) return
    return lines.map((line) => {
      const text = line.map((token) => token.content).join('')
      const offset = line[0]?.offset ?? 0
      const tokens: typeof line = []
      let position = 0
      const push = (end: number, color: string) => {
        if (end > position) tokens.push({ content: text.slice(position, end), offset: offset + position, color })
        position = end
      }
      for (const match of text.matchAll(shellPattern)) {
        push(match.index, 'var(--nb-ink)')
        const name = Object.keys(match.groups ?? {}).find((key) => match.groups?.[key] !== undefined) ?? ''
        push(match.index + match[0].length, shellColors[name] ?? 'var(--nb-ink)')
      }
      push(text.length, 'var(--nb-ink)')
      return tokens
    })
  }
}
