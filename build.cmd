@echo off

pushd %CD%

cd /d "%~dp0"

pushd %CD%
cd .\TechnitiumLibrary
dotnet build ./TechnitiumLibrary.Net -c Release
popd

pushd %CD%
cd .\DnsServer
dotnet build ./DnsServerCore.ApplicationCommon
popd

pushd %CD%
cd .\QueryLogsAMQP
del /Q /S bin > nul 2> nul 
del /Q /S obj > nul 2> nul 
dotnet restore
dotnet build --no-restore -c Release
dotnet test --no-build --verbosity normal
popd

popd
