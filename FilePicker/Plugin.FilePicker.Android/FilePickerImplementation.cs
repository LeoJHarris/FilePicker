namespace LeoJHarris.FilePicker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Android.App;
    using Android.Content;
    using Android.Runtime;

    using Java.IO;

    using LeoJHarris.FilePicker.Abstractions;

    using File = Java.IO.File;
    using Uri = Android.Net.Uri;

    /// <summary>
    /// Implementation for Feature
    /// </summary>
    [Preserve(AllMembers = true)]
    public class FilePickerImplementation : IFilePicker
    {
        private readonly Context _context;
        private int _requestId;
        private TaskCompletionSource<FileData> _completionSource;

        public FilePickerImplementation()
        {
            this._context = Application.Context;
        }

        public async FileData PickFile()
        {
            FileData media = await this.TakeMediaAsync("file/*", Intent.ActionGetContent).ConfigureAwait(true);

            return media;
        }

        private Task<FileData> TakeMediaAsync(string type, string action)
        {
            int id = this.GetRequestId();

            TaskCompletionSource<FileData> taskCompletionSource = new TaskCompletionSource<FileData>(id);

            if (Interlocked.CompareExchange(ref this._completionSource, taskCompletionSource, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            try
            {
                Intent pickerIntent = new Intent(this._context, typeof(FilePickerActivity));
                pickerIntent.SetFlags(ActivityFlags.NewTask);

                this._context.StartActivity(pickerIntent);

                EventHandler<FilePickerEventArgs> handler = null;
                EventHandler<EventArgs> canceledHandler = null;

                handler = (s, e) =>
                    {
                        TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);

                        FilePickerActivity.FilePicked -= handler;

                        tcs?.SetResult(new FileData(e.FileByte, e.FileName, e.FilePath));
                    };

                canceledHandler = (s, e) =>
                    {
                        TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);

                        FilePickerActivity.FilePickCancelled -= canceledHandler;

                        tcs?.SetResult(null);
                    };

                FilePickerActivity.FilePickCancelled += canceledHandler;
                FilePickerActivity.FilePicked += handler;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            return this._completionSource.Task;
        }

        private int GetRequestId()
        {
            int id = this._requestId;

            if (this._requestId == int.MaxValue) this._requestId = 0;
            else this._requestId++;

            return id;
        }

        public async Task<string> SaveFileAsync(FileData fileToSave, string optionalFolderName = null)
        {
            try
            {
                File myFile;
                FileOutputStream fos;

                File document = Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments);

                if (!string.IsNullOrEmpty(optionalFolderName))
                {
                    DirectoryInfo directoryInfo =
                        Directory.CreateDirectory(Path.Combine(document.AbsolutePath, optionalFolderName));

                    myFile = new File(directoryInfo.FullName, fileToSave.FileName);
                }
                else
                {
                    myFile = new File(document, fileToSave.FileName);
                }

                if (System.IO.File.Exists(myFile.Path))
                {
                    return myFile.Path;
                }

                fos = new FileOutputStream(myFile.Path);
                await fos.WriteAsync(fileToSave.DataArray).ConfigureAwait(false);
                fos.Close();
                return myFile.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public void OpenFile(File fileToOpen)
        {
            Uri uri = Uri.FromFile(fileToOpen);
            Intent intent = new Intent();
            string mime = IOUtil.GetMimeType(uri.ToString());

            intent.SetAction(Intent.ActionView);
            intent.SetDataAndType(uri, mime);
            intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);

            this._context.StartActivity(intent);
        }

        public void OpenFile(string fullPathToFile)
        {
            File myFile = new File(fullPathToFile);
            if (myFile.Exists())
            {
                this.OpenFile(myFile);
            }
        }

        public async void OpenFile(FileData fileToOpen)
        {
            File myFile = new File(
                Application.Context.GetExternalFilesDir(
                    Android.OS.Environment.DirectoryDocuments),
                fileToOpen.FileName);

            if (!myFile.Exists())
            {
                string pathToFile = await this.SaveFileAsync(fileToOpen)
                    .ConfigureAwait(true);
                if (string.IsNullOrEmpty(pathToFile))
                {
                    return;
                }
            }

            this.OpenFile(myFile.Path);
        }
    }
}