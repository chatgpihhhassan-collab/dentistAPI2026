@echo off
title Dentia Full-Stack Dev Launcher
echo ===================================================
echo   🦷 Dentia Platform - All-In-One Dev Launcher
echo ===================================================

echo [1/3] Ensuring OpenWA Gateway is active (Port 2785)...
start "Dentia - OpenWA Gateway (2785)" cmd /c "node openwa_gateway_runner.mjs"

echo [2/3] Starting ASP.NET Core Backend API (Port 5000/5001)...
start "Dentia - Backend API" cmd /c "dotnet run --project DentistAPI\DentistAPI.csproj"

echo [3/3] Starting React Frontend (Port 5173)...
start "Dentia - Frontend UI" cmd /c "cd Dentistfrontend && npm run dev"

echo.
echo ===================================================
echo   🚀 Services Started in Separate Windows:
echo   - OpenWA Gateway   : http://localhost:2785
echo   - Backend Swagger  : http://localhost:5000/swagger
echo   - Frontend Portal  : http://localhost:5173
echo ===================================================
timeout /t 5
