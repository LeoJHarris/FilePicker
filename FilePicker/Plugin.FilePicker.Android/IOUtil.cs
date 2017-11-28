using System;

using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Webkit;

using Java.IO;

namespace LeoJHarris.FilePicker
{
    using Android.Net;

    public class IOUtil
    {

        public static string getPath(Context context, Uri uri)
        {
            bool isKitKat = Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat;

            // DocumentProvider
            if (isKitKat && DocumentsContract.IsDocumentUri(context, uri))
            {
                // ExternalStorageProvider
                if (isExternalStorageDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(':');
                    string type = split[0];

                    if ("primary".Equals(type, StringComparison.OrdinalIgnoreCase))
                    {
                        return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
                    }

                    // TODO handle non-primary volumes
                }

                // DownloadsProvider
                else if (isDownloadsDocument(uri))
                {
                    string id = DocumentsContract.GetDocumentId(uri);
                    Uri contentUri = ContentUris.WithAppendedId(
                        Uri.Parse("content://downloads/public_downloads"),
                        long.Parse(id));

                    return getDataColumn(context, contentUri, null, null);
                }

                // MediaProvider
                else if (isMediaDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    string[] split = docId.Split(':');
                    string type = split[0];

                    Uri contentUri = null;
                    if ("image".Equals(type))
                    {
                        contentUri = MediaStore.Images.Media.ExternalContentUri;
                    }
                    else if ("video".Equals(type))
                    {
                        contentUri = MediaStore.Video.Media.ExternalContentUri;
                    }
                    else if ("audio".Equals(type))
                    {
                        contentUri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    string selection = "_id=?";
                    string[] selectionArgs = new[] { split[1] };

                    return getDataColumn(context, contentUri, selection, selectionArgs);
                }
            }

            // MediaStore (and general)
            else if ("content".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return getDataColumn(context, uri, null, null);
            }

            // File
            else if ("file".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return uri.Path;
            }

            return null;
        }

        public static string getDataColumn(Context context, Uri uri, string selection,
        string[] selectionArgs)
        {
            ICursor cursor = null;
            string column = "_data";
            string[] projection = { column };

            try
            {
                cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int column_index = cursor.GetColumnIndexOrThrow(column);
                    return cursor.GetString(column_index);
                }
            }
            finally
            {
                cursor?.Close();
            }

            return null;
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is ExternalStorageProvider.
         */
        public static bool isExternalStorageDocument(Uri uri)
        {
            return "com.android.externalstorage.documents".Equals(uri.Authority);
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is DownloadsProvider.
         */
        public static bool isDownloadsDocument(Uri uri)
        {
            return "com.android.providers.downloads.documents".Equals(uri.Authority);
        }

        /**
         * @param uri The Uri to check.
         * @return Whether the Uri authority is MediaProvider.
         */
        public static bool isMediaDocument(Uri uri)
        {
            return "com.android.providers.media.documents".Equals(uri.Authority);
        }

        public static byte[] readFile(string file)
        {
            try
            {
                return readFile(new File(file));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return new byte[0];
            }
        }

        public static byte[] readFile(File file)
        {
            // Open file
            RandomAccessFile f = new RandomAccessFile(file, "r");

            try
            {
                // Get and check length
                long longlength = f.Length();
                int length = (int)longlength;

                if (length != longlength) throw new IOException("Filesize exceeds allowed size");

                // Read file and return data
                byte[] data = new byte[length];
                f.ReadFully(data);
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return new byte[0];
            }
            finally
            {
                f.Close();
            }
        }

        public static string GetMimeType(string url)
        {
            string type = null;
            string extension = MimeTypeMap.GetFileExtensionFromUrl(url);

            if (extension != null)
            {
                type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            }

            return type;
        }
    }
}