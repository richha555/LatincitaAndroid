# Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

cd "C:\Users\Richard\source\repos\LatincitaAndroid\LatincitaAndroid\View\"

$file = "DetailsPage.xaml"
$backup = "DetailsPage.broken.$(Get-Date -Format 'yyyyMMdd_HHmmss').xaml"

# 1. Make a backup of the current local file
Copy-Item $file $backup

Write-Host "Backup created: $backup"

# 2. Restore the version from the remote main branch
git fetch origin
git restore --source=origin/main --worktree -- $file

Write-Host "Restored $file from origin/main"