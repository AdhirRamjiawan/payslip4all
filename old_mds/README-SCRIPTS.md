# Payslip4All Helper Scripts

This directory contains convenient scripts to start and stop the Payslip4All application components.

## Quick Start

### Run Everything at Once
```bash
# Linux/Mac
./start-all.sh

# Windows
start-all.bat
```

This starts both the backend API and frontend dev server in one command.

## Individual Scripts

### Start Backend API

**Linux/Mac:**
```bash
./start-backend.sh
```

**Windows:**
```bash
start-backend.bat
```

- Starts ASP.NET Core API on `https://localhost:5001`
- Swagger UI available at `https://localhost:5001/swagger`
- Runs in foreground (Ctrl+C to stop)

### Start Frontend Dev Server

**Linux/Mac:**
```bash
./start-frontend.sh
```

**Windows:**
```bash
start-frontend.bat
```

- Starts Angular development server on `http://localhost:4200`
- Watches for file changes and auto-reloads
- Automatically installs dependencies if needed
- Runs in foreground (Ctrl+C to stop)

### Start Both Services

**Linux/Mac:**
```bash
./start-all.sh
```

**Windows:**
```bash
start-all.bat
```

- Starts backend and frontend simultaneously
- Creates logs in `logs/` directory
- Monitors both processes
- To stop: Use `./stop-all.sh` (Linux/Mac) or `stop-all.bat` (Windows)

### Stop All Services

**Linux/Mac:**
```bash
./stop-all.sh
```

**Windows:**
```bash
stop-all.bat
```

- Terminates all backend and frontend processes
- Cleans up PID files
- Safe to run even if services aren't running

## Requirements

### System Requirements
- **Operating System**: Windows, macOS, or Linux
- **Disk Space**: ~500MB (for dependencies and database)

### Software Requirements

#### Backend
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
  - Required to run the ASP.NET Core API
  - Download and install the .NET 8 SDK

#### Frontend
- **Node.js 18+** - [Download](https://nodejs.org)
  - Includes npm package manager
  - Angular CLI will be installed automatically
- **npm** - Included with Node.js
  - Used to install dependencies and run development server

### Verify Installation

```bash
# Check .NET
dotnet --version

# Check Node.js
node --version
npm --version
```

All should return version numbers.

## Understanding the Services

### Backend API
- **Language**: C# (ASP.NET Core 8)
- **Default Port**: 5001 (HTTPS)
- **Database**: SQLite (file-based, automatic setup)
- **When Ready**: Shows "Application started" in console

### Frontend
- **Language**: TypeScript (Angular 17+)
- **Default Port**: 4200 (HTTP)
- **When Ready**: Shows "✓ Compiled successfully" in console

### Accessing the Application

Once both services are running:

1. **Open Frontend**: http://localhost:4200
2. **API Documentation**: https://localhost:5001/swagger
3. **API Health Check**: https://localhost:5001/api/auth/login (POST only, will fail without credentials - but shows API is running)

### Default Login Credentials

You'll need to create an admin user first. See [README.md](./README.md) for setup instructions.

## Troubleshooting

### "dotnet not found"
- Install .NET 8 SDK from https://dotnet.microsoft.com/download
- Restart your terminal after installation
- Verify with: `dotnet --version`

### "node not found"
- Install Node.js from https://nodejs.org
- Restart your terminal after installation
- Verify with: `node --version`

### "ng command not found"
- The `ng` command will be installed locally
- Windows/Mac: Use `npm start` instead of `ng serve`
- The scripts handle this automatically

### Frontend won't connect to API
- Ensure backend is running on 5001: `https://localhost:5001/swagger`
- Check browser console (F12) for CORS errors
- Frontend may need HTTPS; Angular dev server should handle this
- Restart both services if needed

### Port Already in Use
- **Port 5001 (Backend)**:
  ```bash
  # macOS/Linux
  lsof -i :5001
  kill -9 <PID>
  
  # Windows
  netstat -ano | findstr :5001
  taskkill /PID <PID> /F
  ```

- **Port 4200 (Frontend)**:
  ```bash
  # macOS/Linux
  lsof -i :4200
  kill -9 <PID>
  
  # Windows
  netstat -ano | findstr :4200
  taskkill /PID <PID> /F
  ```

### Database Issues
- SQLite database file: `payslip4all.db`
- Location: Project root directory
- If corrupted: Delete and re-run `dotnet ef database update`

### Performance Issues
- Close other applications using ports 5001/4200
- Ensure you have adequate disk space
- Node.js modules can be large; give `npm install` time to complete

## Advanced Usage

### View Logs (start-all.sh)
```bash
# Backend logs
tail -f logs/backend.log

# Frontend logs
tail -f logs/frontend.log
```

### Custom Configuration
Edit these files to change default settings:
- Backend: `src/Payslip4All.Api/appsettings.json`
- Frontend: `src/frontend/Payslip4All.Web/angular.json`

### Database Backups
```bash
# Copy database to backup location
cp payslip4all.db payslip4all.backup.db

# Restore from backup
cp payslip4all.backup.db payslip4all.db
```

## File Descriptions

| File | Purpose | Platform |
|------|---------|----------|
| `start-backend.sh` | Start API server | Linux/Mac |
| `start-backend.bat` | Start API server | Windows |
| `start-frontend.sh` | Start Angular dev server | Linux/Mac |
| `start-frontend.bat` | Start Angular dev server | Windows |
| `start-all.sh` | Start both services | Linux/Mac |
| `start-all.bat` | Start both services | Windows |
| `stop-all.sh` | Stop all services | Linux/Mac |
| `stop-all.bat` | Stop all services | Windows |

## Getting Help

For detailed setup instructions, see:
- [README.md](./README.md) - Full project documentation
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Technical architecture
- [PRODUCT.md](./PRODUCT.md) - Product requirements

For issues or questions:
1. Check the troubleshooting section above
2. Review console/log output for error messages
3. Ensure all prerequisites are installed
4. Try stopping and restarting the services
