/**
 * The source bundle on the site: tools/bundle_site_source.mjs writes every learning file as
 * /source/<path>.txt and one /source/index.json at build time. These helpers read that index,
 * build the in-site links to a file or a directory, and lay out a directory listing. No Vue here,
 * so the unit tests run them in Node.
 */
export interface SourceFile {
  path: string
  bytes: number
  lines: number
}

export interface SourceIndex {
  version: number
  revision: string
  repository: string
  blobBase: string
  files: SourceFile[]
}

export interface DirectoryEntry {
  name: string
  path: string
  directory: boolean
  files: number
  bytes: number
  lines: number
}

export type LineRange = [number, number]

/** The source page, before the site base. */
export const sourceRoute = '/源码.html'

export function fileName(path: string): string {
  return String(path).split('/').pop() ?? ''
}

/** The directory a path sits in, `''` at the root. */
export function parentOf(path: string): string {
  const slash = path.lastIndexOf('/')
  return slash < 0 ? '' : path.slice(0, slash)
}

/** In-site link to one bundled file, with `#L5-L9` keeping a line range. Prefix with the site base. */
export function sourceHref(path: string, range?: LineRange | null): string {
  const fragment = range ? (range[0] === range[1] ? `#L${range[0]}` : `#L${range[0]}-L${range[1]}`) : ''
  return `${sourceRoute}?path=${encodeURIComponent(path)}${fragment}`
}

/** In-site link to a directory listing; the root listing when `dir` is empty. */
export function directoryHref(dir: string): string {
  const clean = dir.replace(/^\/+|\/+$/g, '')
  return clean ? `${sourceRoute}?dir=${encodeURIComponent(clean)}` : sourceRoute
}

/** The GitHub address of a bundled file, for the panel's one text link. */
export function blobUrl(blobBase: string, path: string): string {
  return blobBase + path.split('/').map(encodeURIComponent).join('/')
}

/** `#L5-L9`, `#L5-9` or `#L5` as an ordered pair; null for any other hash. */
export function parseRange(hash: string): LineRange | null {
  const match = String(hash).match(/^#?L(\d+)(?:-L?(\d+))?$/)
  if (!match) return null
  const from = Number(match[1])
  const to = Number(match[2] ?? match[1])
  return [Math.min(from, to), Math.max(from, to)]
}

export function inRange(line: number, ranges: readonly LineRange[]): boolean {
  return ranges.some(([from, to]) => line >= from && line <= to)
}

/** The entries directly under `dir`: subdirectories first, then files, each by name. */
export function listDirectory(files: readonly SourceFile[], dir: string): DirectoryEntry[] {
  const prefix = dir ? `${dir.replace(/\/+$/, '')}/` : ''
  const children = new Map<string, DirectoryEntry>()
  for (const file of files) {
    if (!file.path.startsWith(prefix)) continue
    const rest = file.path.slice(prefix.length)
    const slash = rest.indexOf('/')
    const name = slash === -1 ? rest : rest.slice(0, slash)
    const entry = children.get(name) ?? { name, path: prefix + name, directory: slash !== -1, files: 0, bytes: 0, lines: 0 }
    entry.files += 1
    entry.bytes += file.bytes
    entry.lines += file.lines
    children.set(name, entry)
  }
  return [...children.values()].sort((left, right) =>
    Number(right.directory) - Number(left.directory) || left.name.localeCompare(right.name, 'en'))
}

/** The other files in the same directory as `path`, by name. */
export function siblingsOf(files: readonly SourceFile[], path: string): SourceFile[] {
  const dir = parentOf(path)
  return files
    .filter((file) => file.path !== path && parentOf(file.path) === dir)
    .sort((left, right) => left.path.localeCompare(right.path, 'en'))
}

/** The words beside an entry: files and lines for a directory, lines and size for a file. */
export function describeEntry(entry: Pick<DirectoryEntry, 'directory' | 'files' | 'lines' | 'bytes'>): string {
  return entry.directory
    ? `${entry.files} 个文件 · ${entry.lines} 行`
    : `${entry.lines} 行 · ${(entry.bytes / 1024).toFixed(1)} KB`
}

const indexCache = new Map<string, Promise<SourceIndex>>()
const textCache = new Map<string, Promise<string>>()

/** The bundle index from `<base>index.json`, fetched once per base. Browser only. */
export function loadSourceIndex(base: string): Promise<SourceIndex> {
  let pending = indexCache.get(base)
  if (!pending) {
    pending = fetch(`${base}index.json`).then(async (response) => {
      if (!response.ok) throw new Error(`无法读取源码索引（${response.status}）`)
      const index = await response.json() as SourceIndex
      if (index.version !== 1 || !Array.isArray(index.files)) throw new Error('源码索引的版本不受支持')
      return index
    })
    pending.catch(() => indexCache.delete(base))
    indexCache.set(base, pending)
  }
  return pending
}

/** One bundled file's text from `<base><path>.txt`, fetched once per file, with LF line ends. Browser only. */
export function loadSourceText(base: string, path: string): Promise<string> {
  const key = `${base}${path}`
  let pending = textCache.get(key)
  if (!pending) {
    pending = fetch(`${base}${path.split('/').map(encodeURIComponent).join('/')}.txt`).then(async (response) => {
      if (!response.ok) throw new Error(`无法读取 ${path}（${response.status}）`)
      return (await response.text()).replace(/\r\n?/g, '\n')
    })
    pending.catch(() => textCache.delete(key))
    textCache.set(key, pending)
  }
  return pending
}
