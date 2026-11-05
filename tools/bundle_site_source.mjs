// Source read in place: every file a page can point at, copied at this checkout's revision as plain text
// into docs/public/source/<path>.txt, with one index.json beside them. Runs inside `docs:build` before
// VitePress, like the Trace step, so the panel on a page never drifts from the repository. The folder
// is generated output and stays out of git.
import { existsSync, mkdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { dirname, join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { learningUnits } from '../docs/.vitepress/data/learningUnits.ts'
import { blobBase, repository } from '../docs/.vitepress/data/repository.ts'

const repositoryRoot = join(dirname(fileURLToPath(import.meta.url)), '..')
const outputDirectory = join(repositoryRoot, 'docs', 'public', 'source')

// The learning folders: the library, its tests, the two demos and the four scenario projects, plus the
// root files that explain how they build. Only files a reader can learn from are bundled.
const sourceRoots = [
  'DataStructureAndAlgorithm',
  'DataStructureAndAlgorithm.Test',
  'DataStructureAndAlgorithm.Demo',
  'DataStructureAndAlgorithm.P3Demo',
  'DataStructureAndAlgorithm.Scenarios.CityDelivery',
  'DataStructureAndAlgorithm.Scenarios.MiniSearch',
  'DataStructureAndAlgorithm.Scenarios.MiniStorage',
  'DataStructureAndAlgorithm.Scenarios.ProjectScheduling'
]
const rootFiles = ['README.md', 'DataStructureAndAlgorithm.slnx', 'Directory.Build.props', 'Directory.Build.targets', 'global.json', 'coverage.runsettings']
const sourceExtensions = new Set(['.cs', '.csproj', '.props', '.targets', '.slnx', '.json', '.md', '.ps1', '.psd1', '.cmd', '.sh', '.yml', '.yaml', '.runsettings', '.txt', '.editorconfig'])
// Only files git tracks are bundled, so local build output, coverage and mutation reports never get in. The
// directory list is a second guard for the same folders; it is case-sensitive on purpose.
const excludedDirectories = new Set(['bin', 'obj', 'node_modules', 'TestResults', 'BenchmarkDotNet.Artifacts', 'StrykerOutput', '.git', '.vs', '.idea'])
const sizeCap = 256 * 1024

function extensionOf(name) {
  const dot = name.lastIndexOf('.')
  return dot < 0 ? '' : name.slice(dot).toLowerCase()
}

function trackedFiles(root) {
  const result = spawnSync('git', ['-C', repositoryRoot, 'ls-files', '-z', '--', root], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 })
  if (result.status !== 0) throw new Error(`git ls-files failed for ${root}: ${result.stderr}`)
  return result.stdout.split('\0').filter(Boolean)
}

function revision() {
  if (/^[0-9a-f]{40}$/i.test(process.env.GITHUB_SHA ?? '')) return process.env.GITHUB_SHA.toLowerCase()
  const result = spawnSync('git', ['-C', repositoryRoot, 'rev-parse', 'HEAD'], { encoding: 'utf8' })
  const sha = result.status === 0 ? result.stdout.trim().toLowerCase() : ''
  if (!/^[0-9a-f]{40}$/.test(sha)) throw new Error('Could not read the repository revision from git or GITHUB_SHA.')
  return sha
}

const candidates = []
for (const root of sourceRoots) {
  if (!existsSync(join(repositoryRoot, root))) throw new Error(`Source root does not exist: ${root}`)
  candidates.push(...trackedFiles(root))
}
for (const file of rootFiles) {
  if (existsSync(join(repositoryRoot, file))) candidates.push(file)
}

rmSync(outputDirectory, { recursive: true, force: true })
mkdirSync(outputDirectory, { recursive: true })

const files = []
const bundled = new Set()
let totalBytes = 0
for (const path of candidates) {
  const absolute = join(repositoryRoot, path)
  if (!sourceExtensions.has(extensionOf(path))) continue
  if (path.split('/').some((segment) => excludedDirectories.has(segment))) continue
  if (!existsSync(absolute) || statSync(absolute).size > sizeCap) continue
  const text = readFileSync(absolute, 'utf8').replace(/^﻿/, '')
  const target = join(outputDirectory, `${path}.txt`)
  mkdirSync(dirname(target), { recursive: true })
  writeFileSync(target, text, 'utf8')
  const bytes = Buffer.byteLength(text, 'utf8')
  let lines = (text.match(/\n/g) ?? []).length
  if (text.length > 0 && !text.endsWith('\n')) lines += 1
  files.push({ path, bytes, lines })
  bundled.add(path)
  totalBytes += bytes
}

// Every file a unit or a Trace names must be in the bundle, or its panel would have nothing to show.
const required = new Map()
for (const unit of learningUnits) {
  for (const path of [...unit.sources, ...unit.tests]) required.set(path, `unit ${unit.id}`)
}
const manifestPath = join(repositoryRoot, 'docs', 'public', 'traces', 'manifest.json')
if (existsSync(manifestPath)) {
  for (const trace of JSON.parse(readFileSync(manifestPath, 'utf8')).traces ?? []) required.set(trace.source, `trace ${trace.id}`)
}
for (const [path, owner] of required) {
  if (!bundled.has(path)) throw new Error(`${owner} points at a file outside the source bundle: ${path}`)
}

files.sort((left, right) => (left.path < right.path ? -1 : left.path > right.path ? 1 : 0))
const index = { version: 1, revision: revision(), repository, blobBase, files }
writeFileSync(join(outputDirectory, 'index.json'), `${JSON.stringify(index)}\n`, 'utf8')
console.log(`Bundled ${files.length} source files (${(totalBytes / 1024).toFixed(0)} KB) at ${index.revision.slice(0, 7)} into ${relative(repositoryRoot, outputDirectory)}.`)
