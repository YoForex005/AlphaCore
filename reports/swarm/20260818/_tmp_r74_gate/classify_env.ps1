$ErrorActionPreference = 'Stop'
$outDir = 'D:\Prop\reports\swarm\20260818\_tmp_r74_gate'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Replica-IsSecret([string]$value) {
  if ([string]::IsNullOrWhiteSpace($value)) { return $false }
  if ($value.Contains('<SECRET>', [System.StringComparison]::Ordinal)) { return $false }
  if ($value.Contains('(a/c', [System.StringComparison]::Ordinal)) { return $false }
  return $true
}

function Classify-Value([string]$value) {
  if ($null -eq $value) { return 'MISSING' }
  if ([string]::IsNullOrWhiteSpace($value)) { return 'WHITESPACE_OR_EMPTY' }
  if ($value -eq '<SECRET>') { return 'PLACEHOLDER_SECRET_EXACT' }
  if ($value.Contains('<SECRET>', [System.StringComparison]::Ordinal)) { return 'CONTAINS_SECRET_TOKEN' }
  if ($value.Contains('(a/c', [System.StringComparison]::Ordinal)) { return 'CONTAINS_ACCOUNT_COMMENT' }
  return 'NON_PLACEHOLDER'
}

$envPath = 'D:\Prop\.env'
$envExists = Test-Path -LiteralPath $envPath
$envBytes = 0
$envSha = $null
$envWrite = $null
$map = @{}
$hasAccountCommentAnywhere = $false
if ($envExists) {
  $item = Get-Item -LiteralPath $envPath
  $envBytes = $item.Length
  $envSha = (Get-FileHash -LiteralPath $envPath -Algorithm SHA256).Hash
  $envWrite = $item.LastWriteTimeUtc.ToString('o')
  foreach ($raw in Get-Content -LiteralPath $envPath) {
    $line = $raw.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith('#') -or -not $line.Contains('=')) { continue }
    $i = $line.IndexOf('=')
    $key = $line.Substring(0, $i).Trim()
    $value = $line.Substring($i + 1).Trim()
    if ($value.Length -ge 2 -and $value[0] -eq '"' -and $value[$value.Length - 1] -eq '"') {
      $value = $value.Substring(1, $value.Length - 2)
    }
    $map[$key] = $value
    if ($line.Contains('(a/c', [System.StringComparison]::Ordinal)) { $hasAccountCommentAnywhere = $true }
  }
}

function Slot($key, [switch]$safePrint) {
  $present = $map.ContainsKey($key)
  $value = if ($present) { [string]$map[$key] } else { $null }
  $obj = [ordered]@{
    key = $key
    present = [bool]$present
    length = if ($present) { $value.Length } else { 0 }
    class = if ($present) { Classify-Value $value } else { 'ABSENT' }
    isSecretReplica = if ($present) { [bool](Replica-IsSecret $value) } else { $false }
  }
  if ($safePrint -and $present -and ($value -eq 'true' -or $value -eq 'false' -or $value -eq 'True' -or $value -eq 'False')) {
    $obj.valueSafe = $value
  }
  return [pscustomobject]$obj
}

$mt5 = Slot 'MT5_PASSWORD'
$sw = Slot 'MT5_STARWAVEFX_PASSWORD'
$fix = Slot 'CTRADER_FIX_PASSWORD'
$db = Slot 'DATABASE_URL'
$real = Slot 'REAL_COPY_EXECUTION_ENABLED' -safePrint
$feat = Slot 'FEATURE_COPY_TRADING_ENABLED' -safePrint
$fixEn = Slot 'CTRADER_FIX_ENABLED' -safePrint

$hasRealReplica = $false
if ($mt5.present -and $sw.present) {
  $hasRealReplica = (Replica-IsSecret ([string]$map['MT5_PASSWORD'])) -and (Replica-IsSecret ([string]$map['MT5_STARWAVEFX_PASSWORD']))
}

$exactSecretMt5 = $false
$exactSecretSw = $false
if ($envExists) {
  $exactSecretMt5 = Select-String -LiteralPath $envPath -Pattern '^MT5_PASSWORD=<SECRET>\s*$' -Quiet
  $exactSecretSw = Select-String -LiteralPath $envPath -Pattern '^MT5_STARWAVEFX_PASSWORD=<SECRET>\s*$' -Quiet
}

$files = @(
  'D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs',
  'D:\Prop\src\Infrastructure\DependencyInjection.cs',
  'D:\Prop\apps\api\Program.cs',
  'D:\Prop\apps\mt5-worker\Program.cs',
  'D:\Prop\apps\fix-worker\Program.cs',
  'D:\Prop\apps\mt5-worker\Worker.cs',
  'D:\Prop\apps\fix-worker\Worker.cs',
  'D:\Prop\tools\LiveBrokerProbe\Program.cs',
  'D:\Prop\src\Mt5\Env\EnvFile.cs',
  'D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs',
  'D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs',
  'D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs',
  'D:\Prop\src\Application\Ingestion\DealIngestionService.cs',
  'D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs',
  'D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs'
)

