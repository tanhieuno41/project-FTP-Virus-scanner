# Secure FTP Client - Virus Scanning

## 📋 Mô tả
Secure FTP Client là một ứng dụng WPF hiện đại với giao diện đẹp mắt, hỗ trợ kết nối FTP/FTPS với tính năng quét virus tích hợp sử dụng ClamAV Agent.

## ✨ Tính năng chính

### 🔐 Kết nối FTP/FTPS
- **Kết nối thường**: FTP cơ bản
- **Kết nối bảo mật**: FTPS với SSL/TLS
- **Hỗ trợ Passive Mode**: Tự động xử lý NAT/Firewall
- **Lưu cấu hình**: Ghi nhớ thông tin đăng nhập

### 🛡️ Virus Scanning
- **Tích hợp ClamAV**: Quét virus real-time
- **Fallback Scan**: Phân tích file khi ClamAV không có sẵn
- **Upload Protection**: Tự động quét trước khi upload
- **Scan Results**: Hiển thị kết quả chi tiết

### 📁 File Management
- **Local File Browser**: Duyệt file local với TreeView
- **Server File Browser**: Hiển thị file server với DataGrid
- **Drag & Drop**: Kéo thả file để upload
- **Multiple Selection**: Chọn nhiều file cùng lúc
- **Context Menu**: Menu chuột phải với các tùy chọn
- **File Properties**: Xem thông tin chi tiết file

### 📤 Upload Management
- **Upload Queue**: Quản lý hàng đợi upload
- **Progress Tracking**: Theo dõi tiến trình upload
- **Batch Upload**: Upload nhiều file cùng lúc
- **Queue Control**: Start/Stop/Clear queue

### 🔄 Transfer Features
- **Binary/ASCII Mode**: Chọn chế độ transfer
- **Progress Bar**: Hiển thị tiến trình tổng thể
- **Status Updates**: Cập nhật trạng thái real-time
- **Error Handling**: Xử lý lỗi gracefully

## 🚀 Cách sử dụng

### 1. Kết nối Server
1. Nhập thông tin server (địa chỉ, port, username, password)
2. Chọn "Connect" cho FTP thường hoặc "Secure Connect" cho FTPS
3. Kiểm tra trạng thái kết nối

### 2. Cấu hình Virus Scanning
1. Click "Test Connection" để kiểm tra ClamAV Agent
2. Đảm bảo ClamAV Agent đang chạy (localhost:9999)
3. Xem trạng thái "Connected" hoặc "Disconnected"

### 3. Duyệt File
- **Local Files**: Sử dụng panel bên trái để duyệt file local
- **Server Files**: Sử dụng panel giữa để xem file server
- **Navigation**: Double-click để vào thư mục, sử dụng nút Browse/Refresh

### 4. Upload File
- **Single Upload**: Click "Upload File" và chọn file
- **Drag & Drop**: Kéo file từ local vào upload queue
- **Batch Upload**: Chọn nhiều file và thêm vào queue
- **Start Upload**: Click "Start Upload" để bắt đầu

### 5. Download File
- **Single Download**: Double-click file hoặc chuột phải → Download
- **Multiple Download**: Chọn nhiều file và sử dụng context menu

### 6. File Operations
- **Rename**: Chuột phải → Rename
- **Delete**: Chuột phải → Delete
- **Properties**: Chuột phải → Properties
- **Create Directory**: Sử dụng nút MKDIR

## 🎨 Giao diện

### Dark Theme
- Giao diện tối hiện đại với màu sắc dễ chịu
- Custom title bar với nút minimize/close
- Responsive layout với grid system

### File Browser
- **Local Panel**: TreeView với drag & drop support
- **Server Panel**: DataGrid với sorting và filtering
- **Upload Queue**: ListBox với progress bars

### Status Indicators
- **Connection Status**: Hiển thị trạng thái kết nối
- **ClamAV Status**: Hiển thị trạng thái virus scanning
- **Progress Bar**: Theo dõi tiến trình transfer
- **Log Panel**: Hiển thị log chi tiết

## 🔧 Cấu hình

### Settings
- **Window Position**: Tự động lưu vị trí và kích thước cửa sổ
- **Connection Info**: Lưu thông tin server cuối cùng
- **Transfer Mode**: Mặc định Binary/ASCII mode

### ClamAV Agent
- **Host**: localhost (mặc định)
- **Port**: 9999 (mặc định)
- **Auto Detection**: Tự động phát hiện ClamAV
- **Fallback Mode**: Hoạt động khi ClamAV không có sẵn

## 🛠️ Troubleshooting

### Kết nối FTP
- Kiểm tra thông tin server và credentials
- Thử chế độ Passive Mode
- Kiểm tra firewall và network

### Virus Scanning
- Đảm bảo ClamAV Agent đang chạy
- Kiểm tra port 9999 không bị block
- Xem log để debug connection issues

### File Operations
- Kiểm tra quyền truy cập file/folder
- Đảm bảo đủ disk space
- Xem log để debug transfer errors

