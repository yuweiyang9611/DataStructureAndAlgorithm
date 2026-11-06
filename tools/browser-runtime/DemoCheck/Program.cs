// Runs every unit demo through the exercise engine on the desktop: the same Roslyn compile against
// refs/, the same course assemblies loaded into the runtime, the same ExerciseChecks entry, the same
// captured Console output the browser shows. Exit code 1 when any demo fails to compile or throws,
// so docs:runtime cannot ship a broken 试一试 tab.
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using ExerciseEngine;

if (args.Length < 3)
{
    Console.Error.WriteLine("用法: DemoCheck <refs 目录> <checks 文件> <demo.cs> [demo.cs ...] [--print]");
    return 2;
}

var referenceDirectory = Path.GetFullPath(args[0]);
var checks = File.ReadAllText(args[1]);
var print = args.Contains("--print", StringComparer.Ordinal);
var demos = args.Skip(2).Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToArray();
var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(referenceDirectory, "manifest.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("refs/manifest.json is empty.");

var compiler = new ExerciseCompiler();
foreach (var name in manifest.References) compiler.AddReference(File.ReadAllBytes(Path.Combine(referenceDirectory, name)));
// The course assemblies go into the default load context exactly as the browser host loads them,
// and the compiled demo finds them there by name when it executes.
var loaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
foreach (var name in manifest.Assemblies)
{
    var assembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(referenceDirectory, name))));
    loaded[assembly.GetName().Name ?? name] = assembly;
}
AssemblyLoadContext.Default.Resolving += (_, name) => name.Name is { } simple && loaded.TryGetValue(simple, out var assembly) ? assembly : null;

var failures = 0;
foreach (var path in demos)
{
    var label = Path.GetFileNameWithoutExtension(path);
    var compilation = compiler.Compile(File.ReadAllText(path), checks);
    if (!compilation.Success)
    {
        failures++;
        Console.WriteLine($"== {label}: 编译失败");
        foreach (var diagnostic in compilation.Diagnostics) Console.WriteLine($"   {diagnostic.Severity} ({diagnostic.Line},{diagnostic.Column}): {diagnostic.Message}");
        continue;
    }
    var result = compiler.Execute();
    if (result.Error is not null)
    {
        failures++;
        Console.WriteLine($"== {label}: 运行异常 {result.Error}");
        Console.WriteLine(result.Output);
        continue;
    }
    var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
    Console.WriteLine($"== {label}: {lines} 行输出{(result.OutputTruncated ? "（已截断）" : "")}");
    if (print) Console.WriteLine(result.Output.TrimEnd());
}

Console.WriteLine(failures == 0 ? $"{demos.Length} 个 demo 都能在引擎里编译并运行。" : $"{failures} 个 demo 失败。");
return failures == 0 ? 0 : 1;

sealed record Manifest(string[] References, string[] Assemblies);
