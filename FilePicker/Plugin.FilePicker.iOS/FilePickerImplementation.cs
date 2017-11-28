namespace LeoJHarris.FilePicker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CoreGraphics;

    using Foundation;

    using LeoJHarris.FilePicker.Abstractions;

    using MobileCoreServices;

    using UIKit;

    /// <summary>
    /// Implementation for FilePicker
    /// </summary>
    public class FilePickerImplementation : NSObject, IUIDocumentMenuDelegate, IFilePicker
    {
        private int _requestId;
        private TaskCompletionSource<FileData> _completionSource;

        /// <summary>
        /// Event which is invoked when a file was picked
        /// </summary>
        public EventHandler<FilePickerEventArgs> Handler
        {
            get;
            set;
        }

        private void OnFilePicked(FilePickerEventArgs e)
        {
            this.Handler?.Invoke(null, e);
        }

        public void DidPickDocumentPicker(UIDocumentMenuViewController documentMenu, UIDocumentPickerViewController documentPicker)
        {
            documentPicker.DidPickDocument += this.DocumentPicker_DidPickDocument;
            documentPicker.WasCancelled += this.DocumentPicker_WasCancelled;

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(documentPicker, true, null);
        }

        private void DocumentPicker_DidPickDocument(object sender, UIDocumentPickedEventArgs e)
        {
            bool securityEnabled = e.Url.StartAccessingSecurityScopedResource();
            UIDocument doc = new UIDocument(e.Url);
            NSData data = NSData.FromUrl(e.Url);
            byte[] dataBytes = new byte[data.Length];

            System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32(data.Length));

            string filename = doc.LocalizedName;
            string pathname = doc.FileUrl?.ToString();

            // iCloud drive can return null for LocalizedName.
            if (filename == null)
            {
                // Retrieve actual filename by taking the last entry after / in FileURL.
                // e.g. /path/to/file.ext -> file.ext

                // filesplit is either:
                // 0 (pathname is null, or last / is at position 0)
                // -1 (no / in pathname)
                // positive int (last occurence of / in string)
                int filesplit = pathname?.LastIndexOf('/') ?? 0;

                filename = pathname?.Substring(filesplit + 1);
            }

            this.OnFilePicked(new FilePickerEventArgs(dataBytes, filename, pathname));
        }

        /// <summary>
        /// Handles when the file picker was cancelled. Either in the
        /// popup menu or later on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DocumentPicker_WasCancelled(object sender, EventArgs e)
        {
            {
                TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);
                tcs.SetResult(null);
            }
        }

        /// <summary>
        /// Lets the user pick a file with the systems default file picker
        /// For iOS iCloud drive needs to be configured
        /// </summary>
        /// <returns></returns>
        public async Task<FileData> PickFile()
        {
            FileData media = await this.TakeMediaAsync().ConfigureAwait(true);

            return media;
        }

        private Task<FileData> TakeMediaAsync()
        {
            int id = this.GetRequestId();

            TaskCompletionSource<FileData> taskCompletionSource = new TaskCompletionSource<FileData>(id);

            if (Interlocked.CompareExchange(ref this._completionSource, taskCompletionSource, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            string[] allowedUtis = new string[]
                                       {
                                           UTType.UTF8PlainText, UTType.PlainText, UTType.RTF, UTType.PNG, UTType.Text,
                                           UTType.PDF, UTType.Image, UTType.UTF16PlainText, UTType.FileURL
                                       };

            UIDocumentMenuViewController importMenu =
                new UIDocumentMenuViewController(allowedUtis, UIDocumentPickerMode.Import)
                {
                    Delegate = this,
                    ModalPresentationStyle =
                            UIModalPresentationStyle
                                .Popover
                };

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(importMenu, true, null);

            UIPopoverPresentationController presPopover = importMenu.PopoverPresentationController;

            if (presPopover != null)
            {
                presPopover.SourceView = UIApplication.SharedApplication.KeyWindow.RootViewController.View;
                presPopover.PermittedArrowDirections = UIPopoverArrowDirection.Down;
            }

            this.Handler = null;

            this.Handler = (s, e) =>
                {
                    TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);

                    tcs?.SetResult(new FileData(e.FileByte, e.FileName, e.FilePath));
                };

            return this._completionSource.Task;
        }

        public void WasCancelled(UIDocumentMenuViewController documentMenu)
        {
            TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);

            tcs?.SetResult(null);
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
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                string directoryWithFilePath;

                if (!string.IsNullOrEmpty(optionalFolderName))
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(Path.Combine(documents, optionalFolderName));

                    directoryWithFilePath = Path.Combine(directoryInfo.FullName, fileToSave.FileName);
                }
                else
                {
                    directoryWithFilePath = Path.Combine(documents, fileToSave.FileName);
                }

                File.WriteAllBytes(directoryWithFilePath, fileToSave.DataArray);
                return directoryWithFilePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public void OpenFile(NSUrl fileUrl)
        {
            UIDocumentInteractionController docControl = UIDocumentInteractionController.FromUrl(fileUrl);

            UIWindow window = UIApplication.SharedApplication.KeyWindow;
            UIView[] subViews = window.Subviews;
            UIView lastView = subViews.Last();
            CGRect frame = lastView.Frame;

            docControl.PresentOpenInMenu(frame, lastView, true);
        }

        public void OpenFile(string fullPathToFile)
        {
            if (NSFileManager.DefaultManager.FileExists(fullPathToFile))
            {
                NSUrl url = new NSUrl(fullPathToFile, true);
                this.OpenFile(url);
            }
        }

        public async void OpenFile(FileData fileToOpen)
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            string fileName = Path.Combine(documents, fileToOpen.FileName);

            if (!NSFileManager.DefaultManager.FileExists(fileName))
            {
                await this.SaveFileAsync(fileToOpen).ConfigureAwait(true);
            }
            else
            {
                NSUrl url = new NSUrl(fileName, true);

                this.OpenFile(url);
            }
        }
    }
}