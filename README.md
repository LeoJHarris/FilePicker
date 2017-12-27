IMPORTANT

Android: The WRITE_EXTERNAL_STORAGE & READ_EXTERNAL_STORAGE permissions are required.

iOS: Need Configure iCloud Driver for your app: https://developer.xamarin.com/guides/ios/deployment,_testing,_and_metrics/provisioning/working-with-capabilities/icloud-capabilities/

USAGES:

PickFileAsync:

var file = await CrossFilePicker.Current.PickFileAsync();
            if (file == null)
            {
                return;
            }
            
SaveFileAsync:

string fullPathToFile = await CrossFilePicker.Current.SaveFileAsync(file);

OpenFile:

CrossFilePicker.Current.OpenFile(fullPathToFile);
CrossFilePicker.Current.OpenFile(file);

Bindable properties

The FileData object returned from picking a file contains properties such as the DataArray, FileName etc.

Saving a File will return the full path to the file saved, which will be saved in the DirectoryDocuments on android and MyDocuments
on iOS

Nuget package found at: https://www.nuget.org/packages/LeoJHarris.Plugin.FilePicker/
