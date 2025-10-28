@echo off
REM ================================================
REM Subida forzada de versión local definitiva a main
REM ================================================

setlocal
cd /d "%~dp0"

REM Asegura que sea un repositorio Git válido
git rev-parse --is-inside-work-tree >nul 2>&1 || (
  echo [ERROR] No parece ser un repositorio Git.
  pause
  exit /b 1
)

REM Cambiar a rama main
git checkout main || (
  echo [ERROR] No se pudo cambiar a main.
  pause
  exit /b 1
)

REM Capturar fecha y hora actuales (dd/mm/yy hhmm)
for /f "tokens=1-3 delims=/" %%a in ("%date%") do set FECHA=%%a/%%b/%%c
for /f "tokens=1-2 delims=: " %%a in ("%time%") do set HORA=%%a%%b

echo [INFO] Subiendo version local definitiva...
git add -A

REM Mensaje de commit con hora y fecha exactas
git commit -m "Subida forzada: versión local %FECHA% %HORA%" || echo [INFO] Sin cambios nuevos.

REM Subir forzadamente al repositorio remoto
git push origin main --force

echo [OK] Repositorio remoto ahora es idéntico a tu versión local.
pause
exit /b 0
