import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs'
import { dirname, extname, join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const outputRoot = join(repositoryRoot, 'docs', '.vitepress', 'dist')
const siteOrigin = 'https://yuweiyang9611.github.io'
const siteBase = '/DataStructureAndAlgorithm/'
const errors = []

function walk(directory, extension) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name)
    if (entry.isDirectory()) return walk(path, extension)
    return !extension || extname(entry.name) === extension ? [path] : []
  })
}

function relativeOutput(path) {
  return relative(outputRoot, path).split(sep).join('/')
}

function decodeHtml(value) {
  return value
    .replaceAll('&amp;', '&')
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
}

function attributeValues(html, tagPattern, attribute) {
  const values = []
  const tagExpression = new RegExp(`<${tagPattern}\\b[^>]*>`, 'gi')
  for (const match of html.matchAll(tagExpression)) {
    const attributeMatch = match[0].match(new RegExp(`\\b${attribute}="([^"]*)"`, 'i'))
    if (attributeMatch) values.push(decodeHtml(attributeMatch[1]))
  }
  return values
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

if (!existsSync(outputRoot)) {
  console.error('VitePress output does not exist. Run the build first.')
  process.exit(1)
}

const htmlFiles = walk(outputRoot, '.html')
const htmlByRelativePath = new Map(htmlFiles.map((path) => [relativeOutput(path), readFileSync(path, 'utf8')]))
const canonicalOwners = new Map()
const titleOwners = new Map()
const descriptionOwners = new Map()

for (const [pagePath, html] of htmlByRelativePath) {
  const title = html.match(/<title>([^<]+)<\/title>/i)?.[1]?.trim()
  const descriptions = [...html.matchAll(/<meta\s+name="description"\s+content="([^"]+)"/gi)]
  const canonicals = attributeValues(html, 'link', 'href').filter((value) => {
    const tag = [...html.matchAll(/<link\b[^>]*>/gi)].find((match) => match[0].includes(`href="${value.replaceAll('&', '&amp;')}"`))?.[0]
    return tag && /\brel="canonical"/i.test(tag)
  })
  const ogUrls = [...html.matchAll(/<meta\s+property="og:url"\s+content="([^"]+)"/gi)]

  if (!title) errors.push(`${pagePath}: missing <title>`)
  if (descriptions.length !== 1 || !descriptions[0]?.[1].trim()) {
    errors.push(`${pagePath}: expected one non-empty meta description`)
  }
  if (canonicals.length !== 1) errors.push(`${pagePath}: expected one canonical link`)
  if (ogUrls.length !== 1) errors.push(`${pagePath}: expected one og:url`)
  if (canonicals.length === 1 && ogUrls.length === 1 && canonicals[0] !== decodeHtml(ogUrls[0][1])) {
    errors.push(`${pagePath}: og:url differs from canonical`)
  }
  if (!/<html\s+lang="zh-CN"/i.test(html)) errors.push(`${pagePath}: html lang is not zh-CN`)
  if (html.includes('<!--@include:')) errors.push(`${pagePath}: unprocessed Markdown include directive`)
  if (/&lt;\/?(?:div|section|article)\b/i.test(html)) errors.push(`${pagePath}: HTML layout appears escaped`)

  if (title) {
    if (titleOwners.has(title)) errors.push(`${pagePath}: duplicate title also used by ${titleOwners.get(title)}`)
    else titleOwners.set(title, pagePath)
  }
  const description = descriptions[0]?.[1]?.trim()
  if (description && pagePath !== '404.html') {
    if (descriptionOwners.has(description)) {
      errors.push(`${pagePath}: duplicate description also used by ${descriptionOwners.get(description)}`)
    } else {
      descriptionOwners.set(description, pagePath)
    }
  }

  for (const canonical of canonicals) {
    if (canonicalOwners.has(canonical)) {
      errors.push(`${pagePath}: duplicate canonical also used by ${canonicalOwners.get(canonical)}`)
    } else {
      canonicalOwners.set(canonical, pagePath)
    }
  }

  const attributeExpression = /\b(?:href|src)="([^"]+)"/gi
  for (const match of html.matchAll(attributeExpression)) {
    const rawTarget = decodeHtml(match[1])
    if (!rawTarget || /^(?:mailto:|tel:|data:|javascript:)/i.test(rawTarget)) continue
    if (rawTarget.startsWith('#')) {
      if (rawTarget.length === 1) continue
      try {
        const anchor = decodeURIComponent(rawTarget.slice(1))
        if (!new RegExp(`\\bid="${escapeRegExp(anchor)}"`).test(html)) {
          errors.push(`${pagePath}: missing same-page anchor #${anchor}`)
        }
      } catch {
        errors.push(`${pagePath}: malformed same-page anchor ${rawTarget}`)
      }
      continue
    }

    let resolvedUrl
    try {
      const pageUrl = new URL(`${siteBase}${pagePath}`, siteOrigin)
      resolvedUrl = new URL(rawTarget, pageUrl)
    } catch {
      errors.push(`${pagePath}: invalid URL ${rawTarget}`)
      continue
    }

    if (resolvedUrl.origin !== siteOrigin) continue
    if (!resolvedUrl.pathname.startsWith(siteBase)) {
      errors.push(`${pagePath}: internal URL escapes site base: ${rawTarget}`)
      continue
    }

    let targetRelative
    try {
      targetRelative = decodeURIComponent(resolvedUrl.pathname.slice(siteBase.length))
    } catch {
      errors.push(`${pagePath}: malformed percent-encoding in ${rawTarget}`)
      continue
    }

    if (!targetRelative || targetRelative.endsWith('/')) targetRelative += 'index.html'
    if (!extname(targetRelative)) targetRelative += '.html'
    const targetPath = resolve(outputRoot, targetRelative)
    if (!targetPath.startsWith(resolve(outputRoot) + sep) && targetPath !== resolve(outputRoot)) {
      errors.push(`${pagePath}: resolved target leaves output directory: ${rawTarget}`)
      continue
    }
    if (!existsSync(targetPath) || !statSync(targetPath).isFile()) {
      errors.push(`${pagePath}: missing internal target ${rawTarget} -> ${targetRelative}`)
      continue
    }

    if (resolvedUrl.hash && extname(targetRelative) === '.html') {
      const anchor = decodeURIComponent(resolvedUrl.hash.slice(1))
      const targetHtml = readFileSync(targetPath, 'utf8')
      if (!new RegExp(`\\bid="${escapeRegExp(anchor)}"`).test(targetHtml)) {
        errors.push(`${pagePath}: missing anchor #${anchor} in ${targetRelative}`)
      }
    }
  }
}

