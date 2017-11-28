namespace LeoJHarris.FilePicker.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for FilePicker
    /// </summary>
    public interface IFilePicker
    {
        Task<FileData> PickFile();

        /// <summary>
        /// Saves the file 
        /// </summary>
        /// <param name="fileToSave">File to save</param>
        /// <param name="optionalFolderName">Optional folder to create when saving file in</param>
        /// <returns></returns>
        Task<string> SaveFileAsync(FileData fileToSave, string optionalFolderName = null);

        void OpenFile(string fullPathToFile);

        void OpenFile(FileData fileToOpen);
    }
}