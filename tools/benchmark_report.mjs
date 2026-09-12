import { existsSync, readdirSync, readFileSync, writeFileSync, appendFileSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { execFileSync } from 'node:child_process'

const root = resolve(process.argv[2] ?? 'BenchmarkDotNet.Artifacts')
const previousRoot = process.argv[3] ? resolve(process.argv[3]) : null
function reports(path) {
  if (!path || !existsSync(path)) return []
  return readdirSync(path, { withFileTypes: true }).flatMap(entry =>
    entry.isDirectory() ? reports(join(path, entry.name)) :
      entry.name.endsWith('-report-full-compressed.json') ? [join(path, entry.name)] : [])
}
function rows(path, strict) {
  return reports(path).flatMap(file => {
    const report = JSON.parse(readFileSync(file, 'utf8'))
    return report.Benchmarks.map(benchmark => {
      if (!Number.isFinite(benchmark.Statistics?.Median) || benchmark.Statistics.Median <= 0 || !Number.isSafeInteger(benchmark.Statistics.N) || benchmark.Statistics.N < 1) {
        if (strict) throw new Error('Benchmark produced no measurements: ' + benchmark.FullName)
        return null
      }
      const env = report.HostEnvironmentInfo
      const environmentFields = ['OsVersion', 'ProcessorName', 'RuntimeVersion', 'Architecture', 'DotNetCliVersion', 'BenchmarkDotNetVersion']
      if (!env || environmentFields.some(field => typeof env[field] !== 'string' || !env[field].trim())) {
        if (strict) throw new Error('Benchmark environment metadata is incomplete: ' + file)
        return null
      }
      const job = benchmark.DisplayInfo.match(/:\s*([^([]+)\(/)?.[1]?.trim() ?? benchmark.DisplayInfo
      const jobDefinition = benchmark.DisplayInfo.slice(benchmark.DisplayInfo.indexOf(': ') + 2).split(' [')[0]
      const environment = [env.OsVersion, env.ProcessorName, env.RuntimeVersion, env.Architecture, env.DotNetCliVersion, env.BenchmarkDotNetVersion]
      return {
        key: JSON.stringify([benchmark.FullName, jobDefinition, environment]), type: benchmark.Type,
        method: benchmark.Method, parameters: benchmark.Parameters, job, jobDefinition,
        environment, medianNs: benchmark.Statistics.Median,
        allocatedBytes: benchmark.Memory?.BytesAllocatedPerOperation ?? null,
        samples: benchmark.Statistics.N
      }
    }).filter(Boolean)
  })
}
const current = rows(root, true)
if (!current.length) throw new Error('No benchmark reports found in ' + root)
const specifications = {
  SearchScalingScenarioBenchmarks: { methods: ['BuildIndex', 'ColdQuery', 'CachedQuery', 'Bm25Query'], parameters: { DocumentCount: [100, 1000, 10000] } },
  DeliveryScalingScenarioBenchmarks: { methods: ['FirstPlan', 'CachedRoutes'], parameters: { LocationCount: [20, 100], OutDegree: [2, 8] } },
  SchedulingScalingScenarioBenchmarks: { methods: ['Greedy'], parameters: { TaskCount: [20, 100] } },
  ExactScalingScenarioBenchmarks: { methods: ['ExactComparison'], parameters: { TaskCount: [4, 6, 8] } },
  StorageScalingScenarioBenchmarks: { methods: ['FullWalRecovery', 'SnapshotRecovery', 'Checkpoint'], parameters: { LiveKeys: [100, 1000], HistoryPerKey: [1, 10] } },
  StorageReadScalingScenarioBenchmarks: { methods: ['Read'], parameters: { LiveKeys: [100, 1000], Hot: ['False', 'True'] } }
}
function parameterKey(parameters) { return parameters.split('&').sort().join('&') }
function combinationKey(type, method, parameters) { return JSON.stringify([type, method, parameterKey(parameters)]) }
const group = process.env.BENCHMARK_GROUP
if (group && !['Search', 'Delivery', 'Scheduling', 'Exact', 'Storage'].includes(group)) throw new Error('Unknown benchmark group: ' + group)
const types = Object.keys(specifications).filter(type => !group || type.startsWith(group))
const missing = new Set()
for (const type of types) {
  const definition = specifications[type]
  let combinations = ['']
  for (const [name, values] of Object.entries(definition.parameters)) {
    combinations = combinations.flatMap(prefix => values.map(value => (prefix ? prefix + '&' : '') + name + '=' + value))
  }
  for (const method of definition.methods) for (const parameters of combinations) missing.add(combinationKey(type, method, parameters))
}
for (const row of current) {
  const key = combinationKey(row.type, row.method, row.parameters)
  if (!missing.delete(key)) throw new Error('Unexpected or duplicate benchmark combination: ' + key)
}
if (missing.size) throw new Error('Missing benchmark combinations: ' + [...missing].join(', '))
const old = new Map(rows(previousRoot, false).map(row => [row.key, row]))
let commit = process.env.GITHUB_SHA ?? 'unknown'
let dirty = false
try {
  commit = execFileSync('git', ['-c', 'safe.directory=' + process.cwd(), 'rev-parse', 'HEAD'], { encoding: 'utf8' }).trim()
  dirty = Boolean(execFileSync('git', ['-c', 'safe.directory=' + process.cwd(), 'status', '--porcelain'], { encoding: 'utf8' }).trim())
} catch { /* A source archive can still produce a valid measurement report. */ }
const lines = [
  '# Scenario benchmark evidence', '',
  'Commit: ' + commit + (dirty ? ' (working tree modified)' : ''), '',
  'Each comparison requires identical method, parameters, job, SDK, runtime, OS and CPU. Dry is an execution check, not a performance conclusion.', '',
  '| Method | Parameters | Job | Median (ns) | Allocated (B) | Previous median change |',
  '| --- | --- | --- | ---: | ---: | ---: |'
]
for (const row of current) {
  const baseline = old.get(row.key)
  const delta = baseline?.medianNs > 0 ? ((row.medianNs / baseline.medianNs - 1) * 100).toFixed(1) + '%' : 'no comparable baseline'
  lines.push('| ' + row.type + '.' + row.method + ' | ' + row.parameters + ' | ' + row.job + ' | ' + row.medianNs.toFixed(1) + ' | ' + (row.allocatedBytes ?? 'n/a') + ' | ' + delta + ' |')
}
lines.push('', '## Recovery comparison', '', '| Parameters | Job | Full WAL / snapshot median ratio |', '| --- | --- | ---: |')
for (const full of current.filter(row => row.type === 'StorageScalingScenarioBenchmarks' && row.method === 'FullWalRecovery')) {
  const snapshot = current.find(row => row.type === full.type && row.method === 'SnapshotRecovery' && parameterKey(row.parameters) === parameterKey(full.parameters) && row.jobDefinition === full.jobDefinition && JSON.stringify(row.environment) === JSON.stringify(full.environment))
  if (snapshot) lines.push('| ' + full.parameters + ' | ' + full.job + ' | ' + (full.medianNs / snapshot.medianNs).toFixed(2) + ' |')
}
const summary = lines.join('\n') + '\n'
writeFileSync(join(root, 'summary.md'), summary)
writeFileSync(join(root, 'evidence.json'), JSON.stringify({ commit, dirty, capturedAt: new Date().toISOString(), rows: current }, null, 2) + '\n')
if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, summary)
console.log('Validated ' + current.length + ' measurements. Summary: ' + join(root, 'summary.md'))
