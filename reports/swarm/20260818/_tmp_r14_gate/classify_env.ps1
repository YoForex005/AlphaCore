$ErrorActionPreference = 'Stop'
$out = @{
  utc = [DateTimeOffset]::Now.ToString('o')
  envFileExists = $false
  envFileBytes = 0
  envFileSha256 = $null
  envFileLastWrite = $null
  keys = @()
  process = @()
  user = @()
  machine = @()
}

function Classify([string]$name, $value) {
  if ($null -eq $value) {
    return @{ name = $name; present = $false; class = 'ABSENT'; length = 0; isSecretReplica = $false; containsExactSECRET = $false; containsIgnoreCaseSecret = $false; containsAcComment = $false; containsAcUpper = $false }
  }
  $s = [string]$value
  $exact = $s.Contains('<SECRET>', [System.StringComparison]::Ordinal)
  $ignore = $s.Contains('<SECRET>', [System.StringComparison]::OrdinalIgnoreCase)
  $ac = $s.Contains('(a/c', [System.StringComparison]::Ordinal)
  $acU = $s.Contains('(A/C', [System.StringComparison]::Ordinal)
  $ws = [string]::IsNullOrWhiteSpace($s)
  $isSecret = (-not $ws) -and (-not $exact) -and (-not $ac)
  $cls = if ($ws) { 'EMPTY_OR_WHITESPACE' }
    elseif ($exact) { 'PLACEHOLDER_SECRET_EXACT' }
    elseif ($ignore) { 'PLACEHOLDER_SECRET_CASE_VARIANT' }
    elseif ($ac) { 'ACCOUNT_COMMENT' }
    else { 'NON_PLACEHOLDER' }
  return @{
    name = $name
    present = $true
    class = $cls
    length = $s.Length
    isSecretReplica = $isSecret
    containsExactSECRET = $exact
    containsIgnoreCaseSecret = $ignore
    containsAcComment = $ac
    containsAcUpper = $acU
  }
}

$envPath = 'D:\Prop\.env'
if (Test-Path -LiteralPath $envPath) {
  $item = Get-Item -LiteralPath $envPath
  $out.envFileExists = $true
  $out.envFileBytes = $item.Length
  $out.envFileLastWrite = $item.LastWriteTime.ToString('o')
  $out.envFileSha256 = (Get-FileHash -LiteralPath $envPath -Algorithm SHA256).Hash
  $map = @{}
  Get-Content -LiteralPath $envPath | ForEach-Object {
    $line = $_.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith('#') -or -not $line.Contains('=')) { return }
    $i = $line.IndexOf('=')
    $k = $line.Substring(0, $i).Trim()
    $v = $line.Substring($i + 1).Trim()
    if ($v.Length -ge 2 -and $v[0] -eq '"' -and $v[$v.Length - 1] -eq '"') {
      $v = $v.Substring(1, $v.Length - 2)
    }
    $map[$k] = $v
  }
  $names = @(
    'MT5_PASSWORD','MT5_STARWAVEFX_PASSWORD','CTRADER_FIX_PASSWORD',
    'ACHIEVER_PROXY_PASSWORD','ACHIEVER_PROXY_USERNAME','ACHIEVER_PROXY_ENABLED',
    'MT5_LOGIN','MT5_STARWAVEFX_LOGIN','MT5_SERVER','MT5_STARWAVEFX_SERVER',
    'REAL_COPY_EXECUTION_ENABLED','FEATURE_COPY_TRADING_ENABLED','DATABASE_URL'
  )
  foreach ($n in $names) {
    if ($map.ContainsKey($n)) { $out.keys += ,(Classify $n $map[$n]) }
    else { $out.keys += ,(Classify $n $null) }
  }
  $out.hasRealPasswordsReplica = ($out.keys | Where-Object { $_.name -eq 'MT5_PASSWORD' }).isSecretReplica -and ($out.keys | Where-Object { $_.name -eq 'MT5_STARWAVEFX_PASSWORD' }).isSecretReplica
}

$procNames = @('MT5_PASSWORD','MT5_STARWAVEFX_PASSWORD','CTRADER_FIX_PASSWORD','REAL_COPY_EXECUTION_ENABLED','DATABASE_URL')
foreach ($n in $procNames) {
  $out.process += ,(Classify $n [Environment]::GetEnvironmentVariable($n, 'Process'))
  $out.user += ,(Classify $n [Environment]::GetEnvironmentVariable($n, 'User'))
  $out.machine += ,(Classify $n [Environment]::GetEnvironmentVariable($n, 'Machine'))
}

$src = @(
  'D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs',
  'D:\Prop\src\Infrastructure\DependencyInjection.cs',
  'D:\Prop\apps\api\Program.cs',
  'D:\Prop\apps\mt5-worker\Program.cs',
  'D:\Prop\apps\fix-worker\Program.cs',
  'D:\Prop\tools\LiveBrokerProbe\Program.cs',
  'D:\Prop\src\Mt5\Env\EnvFile.cs'
)
$out.files = @()
foreach ($p in $src) {
  if (Test-Path $p) {
    $out.files += ,@{
      path = $p
      lines = @(Get-Content -LiteralPath $p).Count
      sha256 = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
    }
  }
}

$json = $out | ConvertTo-Json -Depth 6
Set-Content -LiteralPath 'D:\Prop\reports\swarm\20260818\_tmp_r14_gate\ENV_CLASS.json' -Value $json -Encoding UTF8
Write-Output 'ENV_CLASS written (no secret values)'
Write-Output ("hasRealPasswordsReplica=" + $out.hasRealPasswordsReplica)
foreach ($k in $out.keys) {
  Write-Output ("ENV " + $k.name + " class=" + $k.class + " len=" + $k.length + " isSecretReplica=" + $k.isSecretReplica)
}
foreach ($k in $out.process) {
  Write-Output ("PROC " + $k.name + " class=" + $k.class + " present=" + $k.present + " len=" + $k.length)
}
