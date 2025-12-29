@echo off
echo ========================================
echo Starting Bookstore Application
echo ========================================
echo.

echo Starting API...
start "Bookstore API" cmd /k "cd Bookstore.API && dotnet run"

timeout /t 3 /nobreak > nul

echo Starting Worker...
start "Bookstore Worker" cmd /k "cd Bookstore.Worker && dotnet run"

timeout /t 2 /nobreak > nul

echo Starting Web (Customer App)...
start "Bookstore Web" cmd /k "cd Bookstore.Web\Bookstore.Web && dotnet run"

timeout /t 2 /nobreak > nul

echo Starting Admin App...
start "Bookstore Admin" cmd /k "cd Bookstore.Admin && npm run dev"

echo.
echo ========================================
echo All services are starting!
echo ========================================
echo.
echo Services:
echo   - API:      https://localhost:7032
echo   - Web:      https://localhost:7266
echo   - Admin:    http://localhost:5174
echo   - Worker:   Running in background
echo.
echo Press any key to exit this window...
echo (Services will continue running in their own windows)
pause > nul
