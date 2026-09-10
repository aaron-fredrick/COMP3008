using System;
using System.IO;
using Chat.Contracts.SharedTypes;

namespace Chat.Client.Shared.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }

    public class ValidationService
    {
        private const int MaxUsernameLength = 50;
        private const int MaxChannelNameLength = 50;
        private const int MaxMessageLength = 1000;
        private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

        private static readonly string[] SupportedFileExtensions = 
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".txt"
        };

        public ValidationResult ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return ValidationResult.Failure("Username cannot be empty.");
            }

            if (username.Length > MaxUsernameLength)
            {
                return ValidationResult.Failure($"Username cannot exceed {MaxUsernameLength} characters.");
            }

            // Check for invalid characters (only allow alphanumeric, underscore, hyphen)
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return ValidationResult.Failure("Username can only contain letters, numbers, underscores, and hyphens.");
                }
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateChannelName(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                return ValidationResult.Failure("Channel name cannot be empty.");
            }

            if (channelName.Length > MaxChannelNameLength)
            {
                return ValidationResult.Failure($"Channel name cannot exceed {MaxChannelNameLength} characters.");
            }

            // Check for invalid characters
            foreach (char c in channelName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ' ')
                {
                    return ValidationResult.Failure("Channel name can only contain letters, numbers, underscores, hyphens, and spaces.");
                }
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return ValidationResult.Failure("Message cannot be empty.");
            }

            if (message.Length > MaxMessageLength)
            {
                return ValidationResult.Failure($"Message cannot exceed {MaxMessageLength} characters.");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ValidationResult.Failure("File path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                return ValidationResult.Failure("File does not exist.");
            }

            var fileInfo = new FileInfo(filePath);
            
            // Check file size
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return ValidationResult.Failure($"File size exceeds 2MB limit. Current size: {FormatFileSize(fileInfo.Length)}");
            }

            // Check file extension
            string extension = fileInfo.Extension.ToLower();
            bool isSupported = false;
            foreach (string supportedExtension in SupportedFileExtensions)
            {
                if (extension == supportedExtension)
                {
                    isSupported = true;
                    break;
                }
            }

            if (!isSupported)
            {
                return ValidationResult.Failure($"File type '{extension}' is not supported. Supported types: {string.Join(", ", SupportedFileExtensions)}");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateFileData(byte[] fileData, string fileName)
        {
            if (fileData == null || fileData.Length == 0)
            {
                return ValidationResult.Failure("File data cannot be empty.");
            }

            // Check file size
            if (fileData.Length > MaxFileSizeBytes)
            {
                return ValidationResult.Failure($"File size exceeds 2MB limit. Current size: {FormatFileSize(fileData.Length)}");
            }

            // Check file extension
            string extension = Path.GetExtension(fileName).ToLower();
            bool isSupported = false;
            foreach (string supportedExtension in SupportedFileExtensions)
            {
                if (extension == supportedExtension)
                {
                    isSupported = true;
                    break;
                }
            }

            if (!isSupported)
            {
                return ValidationResult.Failure($"File type '{extension}' is not supported. Supported types: {string.Join(", ", SupportedFileExtensions)}");
            }

            return ValidationResult.Success();
        }

        public FileType DetermineFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            
            switch (extension)
            {
                case ".jpg":
                    return FileType.Jpg;
                case ".jpeg":
                    return FileType.Jpeg;
                case ".png":
                    return FileType.Png;
                case ".gif":
                    return FileType.Gif;
                case ".bmp":
                    return FileType.Bmp;
                case ".txt":
                    return FileType.Txt;
                default:
                    return FileType.Unsupported;
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }
            else if (bytes < 1024 * 1024)
            {
                double kb = bytes / 1024.0;
                return $"{Math.Round(kb)} kB";
            }
            else
            {
                double mb = bytes / (1024.0 * 1024.0);
                return $"{Math.Round(mb)} MB";
            }
        }
    }
}
