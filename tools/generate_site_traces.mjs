import { existsSync, mkdirSync, writeFileSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname, join, relative } from 'node:path'

const scriptDirectory = dirname(fileURLToPath(import.meta.url))
const repositoryRoot = join(scriptDirectory, '..')
const outputDirectory = join(repositoryRoot, 'docs', 'public', 'traces')

const scenarios = [
  {
    id: 'gcd',
    title: '欧几里得最大公约数',
    category: '数论',
    level: '入门',
    summary: '观察被除数、除数与余数如何收缩，直到余数为零。',
    project: 'DataStructureAndAlgorithm.P3Demo',
    args: ['gcd', 'json'],
    source: 'DataStructureAndAlgorithm/NumberTheory/NumberTheoryAlgorithms.cs',
    stateHints: { sequence: [], range: [], graph: [] }
  },
  {
    id: 'radix',
    title: 'LSD 基数排序',
    category: '排序',
    level: '入门',
    summary: '逐轮处理固定位段，并保持此前低位的稳定顺序。',
    project: 'DataStructureAndAlgorithm.Demo',
    args: ['radix'],
    source: 'DataStructureAndAlgorithm/Sorting/NonComparisonSortAlgorithms.cs',
    stateHints: { sequence: ['values'], range: ['processedBits'], graph: [] }
  },
  {
    id: 'dijkstra',
    title: 'Dijkstra 最短路',
    category: '图算法',
    level: '进阶',
    summary: '跟踪出队、松弛与过期候选，理解非负权前提下的最短路不变量。',
    project: 'DataStructureAndAlgorithm.Demo',
    args: ['dijkstra'],
    source: 'DataStructureAndAlgorithm/Graph/WeightedGraphAlgorithms.cs',
    stateHints: { sequence: [], range: [], graph: ['source', 'from', 'to', 'vertex', 'distance'] }
  },
  {
    id: 'matrix-chain',
    title: '矩阵链区间动态规划',
    category: '动态规划',
    level: '进阶',
    summary: '比较区间内的分割点，保留乘法次数更少的方案。',
    project: 'DataStructureAndAlgorithm.P3Demo',
    args: ['matrix-chain', 'json'],
    source: 'DataStructureAndAlgorithm/DynamicProgramming/P3DynamicProgrammingAlgorithms.cs',
    stateHints: { sequence: [], range: ['interval'], graph: [] },
    inputMetadata: { dimensions: [40, 20, 30, 10, 30] }
  },
  {
    id: 'red-black',
    title: '左倾红黑树更新',
    category: '有序结构',
    level: '高级',
    summary: '观察插入、删除、旋转与颜色翻转；事件只描述局部操作，不伪造完整树状态。',
    project: 'DataStructureAndAlgorithm.Demo',
    args: ['red-black'],
    source: 'DataStructureAndAlgorithm/Tree/RedBlackTree.cs',
    stateHints: { sequence: [], range: [], graph: ['oldRoot', 'newRoot', 'node', 'nodeColor'] }
  },
  {
    id: 'project-scheduling',
    title: '项目调度时间线',
    category: '综合项目',
    level: '高级',
    summary: '从 DAG、关键路径和拓扑序推进到资源受限的任务落位。',
    project: 'DataStructureAndAlgorithm.Scenarios.ProjectScheduling',
    args: ['--trace', 'json'],
    source: 'DataStructureAndAlgorithm.Scenarios.ProjectScheduling/ProjectScheduler.cs',
    stateHints: { sequence: ['path', 'order'], range: ['start', 'end'], graph: ['id', 'demand'] }
  }
]

function runDotnet(args, capture = false) {
  const result = spawnSync('dotnet', args, {
    cwd: repositoryRoot,
    encoding: 'utf8',
    maxBuffer: 4 * 1024 * 1024,
    stdio: capture ? ['ignore', 'pipe', 'pipe'] : 'inherit'
  })

  if (result.status !== 0) {
    if (capture && result.stderr) process.stderr.write(result.stderr)
    throw new Error(`dotnet ${args.join(' ')} failed with exit code ${result.status}`)
  }

  return capture ? result.stdout.trim() : ''
}

