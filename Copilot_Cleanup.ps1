# Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

cd C:\Users\Richard\source\repos\LatincitaAndroid
Stop-Process -Name devenv -ErrorAction SilentlyContinue 
Remove-Item -Force .\LatincitaAndroid\Windows\Package.appxmanifest 
Remove-Item -Force -Recurse ..vs 
Remove-Item -Force -Recurse .\LatincitaAndroid\bin, .\LatincitaAndroid\obj 
dotnet restore dotnet build