using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

static void DumpPeAndMetadata(string path, TextWriter w)
{
    w.WriteLine($"=== METADATA {path} ===");
    using var fs = File.OpenRead(path);
    using var pe = new PEReader(fs);
    w.WriteLine($"HasMetadata={pe.HasMetadata}");
    w.WriteLine($"IsEntireImageAvailable={pe.IsEntireImageAvailable}");
    w.WriteLine($"IsLoadedImage={pe.IsLoadedImage}");
    w.WriteLine($"PEHeaders.CoffHeader.Machine={pe.PEHeaders.CoffHeader.Machine}");
    w.WriteLine($"PEHeaders.CoffHeader.Characteristics={pe.PEHeaders.CoffHeader.Characteristics}");
    w.WriteLine($"PEHeaders.IsCoffOnly={pe.PEHeaders.IsCoffOnly}");
    w.WriteLine($"PEHeaders.IsDll={pe.PEHeaders.IsDll}");
    w.WriteLine($"PEHeaders.IsExe={pe.PEHeaders.IsExe}");
    w.WriteLine($"PEHeaders.IsConsoleApplication={pe.PEHeaders.IsConsoleApplication}");
    if (pe.PEHeaders.PEHeader is { } ph)
    {
        w.WriteLine($"Magic={ph.Magic}");
        w.WriteLine($"Subsystem={ph.Subsystem}");
        w.WriteLine($"DllCharacteristics={ph.DllCharacteristics}");
        w.WriteLine($"ImageBase=0x{ph.ImageBase:X}");
    }
    var cor = pe.PEHeaders.CorHeader;
    if (cor is null)
    {
        w.WriteLine("CorHeader=null (native, no CLR)");
        return;
    }
    w.WriteLine($"CorHeader.MajorRuntimeVersion={cor.MajorRuntimeVersion}");
    w.WriteLine($"CorHeader.MinorRuntimeVersion={cor.MinorRuntimeVersion}");
    w.WriteLine($"CorHeader.Flags={cor.Flags}");
    w.WriteLine($"CorHeader.EntryPointTokenOrRelativeVirtualAddress=0x{cor.EntryPointTokenOrRelativeVirtualAddress:X8}");
    w.WriteLine($"CorHeader.MetadataDirectory.Size={cor.MetadataDirectory.Size}");
    w.WriteLine($"ILONLY={(cor.Flags & CorFlags.ILOnly) != 0}");
    w.WriteLine($"Requires32Bit={(cor.Flags & CorFlags.Requires32Bit) != 0}");
    w.WriteLine($"ILLibrary={(cor.Flags & CorFlags.ILLibrary) != 0}");
    w.WriteLine($"StrongNameSigned={(cor.Flags & CorFlags.StrongNameSigned) != 0}");
    w.WriteLine($"NativeEntryPoint={(cor.Flags & CorFlags.NativeEntryPoint) != 0}");
    w.WriteLine($"Prefers32Bit={(cor.Flags & CorFlags.Prefers32Bit) != 0}");

    if (!pe.HasMetadata)
    {
        w.WriteLine("No metadata.");
        return;
    }

    var md = pe.GetMetadataReader();
    var asm = md.GetAssemblyDefinition();
    w.WriteLine($"Assembly.Name={md.GetString(asm.Name)}");
    w.WriteLine($"Assembly.Version={asm.Version}");
    w.WriteLine($"Assembly.Culture={md.GetString(asm.Culture)}");
    w.WriteLine($"Assembly.Flags={asm.Flags}");
    w.WriteLine($"Assembly.HashAlgorithm={asm.HashAlgorithm}");
    w.WriteLine($"Assembly.PublicKey={Hex(md.GetBlobBytes(asm.PublicKey))}");

    w.WriteLine("-- AssemblyReferences --");
    foreach (var h in md.AssemblyReferences)
    {
        var r = md.GetAssemblyReference(h);
        w.WriteLine($"  {md.GetString(r.Name)} {r.Version} flags={r.Flags} pk={Hex(md.GetBlobBytes(r.PublicKeyOrToken))}");
    }

    w.WriteLine("-- Module --");
    var mod = md.GetModuleDefinition();
    w.WriteLine($"  Name={md.GetString(mod.Name)} Mvid={md.GetGuid(mod.Mvid)}");

    w.WriteLine("-- CustomAttributes on assembly --");
    foreach (var ah in asm.GetCustomAttributes())
    {
        var ca = md.GetCustomAttribute(ah);
        string typeName = "?";
        if (ca.Constructor.Kind == HandleKind.MemberReference)
        {
            var mr = md.GetMemberReference((MemberReferenceHandle)ca.Constructor);
            if (mr.Parent.Kind == HandleKind.TypeReference)
            {
                var tr = md.GetTypeReference((TypeReferenceHandle)mr.Parent);
                typeName = $"{md.GetString(tr.Namespace)}.{md.GetString(tr.Name)}";
            }
        }
        else if (ca.Constructor.Kind == HandleKind.MethodDefinition)
        {
            var mdh = md.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor);
            var td = md.GetTypeDefinition(mdh.GetDeclaringType());
            typeName = $"{md.GetString(td.Namespace)}.{md.GetString(td.Name)}";
        }
        w.WriteLine($"  [{typeName}] blob={Hex(md.GetBlobBytes(ca.Value))}");
    }

    w.WriteLine("-- TypeDefinitions (exported / public-ish) --");
    int typeCount = 0, publicCount = 0;
    foreach (var th in md.TypeDefinitions)
    {
        var td = md.GetTypeDefinition(th);
        typeCount++;
        var vis = td.Attributes & TypeAttributes.VisibilityMask;
        bool pub = vis is TypeAttributes.Public or TypeAttributes.NestedPublic;
        if (pub) publicCount++;
        string ns = md.GetString(td.Namespace);
        string name = md.GetString(td.Name);
        if (pub || name.Contains("Manager", StringComparison.OrdinalIgnoreCase) || name.Contains("Factory", StringComparison.OrdinalIgnoreCase))
        {
            w.WriteLine($"  {(pub ? "PUB" : "   ")} {ns}.{name} attrs={td.Attributes}");
        }
    }
    w.WriteLine($"TypeDefinitionCount={typeCount} PublicOrNestedPublic={publicCount}");
}