function validatePayload(id, payload) {
  if (!payload || typeof payload !== 'object' || !Array.isArray(payload.trace) || !('result' in payload)) {
    throw new Error(`${id}: expected an object containing result and trace`)
  }

  if (payload.trace.length === 0) throw new Error(`${id}: trace is empty`)

  payload.trace.forEach((event, index) => {
    const prefix = `${id}: trace[${index}]`
    if (event.schemaVersion !== 1) throw new Error(`${prefix} uses unsupported schemaVersion`)
    if (event.step !== index + 1) throw new Error(`${prefix} has a non-contiguous step`)
    if (typeof event.algorithm !== 'string' || !event.algorithm) throw new Error(`${prefix} has no algorithm`)
    if (typeof event.operation !== 'string' || !event.operation) throw new Error(`${prefix} has no operation`)
    if (typeof event.description !== 'string') throw new Error(`${prefix} has an invalid description`)
    if (!event.state || typeof event.state !== 'object' || Array.isArray(event.state)) {
      throw new Error(`${prefix} has an invalid state`)
    }
    for (const [key, value] of Object.entries(event.state)) {
      if (typeof value !== 'string') throw new Error(`${prefix}.state.${key} must be a string`)
    }
  })
}

function displayCommand(scenario) {
  return `dotnet run --project ${scenario.project} -c Release -- ${scenario.args.join(' ')}`
}

const scenarioIds = new Set()
for (const scenario of scenarios) {
  if (!/^[a-z0-9-]+$/.test(scenario.id) || scenarioIds.has(scenario.id)) {
    throw new Error(`Invalid or duplicate trace id: ${scenario.id}`)
  }
  scenarioIds.add(scenario.id)
  for (const key of ['title', 'category', 'level', 'summary', 'project', 'source']) {
    if (typeof scenario[key] !== 'string' || !scenario[key].trim()) {
      throw new Error(`${scenario.id}: ${key} must be a non-empty string`)
    }
  }
  if (!existsSync(join(repositoryRoot, scenario.source))) {
    throw new Error(`${scenario.id}: source does not exist: ${scenario.source}`)
  }
  if (!Array.isArray(scenario.args) || scenario.args.some((value) => typeof value !== 'string')) {
    throw new Error(`${scenario.id}: args must be a string array`)
  }
  for (const hint of ['sequence', 'range', 'graph']) {
    if (!Array.isArray(scenario.stateHints?.[hint]) || scenario.stateHints[hint].some((value) => typeof value !== 'string')) {
      throw new Error(`${scenario.id}: stateHints.${hint} must be a string array`)
    }
  }
}

mkdirSync(outputDirectory, { recursive: true })
runDotnet(['build', 'DataStructureAndAlgorithm.slnx', '-c', 'Release', '--nologo'])

const manifest = {
  schemaVersion: 1,
  generatedFrom: 'repository demos at build time',
  traces: []
}

for (const scenario of scenarios) {
  const raw = runDotnet([
    'run', '--project', scenario.project, '-c', 'Release', '--no-build', '--', ...scenario.args
  ], true)

  let payload
  try {
    payload = JSON.parse(raw)
  } catch (error) {
    throw new Error(`${scenario.id}: demo did not return one valid JSON document`, { cause: error })
  }

  validatePayload(scenario.id, payload)
  const file = `${scenario.id}.json`
  writeFileSync(join(outputDirectory, file), `${JSON.stringify(payload)}\n`, 'utf8')

  manifest.traces.push({
    id: scenario.id,
    title: scenario.title,
    category: scenario.category,
    level: scenario.level,
    summary: scenario.summary,
    eventCount: payload.trace.length,
    algorithm: payload.trace[0].algorithm,
    source: scenario.source,
    command: displayCommand(scenario),
    stateHints: scenario.stateHints,
    inputMetadata: scenario.inputMetadata,
    file
  })
}

writeFileSync(join(outputDirectory, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`, 'utf8')
console.log(`Generated ${manifest.traces.length} trace assets in ${relative(repositoryRoot, outputDirectory)}.`)
