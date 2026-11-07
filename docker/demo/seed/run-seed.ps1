<#
.SYNOPSIS
  Creates the seeded demo database (docker/demo/automatica.core.db) with a temporary container of the demo image.

.DESCRIPTION
  1. starts automatica-demo:local with an empty database (initial password of sa: sa)
  2. stops it and clears the "must change password" flag of sa (seed.py --prepare-db), the demo keeps sa/sa
  3. starts it again and seeds it through the Web API (seed.py)
  4. lets the drivers and logics run for a moment, stops it and copies the database to docker/demo/automatica.core.db

  Afterwards run build-local.ps1 -SkipCore -SkipPlugins -SkipFrontend to bake the database into the image.
  Needs python 3 (standard library only).
#>
[CmdletBinding()]
param(
    [string]$Image = "automatica-demo:local",
    [string]$Container = "automatica-demo-seed",
    [int]$Port = 5011,
    [int]$RunSeconds = 30
)

$ErrorActionPreference = "Stop"
$seedDir = $PSScriptRoot
$demoDir = Split-Path $seedDir
$work = Join-Path $demoDir ".build\seed"
$dbInContainer = "/app/automatica/seed.db"

function Invoke-Docker {
    & docker @args
    if ($LASTEXITCODE -ne 0) { throw "docker $($args -join ' ') failed" }
}

function Wait-Http([string]$url) {
    for ($i = 0; $i -lt 120; $i++) {
        try { Invoke-WebRequest -UseBasicParsing -TimeoutSec 5 $url | Out-Null; return } catch { Start-Sleep -Seconds 2 }
    }
    throw "$url not reachable"
}

function Copy-DbOut {
    Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $work "seed.db*")
    Invoke-Docker cp "${Container}:$dbInContainer" (Join-Path $work "seed.db")
    foreach ($suffix in @("-wal", "-shm")) {
        & docker cp "${Container}:$dbInContainer$suffix" (Join-Path $work "seed.db$suffix") 2>$null
    }
}

if (Test-Path $work) { Remove-Item -Recurse -Force $work }
New-Item -ItemType Directory -Force $work | Out-Null

& docker rm -f $Container 2>$null | Out-Null

# runs as root: the database file copied back into the container belongs to root
Invoke-Docker create --name $Container --user 0 -p "${Port}:5001" `
    -e AUTOMATICA_INITIAL_ADMIN_PASSWORD=sa `
    -e "ConnectionStrings__AutomaticaDatabaseSqlite=Data Source=$dbInContainer" $Image | Out-Null

Write-Host "== first start (creates the database)" -ForegroundColor Cyan
Invoke-Docker start $Container | Out-Null
Wait-Http "http://localhost:$Port/"
Invoke-Docker stop $Container | Out-Null

Copy-DbOut
& python (Join-Path $seedDir "seed.py") --prepare-db (Join-Path $work "seed.db")
if ($LASTEXITCODE -ne 0) { throw "prepare-db failed" }
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $work "seed.db-wal"), (Join-Path $work "seed.db-shm")
Invoke-Docker cp (Join-Path $work "seed.db") "${Container}:$dbInContainer"

Write-Host "== seeding through the Web API" -ForegroundColor Cyan
Invoke-Docker start $Container | Out-Null
Wait-Http "http://localhost:$Port/"
& python (Join-Path $seedDir "seed.py") --url "http://localhost:$Port" --user sa --password sa 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { throw "seeding failed (container $Container is left running for a look)" }

Start-Sleep -Seconds $RunSeconds
Invoke-Docker stop $Container | Out-Null
Copy-DbOut
& python (Join-Path $seedDir "seed.py") --finish-db (Join-Path $work "seed.db")
if ($LASTEXITCODE -ne 0) { throw "finish-db failed" }

Copy-Item -Force (Join-Path $work "seed.db") (Join-Path $demoDir "automatica.core.db")
Invoke-Docker rm $Container | Out-Null
Write-Host "done: $(Join-Path $demoDir 'automatica.core.db')" -ForegroundColor Green
