# ClamAV Installation Script for Windows (PowerShell)
# Run as Administrator

param(
    [string]$InstallPath = "C:\ClamAV",
    [switch]$SkipDatabaseUpdate,
    [switch]$Force
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ClamAV Installation Script for Windows" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ Please run this script as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Running as Administrator" -ForegroundColor Green

# Set variables
$ClamAVVersion = "1.0.1"
$ClamAVUrl = "https://www.clamav.net/downloads/production/clamav-$ClamAVVersion.win.x64.portable.zip"
$BinPath = Join-Path $InstallPath "bin"
$TempZip = Join-Path $env:TEMP "clamav.zip"
$TempExtract = Join-Path $env:TEMP "clamav_temp"

Write-Host "🔧 Installing ClamAV..." -ForegroundColor Yellow
Write-Host ""

# Check if already installed
if (Test-Path $InstallPath) {
    if ($Force) {
        Write-Host "📁 Removing existing installation..." -ForegroundColor Yellow
        Remove-Item $InstallPath -Recurse -Force
    } else {
        Write-Host "⚠️ ClamAV already installed at: $InstallPath" -ForegroundColor Yellow
        $response = Read-Host "Do you want to reinstall? (y/N)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Host "Installation cancelled." -ForegroundColor Red
            exit 0
        }
        Remove-Item $InstallPath -Recurse -Force
    }
}

# Create installation directory
Write-Host "📁 Creating directory: $InstallPath" -ForegroundColor Green
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Download ClamAV
Write-Host "📥 Downloading ClamAV..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $ClamAVUrl -OutFile $TempZip -UseBasicParsing
    Write-Host "✅ Download completed" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to download ClamAV: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Extract ClamAV
Write-Host "📦 Extracting ClamAV..." -ForegroundColor Yellow
try {
    Expand-Archive -Path $TempZip -DestinationPath $TempExtract -Force
    Write-Host "✅ Extraction completed" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to extract ClamAV: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Copy files to installation directory
Write-Host "📋 Copying files..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$TempExtract\*" -Destination $InstallPath -Recurse -Force
    Write-Host "✅ Files copied successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to copy files: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Add to PATH
Write-Host "🔗 Adding to PATH..." -ForegroundColor Yellow
try {
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($currentPath -notlike "*$BinPath*") {
        $newPath = "$currentPath;$BinPath"
        [Environment]::SetEnvironmentVariable("PATH", $newPath, "Machine")
        Write-Host "✅ Added to PATH" -ForegroundColor Green
    } else {
        Write-Host "✅ Already in PATH" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️ Failed to add to PATH automatically" -ForegroundColor Yellow
    Write-Host "Please manually add $BinPath to your PATH" -ForegroundColor Yellow
}

# Update virus database
if (-not $SkipDatabaseUpdate) {
    Write-Host "🔄 Updating virus database..." -ForegroundColor Yellow
    try {
        Push-Location $BinPath
        & ".\freshclam.exe"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Virus database updated" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Failed to update virus database" -ForegroundColor Yellow
            Write-Host "You can run 'freshclam.exe' manually later" -ForegroundColor Yellow
        }
        Pop-Location
    } catch {
        Write-Host "⚠️ Failed to update virus database: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "⏭️ Skipping database update" -ForegroundColor Yellow
}

# Test installation
Write-Host "🧪 Testing installation..." -ForegroundColor Yellow
try {
    $clamscanPath = Join-Path $BinPath "clamscan.exe"
    $result = & $clamscanPath --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ ClamAV installation successful!" -ForegroundColor Green
        Write-Host "Version: $($result[0])" -ForegroundColor Cyan
    } else {
        Write-Host "❌ ClamAV installation test failed" -ForegroundColor Red
        Write-Host "Please restart your computer and try again" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ ClamAV installation test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Cleanup
Write-Host "🧹 Cleaning up temporary files..." -ForegroundColor Yellow
try {
    if (Test-Path $TempZip) { Remove-Item $TempZip -Force }
    if (Test-Path $TempExtract) { Remove-Item $TempExtract -Recurse -Force }
    Write-Host "✅ Cleanup completed" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Failed to cleanup temporary files" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📍 ClamAV installed at: $InstallPath" -ForegroundColor White
Write-Host "🔧 Binary location: $BinPath" -ForegroundColor White
Write-Host "📝 Logs location: $InstallPath\logs" -ForegroundColor White
Write-Host ""
Write-Host "🚀 You can now run ClamAV Agent:" -ForegroundColor Green
Write-Host "   python ClamAVAgent.py" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Note: You may need to restart your terminal or computer" -ForegroundColor Yellow
Write-Host "   for PATH changes to take effect." -ForegroundColor Yellow
Write-Host ""

Read-Host "Press Enter to exit" 