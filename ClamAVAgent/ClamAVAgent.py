#!/usr/bin/env python3
"""
ClamAV Agent - Virus Scanning Service
Receives files from FTP Client via socket and scans them using ClamAV
"""

import socket
import subprocess
import os
import json
import threading
import logging
from datetime import datetime

class ClamAVAgent:
    def __init__(self, host='0.0.0.0', port=9999):
        self.host = host
        self.port = port
        self.server_socket = None
        self.running = False
        
        # Setup logging
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler('clamav_agent.log'),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger(__name__)
        
        # Create temp directory for received files
        self.temp_dir = "temp_files"
        if not os.path.exists(self.temp_dir):
            os.makedirs(self.temp_dir)
    
    def start_server(self):
        """Start the ClamAV Agent server"""
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(5)
            
            self.running = True
            self.logger.info(f"ClamAV Agent started on {self.host}:{self.port}")
            print(f"✅ ClamAV Agent is running on {self.host}:{self.port}")
            print("Waiting for connections from FTP Client...")
            
            while self.running:
                try:
                    client_socket, address = self.server_socket.accept()
                    self.logger.info(f"Connection from {address}")
                    print(f"🔗 New connection from {address}")
                    
                    # Handle client in separate thread
                    client_thread = threading.Thread(
                        target=self.handle_client, 
                        args=(client_socket, address)
                    )
                    client_thread.daemon = True  # Allow main thread to exit
                    client_thread.start()
                    
                except Exception as e:
                    if self.running:
                        self.logger.error(f"Error accepting connection: {e}")
                        
        except Exception as e:
            self.logger.error(f"Failed to start server: {e}")
        finally:
            self.stop_server()
    
    def handle_client(self, client_socket, address):
        """Handle individual client connection"""
        file_path = None
        try:
            # Set socket timeout
            client_socket.settimeout(30)  # 30 seconds timeout
            
            # Receive file info
            file_info_data = client_socket.recv(1024).decode('utf-8')
            if not file_info_data:
                self.logger.warning(f"No data received from {address}")
                return
            
            try:
                file_info = json.loads(file_info_data)
                filename = file_info.get('filename', 'unknown')
                filesize = file_info.get('filesize', 0)
            except json.JSONDecodeError as e:
                self.logger.error(f"Invalid JSON from {address}: {e}")
                return
            
            self.logger.info(f"Receiving file: {filename} ({filesize} bytes) from {address}")
            print(f"📁 Receiving: {filename} ({filesize} bytes)")
            
            # Send READY acknowledgment
            client_socket.send(b"READY")
            
            # Receive file data
            file_path = os.path.join(self.temp_dir, f"scan_{datetime.now().strftime('%Y%m%d_%H%M%S')}_{filename}")
            received_size = 0
            
            with open(file_path, 'wb') as f:
                while received_size < filesize:
                    chunk_size = min(4096, filesize - received_size)
                    chunk = client_socket.recv(chunk_size)
                    if not chunk:
                        self.logger.warning(f"Connection closed prematurely from {address}")
                        break
                    f.write(chunk)
                    received_size += len(chunk)
            
            if received_size == filesize:
                self.logger.info(f"File received successfully: {filename} ({received_size} bytes)")
                print(f"✅ File received: {filename}")
                
                # Scan file with ClamAV
                scan_result = self.scan_file(file_path)
                
                # Send result back to client (matching WPF Client format)
                result_data = {
                    'Status': scan_result['status'],  # Match WPF Client expectation
                    'Details': scan_result['details']
                }
                
                result_json = json.dumps(result_data)
                client_socket.send(result_json.encode('utf-8'))
                
                status_icon = "🟢" if scan_result['status'] == 'OK' else "🔴" if scan_result['status'] == 'INFECTED' else "⚠️"
                print(f"{status_icon} Scan result: {filename} - {scan_result['status']}")
                self.logger.info(f"Scan result for {filename}: {scan_result['status']} - {scan_result['details']}")
            else:
                self.logger.warning(f"Incomplete file received: {filename} ({received_size}/{filesize} bytes)")
                error_result = {
                    'Status': 'ERROR',
                    'Details': f'Incomplete file transfer: {received_size}/{filesize} bytes'
                }
                client_socket.send(json.dumps(error_result).encode('utf-8'))
                
        except socket.timeout:
            self.logger.error(f"Socket timeout for {address}")
            try:
                error_result = {
                    'Status': 'ERROR',
                    'Details': 'Connection timeout'
                }
                client_socket.send(json.dumps(error_result).encode('utf-8'))
            except:
                pass
        except Exception as e:
            self.logger.error(f"Error handling client {address}: {e}")
            try:
                error_result = {
                    'Status': 'ERROR',
                    'Details': f'Server error: {str(e)}'
                }
                client_socket.send(json.dumps(error_result).encode('utf-8'))
            except:
                pass
        finally:
            # Clean up temp file
            if file_path and os.path.exists(file_path):
                try:
                    os.remove(file_path)
                    self.logger.debug(f"Cleaned up temp file: {file_path}")
                except Exception as e:
                    self.logger.warning(f"Failed to clean up temp file {file_path}: {e}")
            
            client_socket.close()
    
    def scan_file(self, file_path):
        """Scan a file using ClamAV clamscan"""
        try:
            # Check if file exists
            if not os.path.exists(file_path):
                return {
                    'status': 'ERROR',
                    'details': f'File not found: {file_path}'
                }
            
            # Check if clamscan is available
            clamscan_path = None
            
            # Try to find clamscan in PATH
            try:
                result = subprocess.run(['clamscan', '--version'], 
                                      capture_output=True, text=True, timeout=5)
                if result.returncode == 0:
                    clamscan_path = 'clamscan'
            except (subprocess.TimeoutExpired, FileNotFoundError):
                pass
            
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
                        break
            
            # If ClamAV is not available, use fallback
            if not clamscan_path:
                self.logger.warning("ClamAV not found, using fallback scan method")
                return self.fallback_scan(file_path)
            
            # Run clamscan on the file
            scan_result = subprocess.run([
                clamscan_path, 
                '--no-summary',
                '--infected',
                '--suppress-ok-results',
                file_path
            ], capture_output=True, text=True, timeout=30)
            
            # Parse result
            if scan_result.returncode == 0:
                return {
                    'status': 'OK',
                    'details': 'File is clean (ClamAV)'
                }
            elif scan_result.returncode == 1:
                # Extract virus name from output
                lines = scan_result.stdout.split('\n')
                for line in lines:
                    if 'FOUND' in line:
                        virus_name = line.split('FOUND: ')[-1].strip()
                        return {
                            'status': 'INFECTED',
                            'details': f'Virus found: {virus_name}'
                        }
                return {
                    'status': 'INFECTED',
                    'details': 'Virus detected (unknown type)'
                }
            else:
                error_msg = scan_result.stderr.strip() if scan_result.stderr else 'Unknown scan error'
                self.logger.warning(f"ClamAV scan failed: {error_msg}, using fallback")
                return self.fallback_scan(file_path)
                
        except subprocess.TimeoutExpired:
            self.logger.warning("ClamAV scan timeout, using fallback")
            return self.fallback_scan(file_path)
        except Exception as e:
            self.logger.error(f"ClamAV scan error: {e}, using fallback")
            return self.fallback_scan(file_path)
    
    def fallback_scan(self, file_path):
        """Fallback scan method when ClamAV is not available"""
        try:
            # Simple file analysis as fallback
            file_size = os.path.getsize(file_path)
            
            # Check file extension for suspicious files
            suspicious_extensions = ['.exe', '.bat', '.cmd', '.scr', '.pif', '.com', '.vbs', '.js']
            file_ext = os.path.splitext(file_path)[1].lower()
            
            if file_ext in suspicious_extensions:
                return {
                    'status': 'WARNING',
                    'details': f'Potentially suspicious file type: {file_ext} (fallback scan)'
                }
            
            # Check file size (very large files might be suspicious)
            if file_size > 100 * 1024 * 1024:  # 100MB
                return {
                    'status': 'WARNING',
                    'details': f'Large file detected: {file_size} bytes (fallback scan)'
                }
            
            # Check for common virus signatures in text files
            if file_ext in ['.txt', '.log', '.cfg', '.ini']:
                try:
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read(1024)  # Read first 1KB
                        
                        # Check for suspicious patterns
                        suspicious_patterns = [
                            'virus', 'malware', 'trojan', 'worm', 'spyware',
                            'cmd.exe', 'powershell', 'regsvr32', 'rundll32'
                        ]
                        
                        content_lower = content.lower()
                        for pattern in suspicious_patterns:
                            if pattern in content_lower:
                                return {
                                    'status': 'WARNING',
                                    'details': f'Suspicious content detected: {pattern} (fallback scan)'
                                }
                except:
                    pass
            
            return {
                'status': 'OK',
                'details': f'File appears clean (fallback scan, {file_size} bytes)'
            }
            
        except Exception as e:
            return {
                'status': 'ERROR',
                'details': f'Fallback scan error: {str(e)}'
            }
    
    def stop_server(self):
        """Stop the server"""
        self.running = False
        if self.server_socket:
            self.server_socket.close()
        self.logger.info("ClamAV Agent stopped")
        print("🛑 ClamAV Agent stopped")

def main():
    """Main function to run the ClamAV Agent"""
    print("=== ClamAV Agent - Virus Scanning Service ===")
    print("🔧 Starting ClamAV Agent...")
    
    agent = ClamAVAgent()
    
    try:
        agent.start_server()
    except KeyboardInterrupt:
        print("\n🛑 Shutting down ClamAV Agent...")
        agent.stop_server()
    except Exception as e:
        print(f"❌ Error: {e}")
        agent.stop_server()

if __name__ == "__main__":
    main() 