$fileRows = @()
foreach ($p in $files) {
  $item = Get-Item -LiteralPath $p
  $fileRows += [pscustomobject]@{
    path = $p
    bytes = $item.Length
    lines = @(Get-Content -LiteralPath $p).Count
    sha256 = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
    lastWriteUtc = $item.LastWriteTimeUtc.ToString('o')
  }
}

$srcRoot = 'D:\Prop\src'
$fixRoot = 'D:\Prop\src\Fix.CTrader'
$hits35D = @(Select-String -Path (Join-Path $fixRoot '*') -Pattern '35=D' -SimpleMatch -Recurse -ErrorAction SilentlyContinue).Count
$hitsNOS = @(Select-String -Path (Join-Path $srcRoot '*') -Pattern 'NewOrderSingle' -Recurse -Include *.cs -ErrorAction SilentlyContinue).Count
$hitsOrderSend = @(Select-String -Path (Join-Path $srcRoot 'Mt5\*') -Pattern 'OrderSend|DealerSend|TradeTrans|DealerBalance' -Recurse -Include *.cs -ErrorAction SilentlyContinue).Count
$hitsHasReal = @(Select-String -Path (Join-Path $srcRoot '*') -Pattern 'HasRealPasswords' -Recurse -Include *.cs -ErrorAction SilentlyContinue | ForEach-Object { $_.Path + ':' + $_.LineNumber })
$hitsFakeDi = @(Select-String -Path 'D:\Prop\src\Infrastructure\DependencyInjection.cs' -Pattern 'FakeMt5' -SimpleMatch).Count
$hitsDemoApi = @(Select-String -Path 'D:\Prop\apps\api\Program.cs' -Pattern 'DemoSeeder' -SimpleMatch).Count
$hitsEnvApi = @(Select-String -Path 'D:\Prop\apps\api\Program.cs' -Pattern 'FindAndLoad' -SimpleMatch).Count
$hitsEnvMt5 = @(Select-String -Path 'D:\Prop\apps\mt5-worker\Program.cs' -Pattern 'FindAndLoad' -SimpleMatch).Count
$hitsEnvFix = @(Select-String -Path 'D:\Prop\apps\fix-worker\Program.cs' -Pattern 'FindAndLoad' -SimpleMatch).Count
$testsHasReal = @(Select-String -Path 'D:\Prop\tests\*' -Pattern 'HasRealPasswords' -Recurse -Include *.cs -ErrorAction SilentlyContinue).Count

$payload = [ordered]@{
  probe = 'W500_R74_env_classify_and_hashes'
  utc = [DateTimeOffset]::UtcNow.ToString('o')
  note = 'Values discarded. Classification + lengths only. No operator secrets printed.'
  envFile = [ordered]@{
    path = $envPath
    exists = $envExists
    bytes = $envBytes
    sha256 = $envSha
    lastWriteUtc = $envWrite
    hasAccountCommentAnywhere = $hasAccountCommentAnywhere
    exactLineMt5PasswordSecret = [bool]$exactSecretMt5
    exactLineStarwavePasswordSecret = [bool]$exactSecretSw
    hasRealPasswordsReplica = $hasRealReplica
  }
  slots = @($mt5, $sw, $fix, $db, $real, $feat, $fixEn)
  files = $fileRows
  greps = [ordered]@{
    fixCTrader35D = $hits35D
    srcNewOrderSingle = $hitsNOS
    srcMt5OrderSendFamily = $hitsOrderSend
    hasRealPasswordsHits = $hitsHasReal
    fakeMt5InDi = $hitsFakeDi
    demoSeederInApi = $hitsDemoApi
    envFindAndLoadApi = $hitsEnvApi
    envFindAndLoadMt5Worker = $hitsEnvMt5
    envFindAndLoadFixWorker = $hitsEnvFix
    testsHasRealPasswords = $testsHasReal
  }
}

$json = $payload | ConvertTo-Json -Depth 8
Set-Content -LiteralPath (Join-Path $outDir 'ENV_AND_HASHES.json') -Value $json -Encoding UTF8
Write-Output ("ENV_EXISTS=" + $envExists + " BYTES=" + $envBytes)
Write-Output ("HAS_REAL_REPLICA=" + $hasRealReplica)
Write-Output ("MT5_CLASS=" + $mt5.class + " LEN=" + $mt5.length)
Write-Output ("SW_CLASS=" + $sw.class + " LEN=" + $sw.length)
Write-Output ("FIX_CLASS=" + $fix.class + " LEN=" + $fix.length)
Write-Output ("REAL_COPY=" + $real.valueSafe + " FEATURE_COPY=" + $feat.valueSafe + " CTRADER_FIX_ENABLED=" + $fixEn.valueSafe)
Write-Output ("EXACT_SECRET_LINES mt5=" + $exactSecretMt5 + " starwave=" + $exactSecretSw)
Write-Output ("GREP 35D=" + $hits35D + " NewOrderSingle=" + $hitsNOS + " OrderSendFamily=" + $hitsOrderSend + " testsHasReal=" + $testsHasReal)
Write-Output ("DI FakeMt5=" + $hitsFakeDi + " API DemoSeeder=" + $hitsDemoApi + " API EnvLoad=" + $hitsEnvApi)
foreach ($f in $fileRows) { Write-Output ("FILE " + $f.path + " lines=" + $f.lines + " sha256=" + $f.sha256) }
