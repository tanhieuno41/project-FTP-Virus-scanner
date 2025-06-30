using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FTPClientWPF.Models
{
    public class UploadItem : INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private long _fileSize;
        private int _progress;
        private string _status;
        private bool _isCompleted;
        private bool _hasError;

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

        public UploadItem(string filePath)
        {
            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
            FileSize = new System.IO.FileInfo(filePath).Length;
            Progress = 0;
            Status = "Queued";
            IsCompleted = false;
            HasError = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 