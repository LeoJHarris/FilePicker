namespace LeoJHarris.FilePicker
{
    using System;
    using System.Collections.Generic;
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

        public async Task<FileData> PickFile()
        {
            return await this.TakeMediaAsync();
        }

        private Task<FileData> TakeMediaAsync()
        {
            if (Interlocked.CompareExchange<TaskCompletionSource<FileData>>(ref this._completionSource, new TaskCompletionSource<FileData>((object)this.GetRequestId()), (TaskCompletionSource<FileData>)null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");
            UIDocumentMenuViewController menuViewController1 = new UIDocumentMenuViewController(new string[9]
            {
        (string) UTType.UTF8PlainText,
        (string) UTType.PlainText,
        (string) UTType.RTF,
        (string) UTType.PNG,
        (string) UTType.Text,
        (string) UTType.PDF,
        (string) UTType.Image,
        (string) UTType.UTF16PlainText,
        (string) UTType.FileURL
            }, UIDocumentPickerMode.Import);
            menuViewController1.Delegate = (IUIDocumentMenuDelegate)this;
            long num = 7;
            menuViewController1.ModalPresentationStyle = (UIModalPresentationStyle)num;
            UIDocumentMenuViewController menuViewController2 = menuViewController1;
            // ISSUE: reference to a compiler-generated method
            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController((UIViewController)menuViewController2, true, (Action)null);
            UIPopoverPresentationController presentationController = menuViewController2.PopoverPresentationController;
            if (presentationController != null)
            {
                presentationController.SourceView = UIApplication.SharedApplication.KeyWindow.RootViewController.View;
                presentationController.PermittedArrowDirections = UIPopoverArrowDirection.Down;
            }
            this.Handler = (EventHandler<FilePickerEventArgs>)null;
            this.Handler = (EventHandler<FilePickerEventArgs>)((s, e) =>
            {
                TaskCompletionSource<FileData> completionSource = Interlocked.Exchange<TaskCompletionSource<FileData>>(ref this._completionSource, (TaskCompletionSource<FileData>)null);
                if (completionSource == null)
                    return;
                FileData result = new FileData(e.FileByte, e.FileName, e.FilePath);
                completionSource.SetResult(result);
            });
            return this._completionSource.Task;
        }
        public void WasCancelled(UIDocumentMenuViewController documentMenu)
        {
            TaskCompletionSource<FileData> tcs = Interlocked.Exchange(ref this._completionSource, null);

            tcs?.SetResult(null);
        }

        private int GetRequestId()
        {
            int requestId = this._requestId;
            if (this._requestId == int.MaxValue)
            {
                this._requestId = 0;
                return requestId;
            }
            this._requestId = this._requestId + 1;
            return requestId;
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