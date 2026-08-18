using System.Reflection;
using MetaQuotes.MT5CommonAPI;
using MetaQuotes.MT5ManagerAPI;

Console.WriteLine($"tf={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"os={System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
Console.WriteLine($"arch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
try
{
    var t = typeof(SMTManagerAPIFactory);
    Console.WriteLine($"typeof(SMTManagerAPIFactory)={t.AssemblyQualifiedName}");
    Console.WriteLine($"asm.Location={t.Assembly.Location}");
    Console.WriteLine($"asm.ImageRuntimeVersion={t.Assembly.ImageRuntimeVersion}");
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
        Console.WriteLine($"  method {m}");
    Console.WriteLine($"ManagerAPIVersion={SMTManagerAPIFactory.ManagerAPIVersion}");
}
catch (Exception ex)
{
    Console.WriteLine($"TYPEOF FAIL {ex.GetType().FullName}: {ex}");
}

try
{
    var res = SMTManagerAPIFactory.Initialize(null);
    Console.WriteLine($"Initialize(null)={res} ({(int)res})");
    if (res == MTRetCode.MT_RET_OK)
    {
        var mgr = SMTManagerAPIFactory.CreateManager(SMTManagerAPIFactory.ManagerAPIVersion, out var cres);
        Console.WriteLine($"CreateManager={cres} mgrNull={mgr is null}");
        mgr?.Dispose();
        SMTManagerAPIFactory.Shutdown();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"INIT FAIL {ex.GetType().FullName}: {ex}");
}
