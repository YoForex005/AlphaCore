using TraderIntelligence.Domain.Volume;

var def = new VolumeConverter();
var mgr = VolumeConverter.Manager;
var ext = VolumeConverter.Extended;

Console.WriteLine("ctor_default_Scale=" + def.Scale);
Console.WriteLine("Manager.Scale=" + mgr.Scale);
Console.WriteLine("Extended.Scale=" + ext.Scale);
Console.WriteLine("ManagerVolumeScale=" + VolumeConverter.ManagerVolumeScale);
Console.WriteLine("ExtendedVolumeScale=" + VolumeConverter.ExtendedVolumeScale);
Console.WriteLine("HundredthsScale=" + VolumeConverter.HundredthsScale);
Console.WriteLine("default_eq_Manager=" + (def.Scale == mgr.Scale));
Console.WriteLine("default_eq_Extended=" + (def.Scale == ext.Scale));
Console.WriteLine("Manager.ToLots(10000)=" + mgr.ToLots(10000));
Console.WriteLine("Manager.ToLots(1000)=" + mgr.ToLots(1000));
Console.WriteLine("Manager.ToLots(100)=" + mgr.ToLots(100));
Console.WriteLine("Extended.ToLots(10000)=" + ext.ToLots(10000));
Console.WriteLine("Extended.ToLots(100000000)=" + ext.ToLots(100000000));
Console.WriteLine("default.ToLots(10000)=" + def.ToLots(10000));
Console.WriteLine("default.ToNative(1)=" + def.ToNative(1m));
Console.WriteLine("Manager.ToNative(1)=" + mgr.ToNative(1m));
Console.WriteLine("Extended.ToNative(1)=" + ext.ToNative(1m));
Console.WriteLine("ratio_ext_div_mgr=" + (ext.ToNative(1m) / mgr.ToNative(1m)));
Console.WriteLine("blast_if_A81_default_on_classic_10000=" + (10000m / VolumeConverter.ExtendedVolumeScale));
Console.WriteLine("blast_if_B14_default_on_classic_10000=" + (10000m / VolumeConverter.ManagerVolumeScale));
