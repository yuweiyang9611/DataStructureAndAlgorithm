// Builds the browser runtime that runs a unit's 试一试 demo on the page: the .NET WebAssembly host
// with Roslyn (tools/browser-runtime/BrowserExercises), the reference assemblies Roslyn compiles
// against, and the course library plus the four scenario assemblies built from this checkout, all
// written to docs/public/runtime/ (generated, about 70 MB, never committed). Then every demo under
// docs/demos/ is compiled and run on the desktop engine with the same references and the
// same entry the browser uses, so docs:build never ships a demo that cannot run.
//
// Needs the .NET 10 SDK pinned in tools/browser-runtime/global.json with the wasm-tools and
// wasm-experimental workloads (`dotnet workload install wasm-tools wasm-experimental`).
import { cpSync, existsSync, mkdirSync, readdirSync, rmSync, writeFileSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import { tmpdir } from 'node:os'
import { dirname, join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { demoChecks, demoPaths } from '../docs/.vitepress/data/demos.ts'

const repositoryRoot = join(dirname(fileURLToPath(import.meta.url)), '..')
const runtimeRoot = join(repositoryRoot, 'tools', 'browser-runtime')
const hostDirectory = join(runtimeRoot, 'BrowserExercises')
const destination = join(repositoryRoot, 'docs', 'public', 'runtime')
const dotnet = process.env.DSA_WASM_DOTNET ?? 'dotnet'

// The framework assemblies a demo may name, from the SDK's reference pack: the patterns site's set
// plus what the course's public API reaches (BigInteger, Span helpers, System.Text.Json in the
// scenario records).
const frameworkReferences = [
  'System.Runtime.dll', 'System.Collections.dll', 'System.Linq.dll', 'System.Console.dll',
  'System.Runtime.Extensions.dll', 'System.ObjectModel.dll', 'System.Reflection.dll', 'System.Reflection.Extensions.dll',
  'System.Threading.dll', 'System.Threading.Tasks.dll', 'netstandard.dll',
  'System.Runtime.Numerics.dll', 'System.Memory.dll', 'System.Text.Json.dll', 'System.Collections.Concurrent.dll'
]

// The course assemblies: Roslyn references them and the runtime loads them, so the demo can call them.
const courseProjects = [
  'DataStructureAndAlgorithm',
  'DataStructureAndAlgorithm.Scenarios.CityDelivery',
  'DataStructureAndAlgorithm.Scenarios.MiniSearch',
  'DataStructureAndAlgorithm.Scenarios.MiniStorage',
  'DataStructureAndAlgorithm.Scenarios.ProjectScheduling'
]

function run(args, cwd, failure) {
  const result = spawnSync(dotnet, args, { cwd, stdio: 'inherit' })
  if (result.status !== 0) throw new Error(failure)
}

/** The one directory called `name` under `root`, wherever the SDK put it. */
function findDirectory(root, name) {
  if (!existsSync(root)) return undefined
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue
    const path = join(root, entry.name)
    if (entry.name === name) return path
    const nested = findDirectory(path, name)
    if (nested) return nested
  }
  return undefined
}

// 1. The host: restore against the lock files, publish for browser-wasm.
run(['restore', 'BrowserExercises.csproj', '--locked-mode'], hostDirectory,
  'Browser host restore failed. Install the SDK pinned in tools/browser-runtime/global.json plus the wasm-tools and wasm-experimental workloads.')
run(['publish', 'BrowserExercises.csproj', '-c', 'Release', '--no-restore'], hostDirectory, 'Browser host publish failed.')
const hostOutput = join(hostDirectory, 'bin', 'Release')
const framework = findDirectory(hostOutput, '_framework')
const compilerReferences = findDirectory(hostOutput, 'compiler-refs')
if (!framework || !compilerReferences) throw new Error(`Published browser framework or compiler-refs is missing under ${hostOutput}`)

// 2. The course library and the scenario projects, Release, from this checkout (类库编成引用).
const courseAssemblies = []
for (const project of courseProjects) {
  run(['build', join(project, `${project}.csproj`), '-c', 'Release', '--nologo'], repositoryRoot, `Build of ${project} failed.`)
  const assembly = join(repositoryRoot, project, 'bin', 'Release', 'net10.0', `${project}.dll`)
  if (!existsSync(assembly)) throw new Error(`Built assembly is missing: ${assembly}`)
  courseAssemblies.push(assembly)
}

// 3. The site assets: _framework/ and refs/ with one manifest naming both lists.
rmSync(destination, { recursive: true, force: true })
mkdirSync(destination, { recursive: true })
cpSync(framework, join(destination, '_framework'), { recursive: true })
const referenceDirectory = join(destination, 'refs')
mkdirSync(referenceDirectory, { recursive: true })
for (const name of frameworkReferences) {
  const source = join(compilerReferences, name)
  if (!existsSync(source)) throw new Error(`Reference assembly is missing from the SDK reference pack: ${name}`)
  cpSync(source, join(referenceDirectory, name))
}
const assemblies = courseAssemblies.map((path) => path.split(/[\\/]/).pop())
for (const [index, name] of assemblies.entries()) cpSync(courseAssemblies[index], join(referenceDirectory, name))
const manifest = { version: 1, references: [...frameworkReferences, ...assemblies], assemblies }
writeFileSync(join(referenceDirectory, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`, 'utf8')

// 4. Every demo compiles and runs on the desktop engine, the way the browser will run it.
const checksFile = join(tmpdir(), `dsa-demo-checks-${process.pid}.cs`)
writeFileSync(checksFile, demoChecks, 'utf8')
const demos = demoPaths.map((path) => join(repositoryRoot, path))
for (const demo of demos) if (!existsSync(demo)) throw new Error(`Demo is missing: ${relative(repositoryRoot, demo)}`)
try {
  run(['run', '--project', 'DemoCheck', '-c', 'Release', '--', referenceDirectory, checksFile, ...demos, ...(process.argv.includes('--print') ? ['--print'] : [])],
    runtimeRoot, 'A unit demo failed to compile or run on the exercise engine; see the lines above.')
} finally {
  rmSync(checksFile, { force: true })
}

console.log(`Browser runtime written to ${relative(repositoryRoot, destination)}: ${frameworkReferences.length} framework references, ${assemblies.length} course assemblies, ${demos.length} demos verified.`)
