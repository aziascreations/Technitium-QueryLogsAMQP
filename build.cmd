@echo off

pushd %CD%

cd /d "%~dp0"

pushd %CD%
cd .\TechnitiumLibrary
dotnet build ./TechnitiumLibrary.Net -c Debug
dotnet build ./TechnitiumLibrary.Net -c Release
dotnet build ./TechnitiumLibrary.Net -c Release /p:Platform="Any CPU"
popd

pushd %CD%
cd .\DnsServer
dotnet build ./DnsServerCore.ApplicationCommon -c Debug
dotnet build ./DnsServerCore.ApplicationCommon -c Release
dotnet build ./DnsServerCore.ApplicationCommon -c Release /p:Platform="Any CPU"
popd

pushd %CD%
cd .\QueryLogsAMQP
del /Q /S bin > nul 2> nul 
del /Q /S obj > nul 2> nul 
dotnet restore
dotnet build --no-restore -c Release /p:Platform="Any CPU"
::dotnet test --no-build --verbosity normal
popd

pushd %CD%
del *.zip > nul 2> nul 
cd ".\QueryLogsAMQP\bin\Any CPU\Release"
"C:\Program Files\7-Zip\7z.exe" a ../../../../QueryLogsAMQP-v0.0.1.zip "./*"
popd

pushd %CD%
cd .\QueryLogsAMQP\bin\Release
:: TODO: Implement this
popd

popd
