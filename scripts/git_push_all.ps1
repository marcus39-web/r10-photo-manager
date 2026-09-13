param(
	[string]$RepoRoot = "D:\\11_Foto_App\\R10CSharp",
	[string]$CommitMessage = "Snapshot: alle Änderungen commit"
)

Write-Host "Repository: $RepoRoot"

# Versuche Visual Studio (devenv) zu beenden
Try {
	Write-Host "Versuche Visual Studio-Prozess zu beenden..."
	Stop-Process -Name devenv -ErrorAction SilentlyContinue
} Catch { }

Set-Location -Path $RepoRoot

# Ensure .gitignore contains .vs/
$gitignorePath = Join-Path $RepoRoot ".gitignore"
if (-Not (Test-Path $gitignorePath)) { New-Item -Path $gitignorePath -ItemType File -Force | Out-Null }
if (-Not (Select-String -Path $gitignorePath -Pattern "^\s*\.vs/" -Quiet)) {
	Write-Host "Füge .vs/ zur .gitignore hinzu"
	Add-Content -Path $gitignorePath -Value "`n# Visual Studio local files`.vs/"
}

# Entferne .vs aus dem Index, falls getrackt
Write-Host "Entferne .vs aus dem Git-Index (wenn vorhanden)..."
git rm -r --cached .vs 2>$null

# Alles hinzufügen
Write-Host "Stage alle Änderungen..."
git add -A

# Commit
Write-Host "Commit mit Nachricht: $CommitMessage"
$commitExit = & git commit -m "$CommitMessage" 2>&1
Write-Host $commitExit

# Pull --rebase
Write-Host "Pull --rebase origin main"
& git pull --rebase origin main

# Push
Write-Host "Push origin main"
& git push origin main

Write-Host "Fertig. Überprüfen Sie die Ausgabe oben auf Fehler."
