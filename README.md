# FilesUploader

A .NET 9 console application that automatically monitors a local folder and uploads files to Azure Blob Storage. This background service is designed for scenarios where you need to continuously back up files from a local directory to cloud storage.

## 🚀 Features

- **Continuous Monitoring**: Automatically scans a specified folder at configurable intervals
- **Azure Blob Storage Integration**: Uploads files to Azure Blob Storage containers
- **Background Processing**: Runs as a Windows service or console application
- **Automatic Cleanup**: Deletes local files after successful upload
- **Comprehensive Logging**: Uses Serilog for detailed logging with file and console output
- **Error Handling**: Robust error handling with detailed logging for troubleshooting
- **Configuration-based**: Easy configuration through `appsettings.json`
- **Subdirectory Support**: Recursively scans all subdirectories

## 🏗️ Architecture

The application follows a clean architecture pattern with dependency injection:

- **Program.cs**: Application entry point and dependency injection configuration
- **AppBackgroundService**: Background service that orchestrates the file scanning process
- **FilesService**: Core service responsible for scanning folders and coordinating uploads
- **AzureStorageService**: Handles all Azure Blob Storage operations
- **AppSettings**: Configuration model with validation attributes

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Azure Storage Account with connection string
- Windows operating system (for service deployment)

## ⚙️ Configuration

Configure the application by editing `appsettings.json`:

```json
{
  "AppSettings": {
    "ScanIntervalMinutes": 1,
    "ScanFolder": "./Backups",
    "Container": "backup",
    "AzureConnectionString": "Your Azure Storage Connection String"
  }
}
```

### Configuration Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| `ScanIntervalMinutes` | Interval between folder scans (1-60 minutes) | Yes | 1 |
| `ScanFolder` | Local folder path to monitor | Yes | "./Backups" |
| `Container` | Azure Blob Storage container name | Yes | "backup" |
| `AzureConnectionString` | Azure Storage connection string | Yes | - |

## 🚀 Getting Started

### 1. Clone and Build

```powershell
git clone <repository-url>
cd FilesUploader
dotnet build
```

### 2. Configure Azure Storage

1. Create an Azure Storage Account
2. Get the connection string from Azure Portal
3. Update `AzureConnectionString` in `appsettings.json`

### 3. Set Up Folder Structure

The application will automatically create the scan folder if it doesn't exist. By default, it monitors the `./Backups` folder.

### 4. Run the Application

```powershell
dotnet run
```

## 📁 How It Works

1. **Initialization**: The application starts and validates configuration settings
2. **Folder Monitoring**: Every configured interval (default: 1 minute), the service scans the specified folder
3. **File Discovery**: All files in the folder and subdirectories are identified
4. **Upload Process**: Each file is uploaded to the specified Azure Blob Storage container
5. **Cleanup**: After successful upload, the local file is deleted
6. **Logging**: All operations are logged with detailed information

### File Processing Flow

```
Local Folder → Scan → Upload to Azure → Delete Local File → Log Results
```

## 📊 Logging

The application uses Serilog for comprehensive logging:

- **Console Output**: Real-time logging with colored output
- **File Logging**: Daily rotating log files in `Logs/` directory
- **Debug Output**: Additional debug information during development

Log files are stored in the `Logs/` directory with the naming pattern `uploader-log{yyyyMMdd}.txt`.

## 🏃‍♂️ Running as a Windows Service

To run as a Windows service, you can use tools like NSSM (Non-Sucking Service Manager):

```powershell
# Install NSSM
choco install nssm

# Create service
nssm install FilesUploader "C:\path\to\FilesUploader.exe"
nssm set FilesUploader AppDirectory "C:\path\to\FilesUploader"
nssm start FilesUploader
```

## 🔧 Development

### Project Structure

```
FilesUploader/
├── Extensions/
│   └── LoggerConfigurationUtils.cs    # Serilog configuration
├── Services/
│   ├── AppBackgroundService.cs        # Main background service
│   ├── AzureStorageService.cs         # Azure Blob operations
│   ├── BaseBackgroundService.cs       # Base service class
│   └── FilesService.cs                # File scanning logic
├── Settings/
│   └── AppSettings.cs                 # Configuration model
├── appsettings.json                   # Application configuration
├── appsettings.Development.json       # Development settings
├── Program.cs                         # Application entry point
└── FilesUploader.csproj              # Project file
```

### Key Dependencies

- **Azure.Storage.Blobs** (12.24.1): Azure Blob Storage client
- **Serilog.AspNetCore** (8.0.2): Structured logging framework
- **Microsoft.Extensions.Hosting**: Background service hosting
- **System.ComponentModel.DataAnnotations**: Configuration validation

## 🛡️ Error Handling

The application implements comprehensive error handling:

- **Upload Failures**: Files that fail to upload are logged, but the process continues
- **File Access Errors**: Handles locked files and permission issues
- **Network Issues**: Retries and logging for Azure connectivity problems
- **Configuration Errors**: Validates settings on startup with clear error messages

## 🔍 Monitoring

Monitor the application through:

- **Log Files**: Check `Logs/uploader-log{date}.txt` for detailed operation history
- **Console Output**: Real-time status during development
- **Azure Portal**: Verify file uploads in the Azure Storage container

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Troubleshooting

### Common Issues

1. **Files not uploading**: Check Azure connection string and container permissions
2. **Permission errors**: Ensure the application has read/write access to the scan folder
3. **Files stuck in folder**: Check logs for upload errors or Azure connectivity issues
4. **High memory usage**: Large files are loaded into memory; consider chunked uploads for very large files

### Debug Steps

1. Check application logs in the `Logs/` directory
2. Verify Azure Storage connection and container existence
3. Ensure local folder permissions are correct
4. Test with small files first

## 📞 Support

For support and questions:
- Check the logs first for error details
- Review configuration settings
- Ensure Azure Storage account is accessible
- Verify folder permissions and structure
