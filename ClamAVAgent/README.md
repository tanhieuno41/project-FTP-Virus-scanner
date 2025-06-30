# ClamAV Agent - Virus Scanning Service

## 📋 Mô tả
ClamAV Agent là một service Python chạy trên server để quét virus cho các file được upload từ FTP Client. Service này sử dụng ClamAV để thực hiện việc quét virus và trả về kết quả cho client.

## 🚀 Cài đặt

### 1. Cài đặt Python dependencies
```bash
# Không cần cài thêm thư viện, chỉ sử dụng thư viện có sẵn của Python
```

### 2. Cài đặt ClamAV
#### Trên Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install clamav clamav-daemon
sudo freshclam  # Cập nhật virus database
```

#### Trên CentOS/RHEL:
```bash
sudo yum install clamav clamav-update
sudo freshclam
```

#### Trên Windows:
1. **Tải ClamAV từ trang chủ**:
   - Truy cập: https://www.clamav.net/downloads
   - Tải phiên bản Windows mới nhất
   - Hoặc sử dụng link trực tiếp: https://www.clamav.net/downloads/production/clamav-1.0.1.win.x64.portable.zip

2. **Cài đặt ClamAV**:
   ```cmd
   # Giải nén file zip
   # Copy thư mục ClamAV vào C:\ClamAV
   # Hoặc C:\Program Files\ClamAV
   ```

3. **Thêm vào PATH**:
   ```cmd
   # Mở System Properties > Environment Variables
   # Thêm C:\ClamAV\bin vào PATH
   # Hoặc C:\Program Files\ClamAV\bin
   ```

4. **Cập nhật virus database**:
   ```cmd
   cd C:\ClamAV\bin
   freshclam.exe
   ```

5. **Kiểm tra cài đặt**:
   ```cmd
   clamscan.exe --version
   ```

**Lưu ý**: Nếu không cài đặt ClamAV, ClamAV Agent sẽ tự động sử dụng fallback scan method.

### 3. Kiểm tra cài đặt
```bash
clamscan --version
```

## 🚀 Chạy ClamAV Agent

### 1. Chạy trực tiếp
```bash
cd ClamAVAgent
python3 ClamAVAgent.py
```

### 2. Cài đặt ClamAV trên Windows (Tự động)
#### Sử dụng PowerShell (Khuyến nghị):
```powershell
# Chạy PowerShell as Administrator
.\install_clamav_windows.ps1

# Hoặc với tùy chọn
.\install_clamav_windows.ps1 -InstallPath "D:\ClamAV" -Force
```

#### Sử dụng Batch script:
```cmd
# Chạy Command Prompt as Administrator
install_clamav_windows.bat
```

### 3. Chạy như service (Linux)
```bash
# Tạo service file
sudo nano /etc/systemd/system/clamav-agent.service
```

Thêm nội dung:
```ini
[Unit]
Description=ClamAV Agent Service
After=network.target

[Service]
Type=simple
User=clamav
WorkingDirectory=/path/to/ClamAVAgent
ExecStart=/usr/bin/python3 /path/to/ClamAVAgent/ClamAVAgent.py
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
# Enable và start service
sudo systemctl enable clamav-agent
sudo systemctl start clamav-agent
sudo systemctl status clamav-agent
```

## 🔧 Cấu hình

### Thay đổi host và port
Sửa trong file `ClamAVAgent.py`:
```python
agent = ClamAVAgent(host='0.0.0.0', port=9999)
```

### Thay đổi thư mục temp
```python
self.temp_dir = "temp_files"  # Thay đổi đường dẫn này
```

## 📊 Protocol Communication

### Request từ WPF Client:
```json
{
    "filename": "test.txt",
    "filesize": 1024
}
```

### Response từ ClamAV Agent:
```json
{
    "Status": "OK",
    "Details": "File is clean"
}
```

### Các trạng thái có thể:
- `OK`: File sạch
- `INFECTED`: File bị nhiễm virus
- `ERROR`: Lỗi quét

## 📝 Logs

### File log: `clamav_agent.log`
```
2024-01-01 10:00:00 - INFO - ClamAV Agent started on 0.0.0.0:9999
2024-01-01 10:01:00 - INFO - Connection from ('192.168.1.100', 12345)
2024-01-01 10:01:01 - INFO - Receiving file: test.txt (1024 bytes)
2024-01-01 10:01:02 - INFO - Scan result for test.txt: OK - File is clean
```

### Console output:
```
=== ClamAV Agent - Virus Scanning Service ===
🔧 Starting ClamAV Agent...
✅ ClamAV Agent is running on 0.0.0.0:9999
Waiting for connections from FTP Client...
🔗 New connection from ('192.168.1.100', 12345)
📁 Receiving: test.txt (1024 bytes)
✅ File received: test.txt
🟢 Scan result: test.txt - OK
```

## 🛠️ Troubleshooting

### 1. Lỗi "ClamAV not found"
```bash
# Kiểm tra cài đặt
which clamscan
clamscan --version

