namespace LeoJHarris.FilePicker
{
    using System;

    using Android.App;
    using Android.Content;
    using Android.Database;
    using Android.Net;
    using Android.OS;
    using Android.Provider;
    using Android.Runtime;

    using LeoJHarris.FilePicker.Abstractions;

    [Activity(ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    [Preserve(AllMembers = true)]
    public class FilePickerActivity : Activity
    {
        private Context context;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.context = Application.Context;


            Intent intent = new Intent(Intent.ActionGetContent);
            intent.SetType("*/*");

            intent.AddCategory(Intent.CategoryOpenable);
            try
            {
                this.StartActivityForResult(Intent.CreateChooser(intent, "Select file"), 0);
            }
            catch (Exception exAct)
            {
                System.Diagnostics.Debug.Write(exAct);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Canceled)
            {
                // Notify user file picking was cancelled.
                OnFilePickCancelled();
                this.Finish();
            }
            else
            {
                System.Diagnostics.Debug.Write(data.Data);
                try
                {
                    Android.Net.Uri _uri = data.Data;

                    string filePath = IOUtil.getPath(this.context, _uri);

                    if (string.IsNullOrEmpty(filePath))
                        filePath = _uri.Path;

                    byte[] file = IOUtil.readFile(filePath);

                    string fileName = this.GetFileName(this.context, _uri);

                    OnFilePicked(new FilePickerEventArgs(file, fileName, filePath));
                }
                catch (Exception readEx)
                {
                    // Notify user file picking failed.
                    OnFilePickCancelled();
                    System.Diagnostics.Debug.Write(readEx);
                }
                finally
                {
                    this.Finish();
                }
            }
        }

        string GetFileName(Context ctx, Android.Net.Uri uri)
        {

            string[] projection = { MediaStore.MediaColumns.DisplayName };

            ContentResolver cr = ctx.ContentResolver;
            string name = string.Empty;
            ICursor metaCursor = cr.Query(uri, projection, null, null, null);

            if (metaCursor != null)
            {
                try
                {
                    if (metaCursor.MoveToFirst())
                    {
                        name = metaCursor.GetString(0);
                    }
                }
                finally
                {
                    metaCursor.Close();
                }
            }

            return name;
        }

        internal static event EventHandler<FilePickerEventArgs> FilePicked;
        internal static event EventHandler<EventArgs> FilePickCancelled;

        private static void OnFilePickCancelled()
        {
            FilePickCancelled?.Invoke(null, null);
        }

        private static void OnFilePicked(FilePickerEventArgs e)
        {
            EventHandler<FilePickerEventArgs> picked = FilePicked;

            if (picked != null)
                picked(null, e);
        }
    }
}