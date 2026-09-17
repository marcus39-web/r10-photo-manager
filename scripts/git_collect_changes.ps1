param(
	[string]$RepoRoot = "D:\\11_Foto_App\\R10CSharp"
)

Set-Location -Path $RepoRoot

Write-Host "Repository: $RepoRoot"
Write-Host "Prüfe Status..."

$paths = @(
	"App.xaml",
	"MainWindow.xaml",
	"MainWindow.xaml.cs",
	"Assets",
	"Pages",
	"Services",
	".gitignore"
)

foreach ($path in $paths) {
	if (Test-Path $path) {
		Write-Host "git add $path"
		git add -- $path
	}
}

Write-Host "Fertig. Danach bitte ausführen:"
Write-Host 'git status'
Write-Host 'git commit -m "Fix RAW/JPG Suche und CR3 Index"'
Write-Host 'git push origin main'