# Cài đặt lại nếu cần
sudo apt-get install clamav
```

### 2. Lỗi "Permission denied"
```bash
# Cấp quyền cho thư mục temp
chmod 755 temp_files
```

### 3. Lỗi "Connection refused"
- Kiểm tra firewall
- Kiểm tra port có đang được sử dụng không
- Kiểm tra WPF Client có kết nối đúng host/port không

### 4. Lỗi "Scan timeout"
- File quá lớn
- ClamAV database cũ
- Hệ thống thiếu tài nguyên

## 🔒 Bảo mật

### 1. Firewall
```bash
# Chỉ cho phép kết nối từ IP cụ thể
sudo ufw allow from 192.168.1.0/24 to any port 9999
```

### 2. Chạy với user riêng
```bash
sudo useradd -r -s /bin/false clamav-agent
sudo chown clamav-agent:clamav-agent /path/to/ClamAVAgent
```

### 3. SSL/TLS (nếu cần)
Có thể mở rộng để hỗ trợ SSL/TLS cho kết nối an toàn hơn.

## 📈 Monitoring

### 1. Kiểm tra status
```bash
# Nếu chạy như service
sudo systemctl status clamav-agent

# Kiểm tra process
ps aux | grep ClamAVAgent

# Kiểm tra port
netstat -tlnp | grep 9999
```

### 2. Kiểm tra logs
```bash
tail -f clamav_agent.log
journalctl -u clamav-agent -f
```

## 🚀 Performance

### 1. Tối ưu ClamAV
```bash
# Cập nhật database thường xuyên
sudo freshclam

# Sử dụng daemon mode (nếu cần)
sudo systemctl start clamav-daemon
```

### 2. Monitoring resources
```bash
# Kiểm tra CPU/Memory usage
top -p $(pgrep -f ClamAVAgent)

# Kiểm tra disk usage
du -sh temp_files/
```

## 📞 Support

Nếu gặp vấn đề, kiểm tra:
1. Logs trong `clamav_agent.log`
2. Console output
3. ClamAV installation
4. Network connectivity
5. File permissions 

## 🔧 Fallback Scan Method

Nếu ClamAV không được cài đặt hoặc không hoạt động, ClamAV Agent sẽ tự động sử dụng **Fallback Scan Method**:

### ✅ **Fallback Scan Features:**
- **File Extension Check**: Kiểm tra các file extension đáng ngờ (.exe, .bat, .cmd, .scr, .pif, .com, .vbs, .js)
- **File Size Analysis**: Cảnh báo với file quá lớn (>100MB)
- **Content Analysis**: Quét nội dung text files tìm patterns đáng ngờ
- **Basic Validation**: Kiểm tra file integrity

### 📊 **Fallback Scan Results:**
- `OK`: File có vẻ an toàn
- `WARNING`: File có dấu hiệu đáng ngờ
- `ERROR`: Lỗi quét

### 💡 **Khi nào sử dụng Fallback:**
- ClamAV chưa được cài đặt
- ClamAV không tìm thấy trong PATH
- Lỗi khi chạy ClamAV
- Timeout khi quét file lớn 