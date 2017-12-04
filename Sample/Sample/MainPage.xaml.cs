namespace Sample
{
    using System;
    using System.IO;

    using LeoJHarris.FilePicker;
    using LeoJHarris.FilePicker.Abstractions;

    using Xamarin.Forms;

    public partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private FileData file = default(FileData);

        private async void Button_OnPickerFileClickedAsync(object sender, EventArgs e)
        {
            this.file = await CrossFilePicker.Current.PickFile().ConfigureAwait(true);
            if (this.file == null)
            {
                return;
            }

            string extensionType = this.file.FileName.Substring(
                this.file.FileName.LastIndexOf(".", StringComparison.Ordinal) + 1,
                this.file.FileName.Length - this.file.FileName.LastIndexOf(".", StringComparison.Ordinal) - 1).ToLower();

            if (extensionType.Equals("png") || extensionType.Equals("jpg") || extensionType.Equals("jpeg"))
            {
                this.ImageForFile.Source = ImageSource.FromStream(() => new MemoryStream(this.file.DataArray));
            }
            else
            {
                await this.DisplayAlert("Name of the file:" + file.FileName + " and path too file", "File info", "OK");
            }
        }

        private async void Button_OnFileSavedClickedAsync(object sender, EventArgs e)
        {
            string fullPathToFile = await CrossFilePicker.Current.SaveFileAsync(this.file).ConfigureAwait(false);
            CrossFilePicker.Current.OpenFile(fullPathToFile);

            if (string.IsNullOrEmpty(fullPathToFile))
            {
                await this.DisplayAlert("File was saved", "File was saved", "OK").ConfigureAwait(false);
            }
        }

        private void Button_OnOpenFileClickedAsync(object sender, EventArgs e)
        {
            if (this.file != default(FileData))
            {
                CrossFilePicker.Current.OpenFile(this.file);
            }
        }
    }
}