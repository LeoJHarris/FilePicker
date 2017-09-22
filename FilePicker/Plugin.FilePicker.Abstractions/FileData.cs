using System;
using System.IO;

namespace LeoJHarris.FilePicker.Abstractions
{
    /// <summary>
    /// The object used as a wrapper for the file picked by the user
    /// </summary>
    public class FileData : IDisposable
    {
        private string _fileName;
        private string _filePath;
        private bool _isDisposed;
        private readonly Action<bool> _dispose;
        private readonly Func<Stream> _streamGetter;

        public FileData()
        { }

        private byte[] _dataArray = null;

        public FileData(byte[] dataArray, string fileName, string filePath)
        {
            _dataArray = dataArray;
            _fileName = fileName;
            _filePath = filePath;
        }

        public byte[] DataArray
        {
            get { return _dataArray; }
        }

        /// <summary>
        /// Filename of the picked file
        /// </summary>
        public string FileName
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(null);

                return _fileName;
            }

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(null);

                _fileName = value;
            }
        }

        /// <summary>
        /// Full filepath of the picked file
        /// </summary>
        public string FilePath
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(null);

                return _filePath;
            }

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(null);

                _filePath = value;
            }
        }

        /// <summary>
        /// Get stream if available
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(null);

            return _streamGetter();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _dispose?.Invoke(disposing);
        }

        ~FileData()
        {
            Dispose(false);
        }
    }
}