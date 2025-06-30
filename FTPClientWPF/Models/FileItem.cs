using System;

namespace FTPClientWPF.Models
{
    public class FileItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsSelected { get; set; }

        public string FileSizeFormatted
        {
            get
            {
                if (IsDirectory) return "<DIR>";
                
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

        public string ModifiedDateFormatted
        {
            get
            {
                return ModifiedDate.ToString("yyyy-MM-dd HH:mm");
            }
        }

        //public string IconPath
        //{
        //    get
        //    {
        //        if (IsDirectory)
        //            return "/Images/folder.png";
                
        //        string extension = System.IO.Path.GetExtension(FileName).ToLower();
        //        if (extension == ".txt") return "/Images/text.png";
        //        if (extension == ".pdf") return "/Images/pdf.png";
        //        if (extension == ".doc" || extension == ".docx") return "/Images/word.png";
        //        if (extension == ".xls" || extension == ".xlsx") return "/Images/excel.png";
        //        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif") return "/Images/image.png";
        //        if (extension == ".mp3" || extension == ".wav") return "/Images/audio.png";
        //        if (extension == ".mp4" || extension == ".avi") return "/Images/video.png";
        //        if (extension == ".zip" || extension == ".rar") return "/Images/archive.png";
        //        if (extension == ".exe") return "/Images/executable.png";
        //        return "/Images/file.png";
        //    }
        //}
    }
} 