## 📊 Log Types
- **Info**: Thông tin chung
- **Command**: FTP commands
- **Response**: Server responses
- **Success**: Thành công
- **Error**: Lỗi
- **Warning**: Cảnh báo

## 🔒 Bảo mật
- **SSL/TLS Support**: Kết nối mã hóa
- **Virus Scanning**: Bảo vệ khỏi malware
- **Secure Credentials**: Không lưu password dạng plain text
- **Connection Validation**: Kiểm tra certificate

## 📝 System Requirements
- **OS**: Windows 10/11
- **.NET**: .NET Framework 4.8
- **Memory**: 512MB RAM
- **Disk**: 100MB free space
- **Network**: Internet connection cho FTP

## 🚀 Performance Tips
- Sử dụng Binary mode cho file lớn
- Enable Passive mode nếu gặp vấn đề NAT
- Sử dụng upload queue cho nhiều file
- Đóng các ứng dụng không cần thiết khi transfer file lớn

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   WPF Client    │    │  ClamAV Agent   │    │   FTP Server    │
│                 │    │                 │    │                 │
│ • Modern UI     │───▶│ • Virus Scanner │    │ • File Storage  │
│ • File Upload   │    │ • Socket Server │    │ • FTP Protocol  │
│ • Virus Check   │    │ • ClamAV Engine │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📋 Requirements

- .NET Framework 4.8
- Windows 10/11
- ClamAV Agent running on localhost:9999
- FTP Server (FileZilla Server, vsftpd, etc.)

## 🚀 Getting Started

### 1. Start ClamAV Agent
```bash
cd ClamAVAgent
python ClamAVAgent.py
```

### 2. Build and Run WPF Client
```bash
cd FTPClientWPF
dotnet build
dotnet run
```

### 3. Connect to FTP Server
1. Enter server details (localhost, testuser, testpass)
2. Click "Test Connection" to verify ClamAV Agent
3. Click "Connect" to connect to FTP server
4. Use "Upload File" to upload with virus scanning

## 🖥️ User Interface

### Connection Panel
- **Server**: FTP server address
- **Port**: FTP server port (default: 21)
- **Username**: FTP username
- **Password**: FTP password
- **Connect**: Standard FTP connection
- **Secure Connect**: FTPS with SSL/TLS
- **Disconnect**: Close connection

### Virus Scanning Panel
- **ClamAV Status**: Connection status to ClamAV Agent
- **Test Connection**: Test ClamAV Agent connectivity
- **Virus Scanning**: Information about last scan
- **Upload File**: Upload file with virus scanning

### File Browser
- **Directory Tree**: Navigate server directories
- **File List**: View server files with details
- **Double-click**: Select files for operations

### Log Panel
- **Real-time Logging**: All operations and status
- **Color-coded**: Different log types (Info, Success, Error)
- **Scrollable**: View complete history

## 🔧 Configuration

### ClamAV Agent Settings
Edit `MainWindow.xaml.cs` to modify:
```csharp
private string clamavHost = "localhost";
private int clamavPort = 9999;
```

### FTP Server Settings
Default test settings:
- Server: `localhost`
- Port: `21`
- Username: `testuser`
- Password: `testpass`

## 📖 Usage Examples

### Basic Connection
1. Enter server details
2. Click "Connect"
3. Browse server files
4. Upload files with virus scanning

### Virus Scanning Workflow
1. **Test ClamAV Connection**
   - Click "Test Connection"
   - Verify status shows "Connected"

2. **Upload File**
   - Click "Upload File"
   - Select file to upload
   - File is automatically scanned
   - Clean files are uploaded
   - Infected files are blocked

3. **Monitor Results**
   - Check log for scan results
   - View status in Virus Scanning panel
   - Infected files show warning dialog

## 🐛 Troubleshooting

### Common Issues

1. **ClamAV Agent Not Connected**
   - Ensure ClamAV Agent is running
   - Check host/port settings
   - Verify firewall settings

2. **FTP Connection Failed**
   - Check server address and port
   - Verify username/password
   - Ensure FTP server is running

3. **Upload Fails**
   - Check file permissions
   - Verify server write access
   - Monitor scan results

### Debug Mode
Enable detailed logging by checking the log panel for error messages.

## 🔒 Security Features

- **Virus Scanning**: All uploads scanned before transfer
- **SSL/TLS Support**: Encrypted connections
- **Secure File Transfer**: Socket-based communication with ClamAV
- **Logging**: Complete audit trail

## 📈 Performance

- **Async Operations**: Non-blocking UI during operations
- **Efficient Scanning**: Stream-based file transfer to ClamAV
- **Memory Management**: Proper resource cleanup
- **Responsive UI**: Real-time status updates

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with ClamAV Agent
5. Submit a pull request

## 📄 License

This project is provided as-is for educational purposes.

---

**Note**: This WPF client integrates with the ClamAV Agent for virus scanning. Ensure the agent is running before using the client. 