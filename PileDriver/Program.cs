using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MimeTypes;
using Google.Apis.Upload;
using System.Diagnostics;

namespace DriveQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.DriveFile };
        static string[] RootBackupFolder = { "0BwG7X-jkzBFMZlV3ajR6WHZibUU" };
        static string backupOrigin = @"C:\PileDriver\";
        static string ApplicationName = "Drive API .NET Quickstart";

        static void Main(string[] args)
        {
            UserCredential credential;

            //if (!EventLog.SourceExists("PileDriver")) EventLog.CreateEventSource("PileDriver", "Application");

            Console.Read();

            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                
            }

            Console.WriteLine("Authorized with Google");

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Console.WriteLine("Drive service created");

            // Get files
            string[] filesToBackup = Directory.GetFiles(backupOrigin);

            Console.WriteLine("Identified {0} files to back up", filesToBackup.Length);

            Dictionary<string, string> DailyFolders = new Dictionary<string, string>();

            foreach (string fullPath in filesToBackup)
            {
                
                FileInfo fileInfo = new FileInfo(fullPath);
                var upload = new Google.Apis.Drive.v3.Data.File();
                
                upload.CreatedTime = fileInfo.CreationTime;
                upload.Description = fullPath;
                upload.Name = fileInfo.Name;

                Console.Write("Uploading {0} ... ", fileInfo.Name);

                // Decide on the daily subfolder
                // For today's date (at runtime): DateTime.Now.ToString("yyyy-MM-dd");
                string dailyFolderName = fileInfo.LastWriteTime.ToString("yyyy-MM-dd");
                string dailyFolderDriveId = null;

                if (DailyFolders.ContainsKey(dailyFolderName))
                {
                    // Reuse existing ID
                    dailyFolderDriveId = DailyFolders[dailyFolderName];
                }
                else
                {
                    // Create a new folder for this day
                    var dailyFolderMeta = new Google.Apis.Drive.v3.Data.File();
                    dailyFolderMeta.MimeType = "application/vnd.google-apps.folder";
                    dailyFolderMeta.Name = dailyFolderName;
                    dailyFolderMeta.Parents = RootBackupFolder;
                    var dailyFolderRequest = service.Files.Create(dailyFolderMeta);
                    dailyFolderRequest.Fields = "id";

                    // Create folder and set this upload to go to it
                    var dailyFolder = dailyFolderRequest.Execute();
                    dailyFolderDriveId = dailyFolder.Id;

                    // Cache for reuse
                    DailyFolders.Add(dailyFolderName, dailyFolderDriveId);

                    EventLog.WriteEntry("PileDriver", string.Format("Created new Daily Folder {0}", dailyFolderName));

                }

                upload.Parents = new string[] { dailyFolderDriveId };

                // Read file data into memory stream
                byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                MemoryStream fileStream = new MemoryStream(fileData);

                FilesResource.CreateMediaUpload uploadRequest = service.Files.Create(upload, fileStream, MimeTypeMap.GetMimeType(fileInfo.Extension));
                uploadRequest.Upload();

                var progress = uploadRequest.GetProgress();

                if (fileData.Length == progress.BytesSent)
                {
                    Console.WriteLine(uploadRequest.ResponseBody.Id);

                    EventLog.WriteEntry("PileDriver", string.Format("Uploaded {0} to {1}", fileInfo.Name, dailyFolderName));

                    // Upload successful, all bytes sent - delete the file
                    System.IO.File.Delete(fullPath);
                }

            }

            Console.WriteLine("Done!");
            Console.Read();

        }
    }
}