@echo off

pushd %CD%

cd /d "%~dp0"

for /R /D %%p in ("*") do (
    rmdir /Q /S %%p\bin 2> nul
    rmdir /Q /S %%p\obj 2> nul
)

popd
