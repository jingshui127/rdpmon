$msbuild = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\amd64\MSBuild.exe"
$projectDir = "c:\Users\Administrator\source\repos\rdpmon\RdpMon"

Write-Host "开始编译 RdpMon 项目..."
Write-Host "MSBuild: $msbuild"
Write-Host "项目路径: $projectDir"

Push-Location $projectDir
& $msbuild "RdpMon.csproj" /p:Configuration=Debug /p:Platform=AnyCPU /t:Rebuild
$buildResult = $LASTEXITCODE
Pop-Location

if ($buildResult -eq 0) {
    Write-Host "编译成功！" -ForegroundColor Green
    $exePath = "$projectDir\bin\Debug\RdpMon.exe"
    if (Test-Path $exePath) {
        Write-Host "开始运行程序..." -ForegroundColor Green
        & $exePath
    } else {
        Write-Host "错误：找不到编译输出文件 $exePath" -ForegroundColor Red
    }
} else {
    Write-Host "编译失败，错误代码: $buildResult" -ForegroundColor Red
}
