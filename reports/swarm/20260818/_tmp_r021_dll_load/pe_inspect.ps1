# R021 PE/CLR inspector. Report-only. Does not touch product source.
$ErrorActionPreference = 'Stop'

function Get-PeClrInfo {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $ms = New-Object System.IO.MemoryStream(,$bytes)
    $br = New-Object System.IO.BinaryReader($ms)

    $info = [ordered]@{
        Path = $Path
        Length = $bytes.Length
        Sha256 = (Get-FileHash -Algorithm SHA256 -Path $Path).Hash
        Mz = ''
        PeOffset = 0
        Machine = ''
        Characteristics = ''
        Magic = ''
        Subsystem = ''
        ImageBase = 0
        SizeOfImage = 0
        ComDescriptorRva = 0
        ComDescriptorSize = 0
        HasClr = $false
        ClrMajor = $null
        ClrMinor = $null
        ClrFlags = $null
        ClrFlagsDecoded = @()
        MetadataRva = $null
        MetadataSize = $null
        NativeEntryPoint = $null
        ImportDlls = @()
        ExportCount = $null
        CliMetadataVersion = $null
        Error = $null
    }

    try {
        $ms.Position = 0
        $mz = [char]$br.ReadByte(); $z = [char]$br.ReadByte()
        $info.Mz = "$mz$z"
        if ($info.Mz -ne 'MZ') { $info.Error = 'not MZ'; return [pscustomobject]$info }

        $ms.Position = 0x3C
        $peOff = $br.ReadInt32()
        $info.PeOffset = $peOff
        $ms.Position = $peOff
        $sig = [System.Text.Encoding]::ASCII.GetString($br.ReadBytes(4))
        if ($sig -ne "PE`0`0") { $info.Error = "bad PE sig $sig"; return [pscustomobject]$info }

        $machine = $br.ReadUInt16()
        $numSections = $br.ReadUInt16()
        $timeDate = $br.ReadUInt32()
        $null = $br.ReadUInt32(); $null = $br.ReadUInt32()
        $optSize = $br.ReadUInt16()
        $chars = $br.ReadUInt16()
        $info.Machine = ('0x{0:X4}' -f $machine) + $(switch ($machine) { 0x14C {' i386'} 0x8664 {' AMD64'} 0xAA64 {' ARM64'} default {''} })
        $info.Characteristics = '0x{0:X4}' -f $chars
        $info.TimeDateStamp = $timeDate
        $info.TimeDateUtc = ([DateTime]'1970-01-01Z').AddSeconds($timeDate).ToString('u')
        $info.NumberOfSections = $numSections
        $info.OptionalHeaderSize = $optSize

        $optStart = $ms.Position
        $magic = $br.ReadUInt16()
        $info.Magic = ('0x{0:X4}' -f $magic) + $(if ($magic -eq 0x10B) {' PE32'} elseif ($magic -eq 0x20B) {' PE32+'} else {''})
        $isPe32Plus = $magic -eq 0x20B

        # skip to Subsystem: PE32=0x44 from opt start, PE32+=0x44 as well? 
        # Optional header: Magic 2, Linker 2, SizeOfCode 4, SizeInit 4, SizeUninit 4, EntryPoint 4, BaseOfCode 4
        # PE32 then BaseOfData 4, ImageBase 4
        # PE32+ ImageBase 8
        $ms.Position = $optStart + 16
        $entryPoint = $br.ReadUInt32()
        $info.AddressOfEntryPoint = '0x{0:X8}' -f $entryPoint
        $ms.Position = $optStart + 24
        if ($isPe32Plus) { $info.ImageBase = $br.ReadUInt64() } else { $info.ImageBase = $br.ReadUInt32(); $null = $br.ReadUInt32() }

        # Subsystem at +68 (0x44) from optional header start for both
        $ms.Position = $optStart + 0x44
        $subsys = $br.ReadUInt16()
        $info.Subsystem = ('0x{0:X4}' -f $subsys) + $(switch ($subsys) { 2 {' WINDOWS_GUI'} 3 {' WINDOWS_CUI'} default {''} })

        # NumberOfRvaAndSizes at +92 (0x5C) PE32, +108 (0x6C) PE32+
        $numRvaOff = if ($isPe32Plus) { 0x6C } else { 0x5C }
        $ms.Position = $optStart + $numRvaOff
        $numRva = $br.ReadUInt32()
        $info.NumberOfRvaAndSizes = $numRva
        $ddStart = $optStart + $numRvaOff + 4

        function Read-DD([int]$index) {
            $ms.Position = $ddStart + ($index * 8)
            [pscustomobject]@{ Rva = $br.ReadUInt32(); Size = $br.ReadUInt32() }
        }

        $exportDD = Read-DD 0
        $importDD = Read-DD 1
        $comDD = Read-DD 14
        $info.ComDescriptorRva = $comDD.Rva
        $info.ComDescriptorSize = $comDD.Size
        $info.HasClr = $comDD.Rva -ne 0 -and $comDD.Size -ne 0
        $info.ExportDirRva = $exportDD.Rva
        $info.ExportDirSize = $exportDD.Size
        $info.ImportDirRva = $importDD.Rva
        $info.ImportDirSize = $importDD.Size

        # section table
        $secStart = $optStart + $optSize
        $sections = @()
        for ($i=0; $i -lt $numSections; $i++) {
            $ms.Position = $secStart + ($i * 40)
            $nameBytes = $br.ReadBytes(8)
            $name = ([System.Text.Encoding]::ASCII.GetString($nameBytes)).Trim([char]0)
            $vsize = $br.ReadUInt32()
            $vaddr = $br.ReadUInt32()
            $rsize = $br.ReadUInt32()
            $rptr = $br.ReadUInt32()
            $sections += [pscustomobject]@{ Name=$name; VirtualSize=$vsize; VirtualAddress=$vaddr; RawSize=$rsize; RawPtr=$rptr }
        }
        $info.Sections = ($sections | ForEach-Object { $_.Name }) -join ','

        function Convert-RvaToOff([uint32]$rva) {
            foreach ($s in $sections) {
                if ($rva -ge $s.VirtualAddress -and $rva -lt ($s.VirtualAddress + [Math]::Max($s.VirtualSize, $s.RawSize))) {
                    return [int64]($s.RawPtr + ($rva - $s.VirtualAddress))
                }
            }
            return -1
        }

        if ($info.HasClr) {
            $off = Convert-RvaToOff $comDD.Rva
            if ($off -ge 0) {
                $ms.Position = $off
                $cb = $br.ReadUInt32()
                $maj = $br.ReadUInt16()
                $min = $br.ReadUInt16()
                $mdRva = $br.ReadUInt32()
                $mdSize = $br.ReadUInt32()
                $flags = $br.ReadUInt32()
                $entryTok = $br.ReadUInt32()
                $info.ClrHeaderCb = $cb
                $info.ClrMajor = $maj
                $info.ClrMinor = $min
                $info.MetadataRva = $mdRva
                $info.MetadataSize = $mdSize
                $info.ClrFlags = '0x{0:X8}' -f $flags
                $decoded = @()
                if ($flags -band 0x1) { $decoded += 'ILONLY' }
                if ($flags -band 0x2) { $decoded += '32BITREQUIRED' }
                if ($flags -band 0x4) { $decoded += 'IL_LIBRARY' }
                if ($flags -band 0x8) { $decoded += 'STRONGNAMESIGNED' }
                if ($flags -band 0x10) { $decoded += 'NATIVE_ENTRYPOINT' }
                if ($flags -band 0x10000) { $decoded += 'TRACKDEBUGDATA' }
                if ($flags -band 0x20000) { $decoded += '32BITPREFERRED' }
                if (-not ($flags -band 0x1)) { $decoded += 'MIXED_MODE(not-ILONLY)' }
                $info.ClrFlagsDecoded = $decoded
                $info.ClrEntryPointToken = '0x{0:X8}' -f $entryTok
                $info.NativeEntryPoint = [bool]($flags -band 0x10)

                $mdOff = Convert-RvaToOff $mdRva
                if ($mdOff -ge 0) {
                    $ms.Position = $mdOff
                    $bsjb = [System.Text.Encoding]::ASCII.GetString($br.ReadBytes(4))
                    $info.MetadataSig = $bsjb
                    $mdMaj = $br.ReadUInt16(); $mdMin = $br.ReadUInt16()
                    $reserved = $br.ReadUInt32()
                    $verLen = $br.ReadUInt32()
                    $ver = [System.Text.Encoding]::UTF8.GetString($br.ReadBytes([int]$verLen)).Trim([char]0)
                    $info.CliMetadataVersion = $ver
                    $info.MetadataMajorMinor = "$mdMaj.$mdMin"
                    $null = $reserved
                }
            }
        }

        if ($importDD.Rva -ne 0) {
            $impOff = Convert-RvaToOff $importDD.Rva
            $dlls = @()
            if ($impOff -ge 0) {
                for ($i=0; $i -lt 64; $i++) {
                    $ms.Position = $impOff + ($i * 20)
                    $ilt = $br.ReadUInt32()
                    $ts = $br.ReadUInt32()
                    $fwd = $br.ReadUInt32()
                    $nameRva = $br.ReadUInt32()
                    $iat = $br.ReadUInt32()
                    if ($ilt -eq 0 -and $nameRva -eq 0 -and $iat -eq 0) { break }
                    $nOff = Convert-RvaToOff $nameRva
                    if ($nOff -ge 0) {
                        $ms.Position = $nOff
                        $sb = New-Object System.Text.StringBuilder
                        while ($true) {
                            $b = $br.ReadByte()
                            if ($b -eq 0) { break }
                            [void]$sb.Append([char]$b)
                        }
                        $dlls += $sb.ToString()
                    }
                    $null = $ts; $null = $fwd
                }
            }
            $info.ImportDlls = $dlls
        }

        if ($exportDD.Rva -ne 0) {
            $exOff = Convert-RvaToOff $exportDD.Rva
            if ($exOff -ge 0) {
                $ms.Position = $exOff + 24
                $nFuncs = $br.ReadUInt32()
                $nNames = $br.ReadUInt32()
                $info.ExportCount = $nFuncs
                $info.ExportNameCount = $nNames
            }
        }
    }
    catch {
        $info.Error = $_.Exception.ToString()
    }
    finally {
        $br.Dispose(); $ms.Dispose()
    }
    return [pscustomobject]$info
}

$libs = @(
    'D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll',
    'D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll',
    'D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll',
    'D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5WebAPI.dll'
)

$results = foreach ($p in $libs) { Get-PeClrInfo -Path $p }
$jsonPath = 'D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json'
$results | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 $jsonPath
$results | Format-List | Out-String | Write-Output
Write-Output "WROTE $jsonPath"
