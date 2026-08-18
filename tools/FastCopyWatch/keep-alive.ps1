$project = "D:\Prop\tools\FastCopyWatch\FastCopyWatch.csproj"
$logDir = "D:\Prop\data"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outLog = Join-Path $logDir "fastcopywatch-keepalive.log"

function Watch-Running {
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
        Where-Object { $_.CommandLine -match "FastCopyWatch" } |
        Select-Object -First 1
}

"keep-alive start $stamp" | Out-File -FilePath $outLog -Append
while ($true) {
    try {
        if (-not (Watch-Running)) {
            "$(Get-Date -Format o) RESTART FastCopyWatch" | Out-File -FilePath $outLog -Append
            Start-Process -FilePath "dotnet" -ArgumentList @(
                "run", "--project", $project, "-c", "Debug", "--no-build"
            ) -WorkingDirectory "D:\Prop" -WindowStyle Hidden
        }
    } catch {
        "$(Get-Date -Format o) KEEPALIVE $($_.Exception.Message)" | Out-File -FilePath $outLog -Append
    }
    Start-Sleep -Seconds 15
}
