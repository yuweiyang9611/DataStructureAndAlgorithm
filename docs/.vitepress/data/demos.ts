import { learningUnits } from './learningUnits.ts'

/**
 * The runnable demo of each learning unit: one short C# file under docs/demos/<unit-id>.cs
 * that prints what the unit is about, using the public API of the course library. The source bundle
 * carries it beside the library files, the unit's panel shows it as the 试一试 tab, and the browser
 * runtime compiles it as the submission with `demoChecks` as the second syntax tree, whose
 * ExerciseChecks.Run entry calls Demo.Run(). tools/build_browser_runtime.mjs runs every demo on the
 * desktop engine with the same two trees, so the files here are the one truth for both.
 */
export const demoDirectory = 'docs/demos'

/** The bundle path of a unit's demo. */
export function demoPath(unitId: string): string {
  return `${demoDirectory}/${unitId}.cs`
}

/** The tab word on the panel. */
export const demoLabel = '试一试'

/** The bundle paths of all 22 demos, in unit order. */
export const demoPaths: string[] = learningUnits.map((unit) => demoPath(unit.id))

/** The namespaces every demo can use without writing them; the same global usings the library projects get. */
export const demoUsings = ['System', 'System.Collections.Generic', 'System.Globalization', 'System.IO', 'System.Linq', 'System.Text', 'System.Threading', 'System.Threading.Tasks']

/** The second syntax tree of a run: the entry the engine looks for, calling the demo. */
export const demoChecks = `${demoUsings.map((name) => `global using ${name};`).join('\n')}
public static class ExerciseChecks
{
    public static string[] Run()
    {
        Demo.Run();
        return new[] { "+demo" };
    }
}
`

/** The reader's copy may grow; the engine refuses more than this. */
export const demoSourceLimit = 100 * 1024
