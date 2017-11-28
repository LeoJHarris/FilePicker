namespace LeoJHarris.FilePicker.Abstractions
{
    using System;
    using System.IO;

    public class FileData : IDisposable
    {
        private string _fileName;
        private string _filePath;
        private bool _isDisposed;
        private readonly Action<bool> _dispose;
        private readonly Func<Stream> _streamGetter;

        public FileData()
        {
        }

        private byte[] _dataArray;

        public FileData(byte[] dataArray, string fileName, string filePath)
        {
            this._dataArray = dataArray;
            this._fileName = fileName;
            this._filePath = filePath;
        }

        public byte[] DataArray
        {
            get { return this._dataArray; }
        }

        /// <summary>
        /// Filename of the picked file
        /// </summary>
        public string FileName
        {
            get
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(null);

                return this._fileName;
            }

            set
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(null);

                this._fileName = value;
            }
        }

        /// <summary>
        /// Full filepath of the picked file
        /// </summary>
        public string FilePath
        {
            get
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(null);

                return this._filePath;
            }

            set
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(null);

                this._filePath = value;
            }
        }

        /// <summary>
        /// Get stream if available
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (this._isDisposed)
                throw new ObjectDisposedException(null);

            return this._streamGetter();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this._isDisposed)
                return;

            this._isDisposed = true;
            this._dispose?.Invoke(disposing);
        }

        ~FileData()
        {
            this.Dispose(false);
        }
    }
}