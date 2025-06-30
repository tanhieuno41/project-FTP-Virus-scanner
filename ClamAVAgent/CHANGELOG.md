# ClamAV Agent - Changelog

## Version 1.1.0 - 2024-06-29

### 🐛 Bug Fixes
- **Fixed Windows ClamAV detection**: Added support for Windows ClamAV paths
- **Fixed protocol mismatch**: Corrected JSON response format to match WPF Client expectations
- **Fixed file cleanup**: Improved temp file cleanup in finally block
- **Fixed error handling**: Better handling of ClamAV not found scenarios

### ✨ New Features
- **Fallback Scan Method**: Automatic fallback when ClamAV is not available
  - File extension analysis for suspicious files
  - File size validation
  - Content pattern detection
  - Basic file integrity checks
- **Windows Installation Scripts**: Automated ClamAV installation
  - PowerShell script with advanced options
  - Batch script for simple installation
  - Automatic PATH configuration
  - Virus database updates
- **Enhanced Logging**: Better console output with emojis and status indicators
- **Improved Error Messages**: More descriptive error messages for troubleshooting

### 🔧 Improvements
- **Better ClamAV Detection**: 
  - Checks PATH first
  - Searches common Windows installation paths
  - Graceful fallback to alternative scan method
- **Enhanced File Handling**:
  - Unique temp filenames with timestamps
  - Better file transfer validation
  - Improved cleanup procedures
- **Socket Improvements**:
  - Added timeout settings (30 seconds)
  - Better connection handling
  - Daemon threads for proper shutdown
- **Documentation**: 
  - Comprehensive README with installation guides
  - Troubleshooting section
  - Performance optimization tips

### 📊 Protocol Changes
- **Response Format**: Changed from `result` to `Status` to match WPF Client
- **Error Handling**: Consistent error response format
- **Status Codes**: Added `WARNING` status for fallback scan results

### 🛠️ Technical Details
- **Fallback Scan Features**:
  - Suspicious extensions: `.exe`, `.bat`, `.cmd`, `.scr`, `.pif`, `.com`, `.vbs`, `.js`
  - Large file detection: >100MB warning
  - Content analysis: Virus/malware pattern detection
  - File size reporting in results
- **Windows Paths Supported**:
  - `C:\Program Files\ClamAV\bin\clamscan.exe`
  - `C:\ClamAV\bin\clamscan.exe`
  - `C:\Program Files (x86)\ClamAV\bin\clamscan.exe`

### 📝 Files Added
- `install_clamav_windows.bat` - Windows batch installation script
- `install_clamav_windows.ps1` - PowerShell installation script
- `quick_test.py` - Fallback scan testing utility
- `test_client.py` - ClamAV Agent testing client
- `CHANGELOG.md` - This changelog file

### 🔄 Migration Notes
- **Backward Compatibility**: All existing functionality preserved
- **Automatic Fallback**: No configuration needed, works automatically
- **Enhanced Logging**: More informative console output
- **Better Error Handling**: More robust error recovery

## Version 1.0.0 - Initial Release

### ✨ Features
- Basic ClamAV integration
- Socket-based file transfer
- JSON communication protocol
- Multi-threading support
- Basic logging system 