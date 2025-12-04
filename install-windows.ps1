# ZIMA Windows Installer (PowerShell)
# Installs ZIMA AI Coding Assistant on Windows

Write-Host "🚀 ZIMA Installer for Windows" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "❌ This installer must be run as Administrator." -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Check for Chocolatey
Write-Host "📦 Checking for Chocolatey package manager..." -ForegroundColor Yellow
if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Chocolatey..." -ForegroundColor Yellow
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
}
Write-Host "✅ Chocolatey found" -ForegroundColor Green

# Check for Node.js
Write-Host "📦 Checking for Node.js..." -ForegroundColor Yellow
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Node.js..." -ForegroundColor Yellow
    choco install nodejs -y
    refreshenv
}
$nodeVersion = node --version
Write-Host "✅ Node.js $nodeVersion found" -ForegroundColor Green

# Check for Ollama
Write-Host "📦 Checking for Ollama..." -ForegroundColor Yellow
if (-not (Get-Command ollama -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Ollama..." -ForegroundColor Yellow
    Write-Host "Please download Ollama from: https://ollama.com/download/windows" -ForegroundColor Yellow
    Write-Host "After installing, re-run this script." -ForegroundColor Yellow
    Start-Process "https://ollama.com/download/windows"
    exit 1
}
Write-Host "✅ Ollama found" -ForegroundColor Green

# Start Ollama service (runs in background on Windows)
Write-Host "🔧 Ensuring Ollama is running..." -ForegroundColor Yellow
$ollamaProcess = Get-Process -Name "ollama" -ErrorAction SilentlyContinue
if (-not $ollamaProcess) {
    Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 3
}
Write-Host "✅ Ollama service running" -ForegroundColor Green

# Pull Qwen2.5-Coder model
Write-Host "📥 Downloading Qwen2.5-Coder-14B model (9GB)..." -ForegroundColor Yellow
Write-Host "This may take 5-15 minutes depending on your connection..." -ForegroundColor Yellow
ollama pull qwen2.5-coder:14b
Write-Host "✅ Model downloaded" -ForegroundColor Green

# Install npm dependencies
Write-Host "📦 Installing ZIMA dependencies..." -ForegroundColor Yellow
npm install --production
Write-Host "✅ Dependencies installed" -ForegroundColor Green

# Create global link
Write-Host "🔗 Creating global 'zima' command..." -ForegroundColor Yellow
npm link
Write-Host "✅ Global command created" -ForegroundColor Green

# Create config directory
$zimaDir = Join-Path $env:USERPROFILE ".zima"
if (-not (Test-Path $zimaDir)) {
    New-Item -ItemType Directory -Path $zimaDir | Out-Null
}

# Test installation
Write-Host ""
Write-Host "🧪 Testing installation..." -ForegroundColor Yellow
refreshenv
if (Get-Command zima -ErrorAction SilentlyContinue) {
    Write-Host "✅ ZIMA installed successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Command 'zima' not found. Try restarting PowerShell" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "✅ Installation Complete!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Quick Start:" -ForegroundColor White
Write-Host "1. Open a new PowerShell window" -ForegroundColor White
Write-Host "2. Type: zima" -ForegroundColor White
Write-Host "3. Start chatting with your AI coding assistant!" -ForegroundColor White
Write-Host ""
Write-Host "Documentation: $PWD\ZIMA_GUIDE.md" -ForegroundColor Gray
Write-Host "Tools Reference: $PWD\TOOLS_REFERENCE.md" -ForegroundColor Gray
Write-Host ""
Write-Host "🎉 Enjoy ZIMA!" -ForegroundColor Cyan