const homepage = htmlByRelativePath.get('index.html') ?? ''
for (const className of ['learning-path', 'learning-topic-grid', 'learning-projects', 'learning-evidence', 'learning-cta']) {
  if (!homepage.includes(`class="${className}"`)) errors.push(`index.html: missing rendered layout ${className}`)
  if (homepage.includes(`&quot;${className}&quot;`)) errors.push(`index.html: ${className} rendered as text`)
}

const moduleExpectations = [
  ['主线/阶段四至六.html', ['第六阶段：动态规划与回溯', '斐波那契', 'n 皇后']],
  ['进阶/十八周学习计划与验收.html', ['第 1–8 周核心阶段验收', '第 13–18 周学习顺序', '第 9–18 周验收']],
  ['综合项目/工程原则与证据链.html', ['四个项目共同的 C# 实践', '持续可证明的证据链']],
  ['学习单元.html', ['class="unit-explorer"', '22 个可验证学习单元']],
  ['追踪实验室.html', ['class="trace-lab"']]
]

for (const [pagePath, sentinels] of moduleExpectations) {
  const html = htmlByRelativePath.get(pagePath)
  if (!html) {
    errors.push(`${pagePath}: expected output page is missing`)
    continue
  }
  for (const sentinel of sentinels) {
    if (!html.includes(sentinel)) errors.push(`${pagePath}: missing expected content ${sentinel}`)
  }

  const headings = [...html.matchAll(/<h([1-6])\b/gi)].map((match) => Number(match[1]))
  for (let index = 1; index < headings.length; index += 1) {
    if (headings[index] > headings[index - 1] + 1) {
      errors.push(`${pagePath}: heading level skips from h${headings[index - 1]} to h${headings[index]}`)
      break
    }
  }
}

for (const asset of ['robots.txt', 'favicon.svg', 'og.png', 'traces/manifest.json']) {
  if (!existsSync(join(outputRoot, asset))) errors.push(`missing public asset ${asset}`)
}

