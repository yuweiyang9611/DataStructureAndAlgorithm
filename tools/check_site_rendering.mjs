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
  ['主线/index.html', ['主线路线总览', '根据前置依赖和本地掌握记录推荐下一步']],
  ['进阶/index.html', ['进阶路线总览', '尚未满足的前置会明确显示']],
  ['综合项目/index.html', ['综合项目总览', '四个项目']],
  ['主线/阶段四至六.html', ['第六阶段：动态规划与回溯', '斐波那契', 'n 皇后']],
  ['进阶/十八周学习计划与验收.html', ['第 1–8 周核心阶段验收', '第 13–18 周学习顺序', '第 9–18 周验收']],
  ['综合项目/工程原则与证据链.html', ['四个项目共同的 C# 实践', '持续可证明的证据链']],
  ['学习单元.html', ['class="unit-explorer"', '22 个可验证学习单元', '根据依赖与复习时间推荐下一步', '掌握验证：自测 + 三类证据', 'class="unit-quiz"', 'class="unit-evidence"']],
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
const sitemapLocations = new Set(
  [...sitemapXml.matchAll(/<loc>([^<]+)<\/loc>/g)].map((match) => match[1])
)
const sitemapBase = `${siteOrigin}${siteBase}`
for (const sourcePage of [
  'CSharp数据结构与算法学习指导.html',
  'CSharp数据结构与算法进阶学习指导.html',
  '综合项目实战学习指导.html'
]) {
  if (sitemapLocations.has(new URL(sourcePage, sitemapBase).href)) {
    errors.push(`sitemap.xml: noindex source page must be excluded: ${sourcePage}`)
  }
}
for (const indexedPage of ['开始学习.html', '学习单元.html', '追踪实验室.html', '主线/', '进阶/', '综合项目/', '主线/阶段四至六.html']) {
  if (!sitemapLocations.has(new URL(indexedPage, sitemapBase).href)) {
    errors.push(`sitemap.xml: missing indexed page ${indexedPage}`)
  }
}

const learningUnitSource = readFileSync(
  join(repositoryRoot, 'docs', '.vitepress', 'data', 'learningUnits.ts'),
  'utf8'
)
const unitBlocks = [...learningUnitSource.matchAll(/^\s{2}\{\r?\n([\s\S]*?)^\s{2}\},?$/gm)].map((match) => match[1])
const learningUnitIds = unitBlocks
  .map((block) => block.match(/^\s{4}id: '([^']+)'/m)?.[1])
  .filter(Boolean)
const uniqueLearningUnitIds = new Set(learningUnitIds)
if (learningUnitIds.length !== 22 || uniqueLearningUnitIds.size !== 22) {
  errors.push(`learningUnits.ts: expected 22 unique units, found ${learningUnitIds.length}/${uniqueLearningUnitIds.size}`)
}

const knownStages = new Set(['S02', 'S03', 'S04', 'S05', 'S06', 'A01', 'A02', 'P03', 'PRACTICE', 'S07'])
const knownDifficulties = new Set(['入门', '进阶', '高级', '混合'])
const prerequisitesById = new Map()
for (const block of unitBlocks) {
  const id = block.match(/^\s{4}id: '([^']+)'/m)?.[1]
  if (!id) continue
  const stage = block.match(/stage: '([^']+)'/)?.[1]
  const difficulty = block.match(/difficulty: '([^']+)'/)?.[1]
  if (!knownStages.has(stage)) errors.push(`learningUnits.ts: invalid stage ${stage} for ${id}`)
  if (!knownDifficulties.has(difficulty)) errors.push(`learningUnits.ts: invalid difficulty ${difficulty} for ${id}`)

  const prerequisiteMatch = block.match(/prerequisites: \[([^\]]*)\]/)
  if (!prerequisiteMatch) {
    errors.push(`learningUnits.ts: missing literal prerequisites array for ${id}`)
  }
  const prerequisiteText = prerequisiteMatch?.[1] ?? ''
  const prerequisites = [...prerequisiteText.matchAll(/'([^']+)'/g)].map((match) => match[1])
  const unsupportedPrerequisiteSyntax = prerequisiteText
    .replace(/'[^']+'/g, '')
    .replace(/,/g, '')
    .trim()
  if (unsupportedPrerequisiteSyntax) {
    errors.push(`learningUnits.ts: prerequisites for ${id} must use single-quoted literal IDs`)
  }
  if (new Set(prerequisites).size !== prerequisites.length) {
    errors.push(`learningUnits.ts: duplicate prerequisite for ${id}`)
  }
  if (prerequisites.includes(id)) errors.push(`learningUnits.ts: ${id} cannot depend on itself`)
  for (const prerequisite of prerequisites) {
    if (!uniqueLearningUnitIds.has(prerequisite)) {
      errors.push(`learningUnits.ts: unknown prerequisite ${prerequisite} for ${id}`)
    }
  }
  prerequisitesById.set(id, prerequisites)
}

