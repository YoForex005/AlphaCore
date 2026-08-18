$ErrorActionPreference = 'Stop'
$outDir = 'D:\Prop\reports\swarm\20260818\_tmp_r54_gate'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

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
  'D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs',
  'D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs',
  'D:\Prop\src\Application\Ingestion\DealIngestionService.cs',
  'D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs',
  'D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs'
)

$rows = @()
foreach ($p in $files) {
  $item = Get-Item -LiteralPath $p
  $hash = (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash
  $lines = @(Get-Content -LiteralPath $p).Count
  $rows += [pscustomobject]@{
    path = $p
    bytes = $item.Length
    lines = $lines
    sha256 = $hash
    lastWriteUtc = $item.LastWriteTimeUtc.ToString('o')
  }
}

# Independent IsSecret replica (same operators as LiveMt5Registration.IsSecret). Synthetic only.
function Replica-IsSecret([string]$value) {
  if ([string]::IsNullOrWhiteSpace($value)) { return $false }
  if ($value.Contains('<SECRET>', [System.StringComparison]::Ordinal)) { return $false }
  if ($value.Contains('(a/c', [System.StringComparison]::Ordinal)) { return $false }
  return $true
}
function Replica-HasReal([string]$a, [string]$s) {
  return (Replica-IsSecret $a) -and (Replica-IsSecret $s)
}

$cases = @(
  @{ name = 'both_missing'; a = $null; s = $null; expected = $false },
  @{ name = 'both_empty'; a = ''; s = ''; expected = $false },
  @{ name = 'both_whitespace'; a = '  '; s = "`t"; expected = $false },
  @{ name = 'achiever_only'; a = 'not-a-placeholder-token'; s = ''; expected = $false },
  @{ name = 'starwave_only'; a = ''; s = 'not-a-placeholder-token'; expected = $false },
  @{ name = 'both_SECRET_token'; a = '<SECRET>'; s = '<SECRET>'; expected = $false },
  @{ name = 'achiever_SECRET_starwave_ok'; a = '<SECRET>'; s = 'not-a-placeholder-token'; expected = $false },
  @{ name = 'achiever_ok_starwave_SECRET'; a = 'not-a-placeholder-token'; s = '<SECRET>'; expected = $false },
  @{ name = 'both_account_comment'; a = 'pw (a/c 1)'; s = 'pw (a/c 2)'; expected = $false },
  @{ name = 'both_ok_synthetic'; a = 'not-a-placeholder-token'; s = 'not-a-placeholder-token'; expected = $true },
  @{ name = 'lowercase_secret_token'; a = '<secret>'; s = '<secret>'; expected = $true },
  @{ name = 'mixed_case_secret_token'; a = '<Secret>'; s = '<Secret>'; expected = $true },
  @{ name = 'dummy_word'; a = 'dummy'; s = 'changeme'; expected = $true },
  @{ name = 'single_char'; a = 'x'; s = 'y'; expected = $true },
  @{ name = 'uppercase_account_comment'; a = 'pw (A/C 1)'; s = 'pw (A/C 2)'; expected = $true }
)

$caseRows = @()
foreach ($c in $cases) {
  $actual = Replica-HasReal $c.a $c.s
  $caseRows += [pscustomobject]@{
    name = $c.name
    expected = $c.expected
    actual = $actual
    pass = ($actual -eq $c.expected)
  }
}

$payload = [ordered]@{
  probe = 'W500_R54_IsSecret_replica_and_hashes'
  utc = [DateTimeOffset]::UtcNow.ToString('o')
  note = 'Synthetic tokens only. No operator secrets. Replica uses same Ordinal substring rules as LiveMt5Registration.IsSecret.'
  files = $rows
  replicaCases = $caseRows
  replicaPassed = @($caseRows | Where-Object { $_.pass }).Count
  replicaTotal = $caseRows.Count
}

$json = $payload | ConvertTo-Json -Depth 6
Set-Content -LiteralPath (Join-Path $outDir 'HASHES_AND_REPLICA.json') -Value $json -Encoding UTF8
Write-Output ("replicaPassed=" + $payload.replicaPassed + "/" + $payload.replicaTotal)
foreach ($r in $rows) { Write-Output ("FILE " + $r.path + " lines=" + $r.lines + " sha256=" + $r.sha256) }
foreach ($c in $caseRows) { Write-Output ("CASE " + $c.name + " expected=" + $c.expected + " actual=" + $c.actual + " pass=" + $c.pass) }