const traceDirectory = join(outputRoot, 'traces')
if (existsSync(join(traceDirectory, 'manifest.json'))) {
  const manifest = JSON.parse(readFileSync(join(traceDirectory, 'manifest.json'), 'utf8'))
  if (manifest.schemaVersion !== 1 || !Array.isArray(manifest.traces) || manifest.traces.length !== 6) {
    errors.push('traces/manifest.json: expected schemaVersion 1 and six traces')
  } else {
    const traceIds = new Set()
    const traceFiles = new Set()
    for (const definition of manifest.traces) {
      if (typeof definition.id !== 'string' || !/^[a-z0-9-]+$/.test(definition.id) || traceIds.has(definition.id)) {
        errors.push(`traces/manifest.json: invalid or duplicate id ${definition.id}`)
        continue
      }
      traceIds.add(definition.id)
      if (typeof definition.file !== 'string' || definition.file !== `${definition.id}.json` || traceFiles.has(definition.file)) {
        errors.push(`traces/manifest.json: invalid or duplicate file for ${definition.id}`)
        continue
      }
      traceFiles.add(definition.file)
      for (const key of ['title', 'category', 'level', 'summary', 'algorithm', 'source', 'command']) {
        if (typeof definition[key] !== 'string' || !definition[key].trim()) {
          errors.push(`traces/manifest.json: ${definition.id}.${key} must be non-empty`)
        }
      }
      if (!Number.isInteger(definition.eventCount) || definition.eventCount <= 0) {
        errors.push(`traces/manifest.json: ${definition.id}.eventCount must be positive`)
      }
      for (const hint of ['sequence', 'range', 'graph']) {
        if (!Array.isArray(definition.stateHints?.[hint]) || definition.stateHints[hint].some((value) => typeof value !== 'string')) {
          errors.push(`traces/manifest.json: ${definition.id}.stateHints.${hint} must be a string array`)
        }
      }
      if (!existsSync(join(repositoryRoot, definition.source))) {
        errors.push(`traces/manifest.json: source does not exist for ${definition.id}`)
      }
      const payloadPath = join(traceDirectory, definition.file)
      if (!existsSync(payloadPath)) {
        errors.push(`traces/manifest.json: missing ${definition.file}`)
        continue
      }
      const payload = JSON.parse(readFileSync(payloadPath, 'utf8'))
      if (!Array.isArray(payload.trace) || payload.trace.length !== definition.eventCount) {
        errors.push(`${definition.file}: event count differs from manifest`)
        continue
      }
      payload.trace.forEach((event, index) => {
        if (event.schemaVersion !== 1 || event.step !== index + 1) {
          errors.push(`${definition.file}: invalid schema or step at index ${index}`)
        }
        if (!event.state || Object.values(event.state).some((value) => typeof value !== 'string')) {
          errors.push(`${definition.file}: state values must be strings at index ${index}`)
        }
        if (typeof event.algorithm !== 'string' || !event.algorithm || typeof event.operation !== 'string' || !event.operation || typeof event.description !== 'string') {
          errors.push(`${definition.file}: invalid event text fields at index ${index}`)
        }
      })
    }

    const actualJsonFiles = readdirSync(traceDirectory, { withFileTypes: true })
      .filter((entry) => entry.isFile() && entry.name.endsWith('.json'))
      .map((entry) => entry.name)
    const expectedJsonFiles = new Set(['manifest.json', ...traceFiles])
    for (const file of actualJsonFiles) {
      if (!expectedJsonFiles.has(file)) errors.push(`traces/: unexpected JSON asset ${file}`)
    }
    for (const file of expectedJsonFiles) {
      if (!actualJsonFiles.includes(file)) errors.push(`traces/: missing expected JSON asset ${file}`)
    }
  }
}

for (const sourcePage of [
  'CSharp数据结构与算法学习指导.html',
  'CSharp数据结构与算法进阶学习指导.html',
  '综合项目实战学习指导.html'
]) {
  const html = htmlByRelativePath.get(sourcePage) ?? ''
  if (!/<meta\s+name="robots"\s+content="noindex, follow"/i.test(html)) {
    errors.push(`${sourcePage}: complete source page must be noindex, follow`)
  }
}

const sitemapXml = readFileSync(join(outputRoot, 'sitemap.xml'), 'utf8')
for (const sourcePage of [
  'CSharp数据结构与算法学习指导.html',
  'CSharp数据结构与算法进阶学习指导.html',
  '综合项目实战学习指导.html'
]) {
  if (sitemapXml.includes(encodeURI(sourcePage))) {
    errors.push(`sitemap.xml: noindex source page must be excluded: ${sourcePage}`)
  }
}
for (const indexedPage of ['开始学习.html', '学习单元.html', '追踪实验室.html', '主线/阶段四至六.html']) {
  if (!sitemapXml.includes(encodeURI(indexedPage))) {
    errors.push(`sitemap.xml: missing indexed page ${indexedPage}`)
  }
}

const learningUnitSource = readFileSync(
  join(repositoryRoot, 'docs', '.vitepress', 'data', 'learningUnits.ts'),
  'utf8'
)
const learningUnitIds = [...learningUnitSource.matchAll(/^\s{4}id: '([^']+)'/gm)].map((match) => match[1])
const uniqueLearningUnitIds = new Set(learningUnitIds)
if (learningUnitIds.length !== 22 || uniqueLearningUnitIds.size !== 22) {
  errors.push(`learningUnits.ts: expected 22 unique units, found ${learningUnitIds.length}/${uniqueLearningUnitIds.size}`)
}

for (const block of learningUnitSource.matchAll(/prerequisites: \[([^\]]*)\]/g)) {
  for (const match of block[1].matchAll(/'([^']+)'/g)) {
    if (!uniqueLearningUnitIds.has(match[1])) {
      errors.push(`learningUnits.ts: unknown prerequisite ${match[1]}`)
    }
  }
}

const learningPaths = new Set(
  [...learningUnitSource.matchAll(/'((?:DataStructureAndAlgorithm)[^']+\.cs)'/g)].map((match) => match[1])
)
for (const path of learningPaths) {
  if (!existsSync(join(repositoryRoot, path))) errors.push(`learningUnits.ts: missing source or test path ${path}`)
}

if (errors.length) {
  console.error(`Site verification failed with ${errors.length} issue(s):`)
  for (const error of errors.slice(0, 80)) console.error(`- ${error}`)
  if (errors.length > 80) console.error(`- … ${errors.length - 80} more`)
  process.exit(1)
}

console.log(`Verified ${htmlFiles.length} HTML pages, internal targets, metadata, module structure and six Trace assets.`)
