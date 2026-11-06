// One run of a unit's 试一试 demo: creates the .NET WebAssembly runtime (built by docs:runtime into
// runtime/), hands Roslyn every reference in runtime/refs/manifest.json, loads the course assemblies
// into the runtime so the compiled demo can call them, compiles, executes, and reports each stage.
// A port of the patterns site's compiler-worker.js; the page ends the worker when the run settles.
import { dotnet } from "./runtime/_framework/dotnet.js";

self.onmessage = async ({ data }) => {
  try {
    const runtime = await dotnet.create();
    const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    const compiler = exports.BrowserCompiler;
    const base = new URL("./runtime/refs/", import.meta.url);
    const manifest = await fetch(new URL("manifest.json", base)).then((response) => {
      if (!response.ok) throw new Error("无法加载编译引用清单。");
      return response.json();
    });
    const bytes = new Map(await Promise.all(manifest.references.map(async (name) => {
      const response = await fetch(new URL(name, base));
      if (!response.ok) throw new Error(`无法加载 ${name}`);
      return [name, new Uint8Array(await response.arrayBuffer())];
    })));
    for (const name of manifest.references) compiler.AddReference(bytes.get(name));
    for (const name of manifest.assemblies) compiler.LoadAssembly(bytes.get(name));
    self.postMessage({ stage: "compile" });
    const compilation = JSON.parse(compiler.Compile(data.source, data.checks));
    if (!compilation.success) { self.postMessage({ stage: "done", compilation }); return; }
    self.postMessage({ stage: "execute", compilation });
    const result = JSON.parse(compiler.Execute());
    self.postMessage({ stage: "done", compilation, result });
  } catch (error) {
    self.postMessage({ stage: "error", error: error.message ?? String(error) });
  }
};