static void TryLoadContext(string path, TextWriter w)
{
    w.WriteLine($"=== MetadataLoadContext {path} ===");
    try
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var paths = Directory.GetFiles(runtimeDir, "*.dll").ToList();
        paths.Add(path);
        var sibling = Path.Combine(Path.GetDirectoryName(path)!, "MetaQuotes.MT5CommonAPI64.dll");
        if (File.Exists(sibling)) paths.Add(sibling);
        using var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths));
        var asm = ctx.LoadFromAssemblyPath(path);
        w.WriteLine($"FullName={asm.FullName}");
        w.WriteLine($"Location={asm.Location}");
        w.WriteLine($"ImageRuntimeVersion={asm.ImageRuntimeVersion}");
        w.WriteLine($"IsDynamic={asm.IsDynamic}");
        w.WriteLine($"EntryPoint={asm.EntryPoint}");
        foreach (var an in asm.GetReferencedAssemblies())
            w.WriteLine($"  ref {an.FullName}");
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            w.WriteLine($"GetTypes ReflectionTypeLoadException: {ex.Message}");
            foreach (var le in ex.LoaderExceptions ?? Array.Empty<Exception>())
                w.WriteLine($"  loader: {le?.GetType().Name}: {le?.Message}");
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }
        w.WriteLine($"TypesLoaded={types.Length}");
        foreach (var t in types.OrderBy(t => t.FullName))
        {
            if (t.IsPublic)
                w.WriteLine($"  public {t.FullName}");
        }
        var factory = types.FirstOrDefault(t => t.Name == "SMTManagerAPIFactory");
        if (factory is not null)
        {
            w.WriteLine("-- SMTManagerAPIFactory members --");
            foreach (var m in factory.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                w.WriteLine($"  {m.MemberType} {m}");
        }
    }
    catch (Exception ex)
    {
        w.WriteLine($"FAIL {ex.GetType().FullName}: {ex}");
    }
}

static void TryRuntimeLoad(string path, TextWriter w)
{
    w.WriteLine($"=== Runtime Assembly.LoadFrom {path} ===");
    w.WriteLine($"Runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
    w.WriteLine($"OS={System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
    w.WriteLine($"ProcessArch={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
    try
    {
        var asm = Assembly.LoadFrom(path);
        w.WriteLine($"OK FullName={asm.FullName}");
        w.WriteLine($"ImageRuntimeVersion={asm.ImageRuntimeVersion}");
        w.WriteLine($"IsCollectible={asm.IsCollectible}");
        Type[] types;
        try { types = asm.GetExportedTypes(); }
        catch (Exception ex)
        {
            w.WriteLine($"GetExportedTypes FAIL {ex.GetType().Name}: {ex.Message}");
            types = Array.Empty<Type>();
        }
        foreach (var t in types.Take(40))
            w.WriteLine($"  exported {t.FullName}");
        w.WriteLine($"ExportedTypeCount={types.Length}");
    }
    catch (Exception ex)
    {
        w.WriteLine($"FAIL {ex.GetType().FullName}: {ex.Message}");
        w.WriteLine(ex.ToString());
    }
}

var manager = args.Length > 0
    ? args[0]
    : @"D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll";
var common = Path.Combine(Path.GetDirectoryName(manager)!, "MetaQuotes.MT5CommonAPI64.dll");

var sb = new StringBuilder();
using var w = new StringWriter(sb);
w.WriteLine($"utc={DateTime.UtcNow:o}");
w.WriteLine($"tf={typeof(object).Assembly.ImageRuntimeVersion} {typeof(object).Assembly.GetName()}");
DumpPeAndMetadata(manager, w);
w.WriteLine();
DumpPeAndMetadata(common, w);
w.WriteLine();
TryLoadContext(manager, w);
w.WriteLine();
TryRuntimeLoad(manager, w);
w.WriteLine();
TryRuntimeLoad(common, w);
Console.Write(sb.ToString());
var outPath = Path.Combine(AppContext.BaseDirectory, "inspect_stdout.txt");
File.WriteAllText(@"D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\inspect_stdout.txt", sb.ToString());
File.WriteAllText(outPath, sb.ToString());
