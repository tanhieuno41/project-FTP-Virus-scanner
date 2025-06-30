#!/usr/bin/env python3
"""
Quick Test for ClamAV Agent Fallback Scan
"""

import os
import json
import socket
import time

def test_fallback_scan():
    """Test fallback scan method"""
    print("🧪 Testing ClamAV Agent Fallback Scan")
    print("=" * 50)
    
    # Create test files
    test_files = [
        ("clean.txt", "This is a clean text file with normal content."),
        ("suspicious.bat", "cmd.exe /c echo virus"),
        ("large_file.txt", "Large content. " * 10000),  # ~200KB
        ("virus_mention.txt", "This file mentions virus and malware patterns.")
    ]
    
    # Start ClamAV Agent in background (simulate)
    print("🔧 Starting ClamAV Agent (simulated)...")
    
    for filename, content in test_files:
        print(f"\n📁 Testing file: {filename}")
        print("-" * 30)
        
        # Create test file
        with open(filename, 'w') as f:
            f.write(content)
        
        file_size = os.path.getsize(filename)
        print(f"📊 File size: {file_size} bytes")
        
        # Simulate scan result
        result = simulate_scan(filename, file_size, content)
        
        # Display result
        status = result.get('Status', 'UNKNOWN')
        details = result.get('Details', 'No details')
        
        if status == 'OK':
            print(f"🟢 Result: {status} - {details}")
        elif status == 'WARNING':
            print(f"🟡 Result: {status} - {details}")
        elif status == 'INFECTED':
            print(f"🔴 Result: {status} - {details}")
        else:
            print(f"⚠️ Result: {status} - {details}")
        
        # Cleanup
        os.remove(filename)
    
    print("\n" + "=" * 50)
    print("✅ Fallback scan test completed!")

def simulate_scan(filename, file_size, content):
    """Simulate the fallback scan logic"""
    file_ext = os.path.splitext(filename)[1].lower()
    
    # Check file extension for suspicious files
    suspicious_extensions = ['.exe', '.bat', '.cmd', '.scr', '.pif', '.com', '.vbs', '.js']
    
    if file_ext in suspicious_extensions:
        return {
            'Status': 'WARNING',
            'Details': f'Potentially suspicious file type: {file_ext} (fallback scan)'
        }
    
    # Check file size (very large files might be suspicious)
    if file_size > 100 * 1024 * 1024:  # 100MB
        return {
            'Status': 'WARNING',
            'Details': f'Large file detected: {file_size} bytes (fallback scan)'
        }
    
    # Check for common virus signatures in text files
    if file_ext in ['.txt', '.log', '.cfg', '.ini']:
        content_lower = content.lower()
        suspicious_patterns = [
            'virus', 'malware', 'trojan', 'worm', 'spyware',
            'cmd.exe', 'powershell', 'regsvr32', 'rundll32'
        ]
        
        for pattern in suspicious_patterns:
            if pattern in content_lower:
                return {
                    'Status': 'WARNING',
                    'Details': f'Suspicious content detected: {pattern} (fallback scan)'
                }
    
    return {
        'Status': 'OK',
        'Details': f'File appears clean (fallback scan, {file_size} bytes)'
    }

def test_clamav_detection():
    """Test ClamAV detection"""
    print("\n🔍 Testing ClamAV Detection")
    print("=" * 30)
    
    import subprocess
    
    # Try to find clamscan
    clamscan_path = None
    
    # Try to find clamscan in PATH
    try:
        result = subprocess.run(['clamscan', '--version'], 
                              capture_output=True, text=True, timeout=5)
        if result.returncode == 0:
            clamscan_path = 'clamscan'
            print("✅ ClamAV found in PATH")
    except (subprocess.TimeoutExpired, FileNotFoundError):
        print("❌ ClamAV not found in PATH")
    
    # Try Windows paths if not found
    if not clamscan_path:
        windows_paths = [
            r'C:\Program Files\ClamAV\bin\clamscan.exe',
            r'C:\ClamAV\bin\clamscan.exe',
            r'C:\Program Files (x86)\ClamAV\bin\clamscan.exe'
        ]
        
        for path in windows_paths:
            if os.path.exists(path):
                clamscan_path = path
                print(f"✅ ClamAV found at: {path}")
                break
    
    if not clamscan_path:
        print("❌ ClamAV not found anywhere")
        print("💡 Will use fallback scan method")
    else:
        print(f"🔧 ClamAV path: {clamscan_path}")

if __name__ == "__main__":
    test_clamav_detection()
    test_fallback_scan() 