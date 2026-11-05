/**
 * Colours for the source panel, a port of the family's code-colors.js: a regex pass that assigns the
 * kit's token jobs (`.tok-keyword .tok-number .tok-type .tok-call .tok-string .tok-comment .tok-meta`)
 * to runs of text. It returns data, not DOM, so the same pass serves the Vue panel and the unit tests.
 */
export type TokenKind = 'keyword' | 'number' | 'type' | 'call' | 'string' | 'comment' | 'meta'

export interface CodeToken {
  text: string
  kind?: TokenKind
}

type Rules = Partial<Record<TokenKind, string>>

const csharpKeywords = 'abstract and as async await base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false file finally fixed float for foreach get goto if implicit in init int interface internal is lock long namespace new not null object operator or out override params partial private protected public readonly record ref required return sbyte sealed set short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile when where while with yield'.replaceAll(' ', '|')

// A rule set's own order is its match priority at one position: in C# a comment wins over a string and a
// string over a keyword; in a data file a key (type) wins over a plain string.
const csharp: Rules = {
  comment: String.raw`//[^\n]*|/\*[\s\S]*?\*/`,
  string: String.raw`\$?@?"""[\s\S]*?"""|\$?@?"(?:[^"\\\n]|\\.|"")*"|'(?:[^'\\\n]|\\.)+'`,
  meta: String.raw`^[ \t]*#[a-z]+[^\n]*|(?<=^[ \t]*)\[[A-Z]\w*(?:\([^)\n]*\))?\]`,
  number: String.raw`\b\d[\d_]*(?:\.\d+)?[fFdDmMlLuU]*\b`,
  keyword: String.raw`\b(?:${csharpKeywords})\b`,
  call: String.raw`\b[A-Za-z_]\w*(?=(?:<[\w<>, ?]*>)?\()`,
  type: String.raw`\b[A-Z]\w*`
}

// In a shell the program is the type and its switches are the keywords.
const shell: Rules = {
  comment: String.raw`(?<!\S)#[^\n]*`,
  string: String.raw`"[^"\n]*"|'[^'\n]*'`,
  type: String.raw`^[ \t]*(?:[.&][ \t]+)?[A-Za-z_./\\][\w./\\:-]*(?![\w./\\:-]|[ \t]*=)`,
  keyword: String.raw`(?<=\s)--?[A-Za-z][\w-]*`,
  meta: String.raw`\$[A-Za-z_][\w:]*`,
  number: String.raw`(?<![\w.-])\d+(?:\.\d+)?\b`
}

// Data files: keys as types, literals as strings and numbers.
const data: Rules = {
  comment: String.raw`//[^\n]*|/\*[\s\S]*?\*/|(?<!\S)#[^\n]*`,
  type: String.raw`"(?:[^"\\\n]|\\.)*"(?=\s*:)|^[ \t]*-?[ \t]*[A-Za-z_][\w.-]*(?=:(?:\s|$))`,
  string: String.raw`"(?:[^"\\\n]|\\.)*"|'[^'\n]*'`,
  number: String.raw`(?<![\w.-])-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b`,
  keyword: String.raw`\b(?:true|false|null)\b`
}

// Project files and other XML: element names as types, attribute names as meta.
const xml: Rules = {
  comment: String.raw`<!--[\s\S]*?-->`,
  string: String.raw`"[^"\n]*"|'[^'\n]*'`,
  type: String.raw`(?<=</?)[A-Za-z_][\w.:-]*`,
  meta: String.raw`(?<=<[^>\n]*\s)[A-Za-z_][\w.:-]*(?=\s*=)|<\?\w+|\?>`,
  number: String.raw`(?<![\w.-])\d+(?:\.\d+)*\b`
}

// Markdown: headings as types, code as strings, link targets as meta.
const markdown: Rules = {
  comment: String.raw`<!--[\s\S]*?-->`,
  string: '```[^\n]*\n[\\s\\S]*?\n[ \\t]*```|`[^`\n]+`',
  type: String.raw`^#{1,6}[ \t]+[^\n]*`,
  meta: String.raw`(?<=\]\()[^)\n]+(?=\))`,
  keyword: String.raw`^[ \t]*(?:[-*+]|\d+\.)(?=[ \t])`
}

const kinds: TokenKind[] = ['comment', 'string', 'meta', 'number', 'keyword', 'call', 'type']

function compile(rules: Rules) {
  return new RegExp(Object.entries(rules).map(([kind, source]) => `(?<${kind}>${source})`).join('|'), 'gm')
}

const patterns: Record<string, RegExp> = {
  csharp: compile(csharp),
  powershell: compile(shell),
  bat: compile(shell),
  shell: compile(shell),
  json: compile(data),
  yaml: compile(data),
  xml: compile(xml),
  markdown: compile(markdown)
}

const languages: Record<string, string> = {
  cs: 'csharp',
  ps1: 'powershell',
  psd1: 'powershell',
  cmd: 'bat',
  bat: 'bat',
  sh: 'shell',
  json: 'json',
  yml: 'yaml',
  yaml: 'yaml',
  csproj: 'xml',
  props: 'xml',
  targets: 'xml',
  slnx: 'xml',
  runsettings: 'xml',
  xml: 'xml',
  md: 'markdown'
}

/** The language name for a path, by its extension; the extension itself, or `text`, when none is known. */
export function languageFor(path: string): string {
  const extension = String(path).toLowerCase().match(/\.([a-z0-9]+)$/)?.[1] ?? ''
  return languages[extension] ?? (extension || 'text')
}

/** One token per run of text; runs the rules do not match carry no kind. */
export function highlight(source: string, language: string): CodeToken[] {
  const pattern = patterns[language]
  if (!pattern) return source ? [{ text: source }] : []
  const tokens: CodeToken[] = []
  let position = 0
  for (const match of source.matchAll(pattern)) {
    if (match.index > position) tokens.push({ text: source.slice(position, match.index) })
    const kind = kinds.find((name) => match.groups?.[name] !== undefined)
    tokens.push(kind ? { text: match[0], kind } : { text: match[0] })
    position = match.index + match[0].length
  }
  if (position < source.length) tokens.push({ text: source.slice(position) })
  return tokens
}

/** The tokens split into lines, each line keeping the kind of every token it holds a part of. */
export function splitLines(tokens: CodeToken[]): CodeToken[][] {
  const lines: CodeToken[][] = [[]]
  for (const token of tokens) {
    const pieces = token.text.split('\n')
    pieces.forEach((piece, index) => {
      if (index > 0) lines.push([])
      if (!piece) return
      lines[lines.length - 1].push(token.kind ? { text: piece, kind: token.kind } : { text: piece })
    })
  }
  // A trailing newline ends the last line; it does not start another.
  if (lines.length > 1 && lines[lines.length - 1].length === 0) lines.pop()
  return lines
}

/** Lines of coloured tokens for a file's text; CRLF is read as LF so line numbers match the repository. */
export function highlightLines(source: string, language: string): CodeToken[][] {
  return splitLines(highlight(source.replace(/\r\n?/g, '\n'), language))
}