const indegree = new Map(learningUnitIds.map((id) => [id, prerequisitesById.get(id)?.length ?? 0]))
const dependents = new Map(learningUnitIds.map((id) => [id, []]))
for (const [id, prerequisites] of prerequisitesById) {
  for (const prerequisite of prerequisites) dependents.get(prerequisite)?.push(id)
}
const readyUnits = learningUnitIds.filter((id) => indegree.get(id) === 0)
let visitedUnitCount = 0
while (readyUnits.length) {
  const id = readyUnits.shift()
  visitedUnitCount += 1
  for (const dependent of dependents.get(id) ?? []) {
    const nextDegree = indegree.get(dependent) - 1
    indegree.set(dependent, nextDegree)
    if (nextDegree === 0) readyUnits.push(dependent)
  }
}
if (visitedUnitCount !== learningUnitIds.length) {
  errors.push('learningUnits.ts: prerequisite graph contains a cycle')
}

const learningUnitHtml = htmlByRelativePath.get('学习单元.html') ?? ''
const quizFieldsetCount = [...learningUnitHtml.matchAll(/class="unit-quiz"/g)].length
const evidenceFieldsetCount = [...learningUnitHtml.matchAll(/class="unit-evidence"/g)].length
const quizRadioCount = [...learningUnitHtml.matchAll(/<input\b[^>]*\btype="radio"/g)].length
const evidenceCheckboxCount = [...learningUnitHtml.matchAll(/<input\b[^>]*\btype="checkbox"/g)].length
if (quizFieldsetCount !== learningUnitIds.length) {
  errors.push(`学习单元.html: expected ${learningUnitIds.length} quiz fieldsets, found ${quizFieldsetCount}`)
}
if (evidenceFieldsetCount !== learningUnitIds.length) {
  errors.push(`学习单元.html: expected ${learningUnitIds.length} evidence fieldsets, found ${evidenceFieldsetCount}`)
}
if (quizRadioCount !== learningUnitIds.length * 4) {
  errors.push(`学习单元.html: expected ${learningUnitIds.length * 4} quiz radios, found ${quizRadioCount}`)
}
if (evidenceCheckboxCount !== learningUnitIds.length * 3) {
  errors.push(`学习单元.html: expected ${learningUnitIds.length * 3} evidence checkboxes, found ${evidenceCheckboxCount}`)
}
for (const [id, prerequisites] of prerequisitesById) {
  const dependentPosition = learningUnitHtml.indexOf(`id="unit-${id}"`)
  if (dependentPosition < 0) errors.push(`学习单元.html: missing rendered unit ${id}`)
  for (const prerequisite of prerequisites) {
    const prerequisitePosition = learningUnitHtml.indexOf(`id="unit-${prerequisite}"`)
    if (prerequisitePosition > dependentPosition) {
      errors.push(`学习单元.html: prerequisite ${prerequisite} renders after dependent ${id}`)
    }
  }
}

const learningPaths = new Set(
  [...learningUnitSource.matchAll(/'((?:DataStructureAndAlgorithm)[^']+\.cs)'/g)].map((match) => match[1])
)
for (const path of learningPaths) {
  if (!existsSync(join(repositoryRoot, path))) errors.push(`learningUnits.ts: missing source or test path ${path}`)
}

