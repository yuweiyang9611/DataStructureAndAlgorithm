using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.Json;
using ExerciseEngine;

[SupportedOSPlatform("browser")]
public static partial class BrowserCompiler
{
    private static readonly ExerciseCompiler Compiler = new();
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Dictionary<string, Assembly> Loaded = new(StringComparer.OrdinalIgnoreCase);
    // The worker creates the runtime without running Main, so the hook goes with the type's first use.
    static BrowserCompiler()
    {
        AssemblyLoadContext.Default.Resolving += (_, name) => name.Name is { } simple && Loaded.TryGetValue(simple, out var assembly) ? assembly : null;
    }
    public static void Main() { }
    [JSExport] public static void AddReference(byte[] bytes) => Compiler.AddReference(bytes);
    // The course library and the scenario assemblies: Roslyn sees them through AddReference, and the
    // runtime must hold them too, so the compiled demo can bind to them by name when it executes.
    [JSExport] public static void LoadAssembly(byte[] bytes)
    {
        var assembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(bytes));
        if (assembly.GetName().Name is { } name) Loaded[name] = assembly;
    }
    [JSExport] public static string Compile(string source, string checks) => JsonSerializer.Serialize(Compiler.Compile(source, checks), Json);
    [JSExport] public static string Execute() => JsonSerializer.Serialize(Compiler.Execute(), Json);
}
