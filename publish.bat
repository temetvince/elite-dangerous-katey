@echo off
setlocal
rem Publishes Katey as one self-contained, compressed Windows exe, katey.exe, into .\bin, with the manual
rem and the two notice files beside it. Nothing needs installing on the machine
rem it runs on. Debug symbols and XML documentation never ship; the documentation is still generated,
rem because the analyzer gate needs it, it is simply not published. KATEY_VERSION, such as 1.2.0, stamps the
rem build's version, which Katey reports to Spansh; 1.0.0 when unset.
if "%KATEY_VERSION%"=="" set KATEY_VERSION=1.0.0

dotnet publish "%~dp0src\Katey.Ui" ^
    -c Release -f net10.0 -r win-x64 --self-contained ^
    -o "%~dp0bin" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -p:PublishDocumentationFile=false ^
    -p:PublishReferencesDocumentationFiles=false ^
    -p:SatelliteResourceLanguages=en ^
    -p:Version=%KATEY_VERSION%
if errorlevel 1 exit /b 1

rem SkiaSharp and HarfBuzzSharp ship their native symbol files; they are not Katey's and do not ship.
del /q "%~dp0bin\*.pdb" 2>nul

rem The manual and the legal notices travel with the executable.
copy /y "%~dp0MANUAL.md" "%~dp0bin\" >nul
copy /y "%~dp0NOTICE.md" "%~dp0bin\" >nul
copy /y "%~dp0THIRD-PARTY-NOTICES.txt" "%~dp0bin\" >nul
echo.
echo Published to %~dp0bin
dir /b "%~dp0bin"
endlocal
exit /b 0
