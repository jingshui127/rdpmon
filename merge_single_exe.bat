@echo off
REM ILMerge 单 EXE 合并脚本

cd /d "%~dp0"

set ILMERGE=.\packages\ILMerge.3.0.41\tools\net452\ILMerge.exe
set SOURCE_DIR=RdpMon\bin\Release
set OUTPUT_EXE=RdpMon\bin\Release\RdpMon_Merged.exe

echo ========================================
echo 开始合并 DLL 到单 EXE 文件...
echo ========================================

if not exist "%ILMERGE%" (
    echo 错误: ILMerge 未找到，请先运行安装脚本
    pause
    exit /b 1
)

echo 源目录: %SOURCE_DIR%
echo 输出: %OUTPUT_EXE%
echo.

REM 执行合并（保留配置文件不合并）
"%ILMERGE%" ^
    /out:"%OUTPUT_EXE%" ^
    "%SOURCE_DIR%\RdpMon.exe" ^
    "%SOURCE_DIR%\LiteDB.dll" ^
    "%SOURCE_DIR%\NewLife.Core.dll" ^
    "%SOURCE_DIR%\NewLife.Remoting.dll" ^
    "%SOURCE_DIR%\Stardust.dll" ^
    "%SOURCE_DIR%\System.Buffers.dll" ^
    "%SOURCE_DIR%\System.Memory.dll" ^
    "%SOURCE_DIR%\System.Numerics.Vectors.dll" ^
    "%SOURCE_DIR%\System.Runtime.CompilerServices.Unsafe.dll" ^
    /allowDup:System.Object

if errorlevel 1 (
    echo.
    echo 错误: 合并失败
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✓ 合并完成！
echo ========================================
echo.
echo 生成的单 EXE 文件:
echo   %OUTPUT_EXE%
echo.
echo 文件大小:
for %%A in ("%OUTPUT_EXE%") do echo   %%~zA 字节
echo.
pause
