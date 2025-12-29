#!/bin/bash

echo "========================================"
echo "Starting Bookstore Application"
echo "========================================"
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check for required tools
if ! command_exists dotnet; then
    echo "Error: .NET SDK is not installed"
    exit 1
fi

if ! command_exists npm; then
    echo "Error: npm is not installed"
    exit 1
fi

# Start API
echo "Starting API..."
cd Bookstore.API
dotnet run > /dev/null 2>&1 &
API_PID=$!
cd ..

sleep 3

# Start Worker
echo "Starting Worker..."
cd Bookstore.Worker
dotnet run > /dev/null 2>&1 &
WORKER_PID=$!
cd ..

sleep 2

# Start Web (Customer App)
echo "Starting Web (Customer App)..."
cd Bookstore.Web/Bookstore.Web
dotnet run > /dev/null 2>&1 &
WEB_PID=$!
cd ../..

sleep 2

# Start Admin App
echo "Starting Admin App..."
cd Bookstore.Admin
npm run dev > /dev/null 2>&1 &
ADMIN_PID=$!
cd ..

echo ""
echo "========================================"
echo "All services are starting!"
echo "========================================"
echo ""
echo "Services:"
echo "  - API:      https://localhost:7032"
echo "  - Web:      https://localhost:7266"
echo "  - Admin:    http://localhost:5174"
echo "  - Worker:   Running in background"
echo ""
echo "Process IDs:"
echo "  - API:      $API_PID"
echo "  - Worker:   $WORKER_PID"
echo "  - Web:      $WEB_PID"
echo "  - Admin:    $ADMIN_PID"
echo ""
echo "To stop all services, run:"
echo "  kill $API_PID $WORKER_PID $WEB_PID $ADMIN_PID"
echo ""
echo "Press Ctrl+C to stop monitoring..."

# Keep script running and handle Ctrl+C
trap "echo ''; echo 'Stopping services...'; kill $API_PID $WORKER_PID $WEB_PID $ADMIN_PID 2>/dev/null; exit" INT

# Wait for any process to exit
wait
