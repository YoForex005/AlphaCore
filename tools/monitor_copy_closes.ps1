$ErrorActionPreference = 'Continue'
$ledgerPath = 'D:\Prop\data\demo_copy_ledger.json'
$statePath = 'D:\Prop\data\copy_monitor_state.json'
$failAfterSec = 50

$state = @{}
if (Test-Path $statePath) {
  try { $state = Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable } catch { $state = @{} }
}

while ($true) {
  try {
    $null = Invoke-RestMethod 'http://127.0.0.1:5000/api/health' -TimeoutSec 8
    if (-not (Test-Path $ledgerPath)) { Write-Output 'FAILED'; exit 1 }
    $ledger = Get-Content $ledgerPath -Raw | ConvertFrom-Json
    foreach ($f in $ledger) {
      if ($f.DestClosed) { continue }
      if ([string]::IsNullOrWhiteSpace($f.DestPositionId)) { continue }
      $broker = if ($f.Broker) { $f.Broker } else { 'ACHIEVER' }
      $d = Invoke-RestMethod "http://127.0.0.1:5000/api/traders/$broker/$($f.SourceLogin)" -TimeoutSec 20
      $src = @($d.trades | Where-Object { $_.positionId.ToString() -eq $f.SourcePositionId.ToString() }) | Select-Object -First 1
      if ($null -eq $src) { continue }
      $key = "$($f.SourceLogin)/$($f.SourcePositionId)"
      if (-not $src.completed) {
        if ($state.ContainsKey($key)) { $state.Remove($key) }
        continue
      }
      if (-not $state.ContainsKey($key)) { $state[$key] = [int][double]::Parse((Get-Date -UFormat %s)) }
      $age = [int][double]::Parse((Get-Date -UFormat %s)) - [int]$state[$key]
      if ($age -ge $failAfterSec) {
        Write-Output 'FAILED'
        exit 1
      }
    }
    ($state | ConvertTo-Json) | Set-Content $statePath -Encoding utf8
  } catch {
    Write-Output 'FAILED'
    exit 1
  }
  Start-Sleep -Milliseconds 500
}
