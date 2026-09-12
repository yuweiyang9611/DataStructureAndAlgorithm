import { test } from 'node:test'
import assert from 'node:assert/strict'
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'
import { spawnSync } from 'node:child_process'

const script = resolve('tools/benchmark_report.mjs')
function report(median = 2000, runtime = '.NET 10.0.12') {
  return {
    HostEnvironmentInfo: { OsVersion:'Windows 11', ProcessorName:'test CPU', RuntimeVersion:runtime, Architecture:'X64', DotNetCliVersion:'10.0.401', BenchmarkDotNetVersion:'0.15.8' },
    Benchmarks: [20,100].map(count => ({
      FullName:'Bench.SchedulingScalingScenarioBenchmarks.Greedy(TaskCount: '+count+')',
      Type:'SchedulingScalingScenarioBenchmarks', Method:'Greedy', Parameters:'TaskCount='+count,
      DisplayInfo:'SchedulingScalingScenarioBenchmarks.Greedy: ShortRun(IterationCount=3) [TaskCount='+count+']',
      Statistics:{Median:median,N:3}, Memory:{BytesAllocatedPerOperation:128}
    }))
  }
}
function fixture() {
  const root = mkdtempSync(join(tmpdir(),'dsa-report-test-'))
  const current=join(root,'current'), previous=join(root,'previous')
  mkdirSync(current);mkdirSync(previous)
  const write=(folder:string,data:unknown)=>writeFileSync(join(folder,'test-report-full-compressed.json'),JSON.stringify(data))
  const run=(group='Scheduling')=>spawnSync(process.execPath,[script,current,previous],{
    encoding:'utf8', env:{...process.env,BENCHMARK_GROUP:group,GITHUB_STEP_SUMMARY:''}
  })
  return {current,previous,write,run,summary:()=>readFileSync(join(current,'summary.md'),'utf8'),cleanup:()=>rmSync(root,{recursive:true,force:true})}
}
test('benchmark history compares equal environments and reports slower measurements',()=>{
  const f=fixture()
  try {
    f.write(f.current,report(2000));f.write(f.previous,report(1000))
    assert.equal(f.run().status,0)
    assert.match(f.summary(),/100\.0%/)
    f.write(f.previous,report(1000,'.NET 10.0.11'))
    assert.equal(f.run().status,0)
    assert.match(f.summary(),/no comparable baseline/)
    assert.doesNotMatch(f.summary(),/100\.0%/)
  } finally {f.cleanup()}
})
test('benchmark gate rejects duplicate, missing and unexpected parameter combinations',()=>{
  const f=fixture()
  try {
    const duplicate=report();duplicate.Benchmarks[1]=duplicate.Benchmarks[0]
    f.write(f.current,duplicate)
    assert.match(f.run().stderr,/duplicate benchmark combination/)
    const missing=report();missing.Benchmarks.pop();f.write(f.current,missing)
    assert.match(f.run().stderr,/Missing benchmark combinations/)
    const unexpected=report();unexpected.Benchmarks[1].Parameters='TaskCount=101';f.write(f.current,unexpected)
    assert.match(f.run().stderr,/Unexpected or duplicate/)
    f.write(f.current,report())
    assert.match(f.run('').stderr,/Missing benchmark combinations/)
    assert.match(f.run('Unknown').stderr,/Unknown benchmark group/)
  } finally {f.cleanup()}
})
test('benchmark gate rejects absent measurements and incomplete environment metadata',()=>{
  const f=fixture()
  try {
    const invalid=report();invalid.Benchmarks[0].Statistics.N=0;f.write(f.current,invalid)
    assert.match(f.run().stderr,/no measurements/)
    const noEnvironment=report();noEnvironment.HostEnvironmentInfo.DotNetCliVersion='';f.write(f.current,noEnvironment)
    assert.match(f.run().stderr,/environment metadata is incomplete/)
  } finally {f.cleanup()}
})