const masterySource = readFileSync(
  join(repositoryRoot, 'docs', '.vitepress', 'data', 'masteryContent.ts'),
  'utf8'
)
const masteryRevision = Number(masterySource.match(/export const masteryRevision = (\d+)/)?.[1])
if (!Number.isInteger(masteryRevision) || masteryRevision <= 0) {
  errors.push('masteryContent.ts: masteryRevision must be a positive integer')
}
const masteryMatches = [...masterySource.matchAll(/^\s{2}'([^']+)': \{/gm)]
const masteryIds = masteryMatches.map((match) => match[1])
if (masteryIds.length !== 22 || new Set(masteryIds).size !== 22) {
  errors.push(`masteryContent.ts: expected 22 unique entries, found ${masteryIds.length}/${new Set(masteryIds).size}`)
}
for (const id of learningUnitIds) {
  if (!masteryIds.includes(id)) errors.push(`masteryContent.ts: missing mastery content for ${id}`)
}
for (const id of masteryIds) {
  if (!uniqueLearningUnitIds.has(id)) errors.push(`masteryContent.ts: unknown unit ${id}`)
}
for (let index = 0; index < masteryMatches.length; index += 1) {
  const match = masteryMatches[index]
  const id = match[1]
  const start = match.index
  const end = masteryMatches[index + 1]?.index ?? masterySource.lastIndexOf('\n}')
  const block = masterySource.slice(start, end)
  const question = block.match(/question: '([^']+)'/)?.[1]
  const optionLine = block.match(/options: \[([^\r\n]+)\]/)?.[1] ?? ''
  const options = [...optionLine.matchAll(/'([^']+)'/g)].map((option) => option[1])
  const correctIndex = Number(block.match(/correctIndex: (\d+)/)?.[1])
  const explanation = block.match(/explanation: '([^']+)'/)?.[1]
  if (!question?.trim()) errors.push(`masteryContent.ts: empty question for ${id}`)
  if (options.length !== 4 || new Set(options).size !== 4) {
    errors.push(`masteryContent.ts: ${id} must have four unique options`)
  }
  if (!Number.isInteger(correctIndex) || correctIndex < 0 || correctIndex >= options.length) {
    errors.push(`masteryContent.ts: invalid correctIndex for ${id}`)
  }
  if (!explanation?.trim()) errors.push(`masteryContent.ts: empty explanation for ${id}`)
  const unitStart = learningUnitHtml.indexOf(`id="unit-${id}"`)
  const unitEnd = unitStart >= 0 ? learningUnitHtml.indexOf('</li>', unitStart) : -1
  const renderedUnit = unitStart >= 0 && unitEnd > unitStart
    ? decodeHtml(learningUnitHtml.slice(unitStart, unitEnd))
    : ''
  for (const expectedText of [question, ...options].filter(Boolean)) {
    if (!renderedUnit.includes(expectedText)) {
      errors.push(`学习单元.html: ${id} is missing rendered mastery text ${expectedText}`)
    }
  }
  for (const evidenceId of ['invariant', 'test', 'transfer']) {
    const evidence = block.match(new RegExp(`\\b${evidenceId}: '([^']+)'`))?.[1]
    if (!evidence?.trim()) errors.push(`masteryContent.ts: missing ${evidenceId} evidence for ${id}`)
    else if (!renderedUnit.includes(evidence)) {
      errors.push(`学习单元.html: ${id} is missing rendered ${evidenceId} evidence`)
    }
  }
}

const explorerSource = readFileSync(
  join(repositoryRoot, 'docs', '.vitepress', 'theme', 'components', 'LearningUnitExplorer.vue'),
  'utf8'
)
const reviewDays = [...(explorerSource.match(/const reviewIntervals = \[([^\]]+)\]\.map/)?.[1] ?? '').matchAll(/\d+/g)]
  .map((match) => Number(match[0]))
if (reviewDays.length < 2 || reviewDays.some((days) => days <= 0) || reviewDays.some((days, index) => index > 0 && days <= reviewDays[index - 1])) {
  errors.push('LearningUnitExplorer.vue: review intervals must be strictly increasing positive days')
}
for (const sentinel of [
  "dsa-learning-progress-v1",
  "dsa-learning-progress-v2",
  "'not-started'",
  "'learning'",
  "'verified'",
  "'review'",
  'topologicalOrder',
  'selfTestRevision',
  'evidenceRevision',
  'record.selfTestPassedAt >= record.nextReviewAt',
  'prerequisitesSatisfied(unit)'
]) {
  if (!explorerSource.includes(sentinel)) errors.push(`LearningUnitExplorer.vue: missing mastery sentinel ${sentinel}`)
}

if (errors.length) {
  console.error(`Site verification failed with ${errors.length} issue(s):`)
  for (const error of errors.slice(0, 80)) console.error(`- ${error}`)
  if (errors.length > 80) console.error(`- … ${errors.length - 80} more`)
  process.exit(1)
}

console.log(`Verified ${htmlFiles.length} HTML pages, internal targets, metadata, mastery data, module structure and six Trace assets.`)
