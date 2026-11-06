import { demoChecks, demoSourceLimit } from '../data/demos.ts'

/**
 * Runs a unit's 试一试 demo in the browser on the course's Roslyn + .NET WebAssembly runtime, a port
 * of the patterns site's demo-runner.js. Each run is one module worker (public/compiler-worker.js)
 * that loads the runtime, compiles the demo with `demoChecks` as its second tree and executes
 * ExerciseChecks.Run; the worker is ended when the run settles, so a runaway demo cannot keep the
 * page's thread. Browser only: nothing here touches the window at import time.
 */
export type RunStage = 'load' | 'compile' | 'execute'

export interface RunResult {
  ok: boolean
  stage: RunStage | 'stopped'
  output: string
  diagnostics: string[]
  error: string | null
  timedOut?: boolean
  stopped?: boolean
}

export interface RunHandle {
  done: Promise<RunResult>
  stop: () => void
}

/** Timeouts in milliseconds and the stage words the panel shows, as on the patterns site. */
export const stages: Record<RunStage, [number, string]> = { load: [60_000, '加载运行时'], compile: [30_000, '编译'], execute: [5_000, '执行'] }

interface WorkerMessage {
  stage: 'compile' | 'execute' | 'done' | 'error'
  error?: string
  compilation?: { success: boolean; diagnostics: { severity: string; line: number; column: number; message: string }[] }
  result?: { output: string; outputTruncated: boolean; error: string | null }
}

export function runDemo({ source, workerUrl, onStage }: { source: string; workerUrl: string; onStage?: (stage: RunStage, label: string) => void }): RunHandle {
  let worker: Worker | null = null
  let timer = 0
  let settle: (result: RunResult) => void = () => {}
  const done = new Promise<RunResult>((resolve) => { settle = resolve })

  function finish(result: RunResult) {
    window.clearTimeout(timer)
    worker?.terminate()
    worker = null
    settle(result)
  }

  function stage(name: RunStage) {
    window.clearTimeout(timer)
    const [timeout, label] = stages[name]
    onStage?.(name, label)
    timer = window.setTimeout(() => finish({ ok: false, timedOut: true, stage: name, output: '', diagnostics: [], error: `${label}超时（${timeout / 1000} 秒），已停止。` }), timeout)
  }

  if (new TextEncoder().encode(source).length > demoSourceLimit) {
    finish({ ok: false, stage: 'compile', output: '', diagnostics: [], error: '源码超过 100 KB，已拒绝运行。' })
    return { done, stop: () => {} }
  }

  stage('load')
  try {
    const active = new Worker(workerUrl, { type: 'module' })
    worker = active
    active.onerror = (event) => { event.preventDefault(); finish({ ok: false, stage: 'load', output: '', diagnostics: [], error: `运行失败：${event.message}` }) }
    active.onmessage = ({ data }: MessageEvent<WorkerMessage>) => {
      if (worker !== active) return
      if (data.stage === 'compile' || data.stage === 'execute') { stage(data.stage); return }
      if (data.stage === 'error') { finish({ ok: false, stage: 'load', output: '', diagnostics: [], error: `运行失败：${data.error}` }); return }
      if (data.stage !== 'done') return
      const diagnostics = (data.compilation?.diagnostics ?? []).map((item) => `${item.severity} (${item.line},${item.column}): ${item.message}`)
      if (!data.compilation?.success) { finish({ ok: false, stage: 'compile', output: '', diagnostics, error: '编译失败。' }); return }
      const result = data.result
      const output = (result?.output ?? '') + (result?.outputTruncated ? '\n[输出已达到 64 KB，后续输出已截断]' : '')
      finish({ ok: Boolean(result && !result.error), stage: 'execute', output, diagnostics, error: result?.error ? `运行异常：${result.error}` : null })
    }
    active.postMessage({ source, checks: demoChecks })
  } catch (error) {
    finish({ ok: false, stage: 'load', output: '', diagnostics: [], error: `无法启动运行环境：${error instanceof Error ? error.message : String(error)}` })
  }

  return { done, stop: () => finish({ ok: false, stopped: true, stage: 'stopped', output: '', diagnostics: [], error: '已停止。' }) }
}
