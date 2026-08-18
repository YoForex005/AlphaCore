using System.Reflection;
using System.Runtime.Loader;

var path = args.Length > 0
    ? args[0]
    : @"D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll";

Console.WriteLine($"tf={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"os={System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
Console.WriteLine($"arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"path={path}");
Console.WriteLine($"exists={File.Exists(path)} len={(File.Exists(path) ? new FileInfo(path).Length : 0)}");

void Try(string label, Action action)
{
    Console.WriteLine($"--- {label} ---");
    try { action(); Console.WriteLine("OK"); }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {ex.GetType().FullName}: {ex.Message}");
        if (ex.InnerException is not null)
            Console.WriteLine($"  inner {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
    }
}

Try("Assembly.LoadFrom", () =>
{
    var a = Assembly.LoadFrom(path);
    Console.WriteLine($"  FullName={a.FullName}");
    Console.WriteLine($"  ImageRuntimeVersion={a.ImageRuntimeVersion}");
    Console.WriteLine($"  Location={a.Location}");
});

Try("AssemblyLoadContext.Default.LoadFromAssemblyPath", () =>
{
    var a = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    Console.WriteLine($"  FullName={a.FullName}");
});

Try("new AssemblyLoadContext(collectible).LoadFromAssemblyPath", () =>
{
    var alc = new AssemblyLoadContext("r021", isCollectible: true);
    var a = alc.LoadFromAssemblyPath(path);
    Console.WriteLine($"  FullName={a.FullName}");
    Console.WriteLine($"  ImageRuntimeVersion={a.ImageRuntimeVersion}");
    try
    {
        var types = a.GetExportedTypes();
        Console.WriteLine($"  ExportedTypes={types.Length}");
        foreach (var t in types.Take(20))
            Console.WriteLine($"    {t.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  GetExportedTypes FAIL {ex.GetType().Name}: {ex.Message}");
    }
    alc.Unload();
});

Try("NativeLibrary.Load", () =>
{
    var handle = System.Runtime.InteropServices.NativeLibrary.Load(path);
    Console.WriteLine($"  handle=0x{handle:X}");
    System.Runtime.InteropServices.NativeLibrary.Free(handle);
});
