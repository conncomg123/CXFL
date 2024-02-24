using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CsXFL;
public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand OpenDocumentCMD { get; private set; }
    public ICommand SaveDocumentCMD { get; private set; }
    public MainViewModel()
    {
        OpenDocumentCMD = new Command(OpenDocument);
        SaveDocumentCMD = new Command(SaveDocument);
    }

    public event EventHandler<DocumentEventArgs> DocumentOpened;
    private async void OpenDocument()
    {
        try
        {
            FilePickerFileType fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { ".fla", ".xfl" } }
        });

            FileResult result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = fileTypes,
            });

            if (result != null)
            {
                string filePath = result.FullPath;

                Document doc = await An.OpenDocumentAsync(filePath);
                Trace.WriteLine(doc.Filename);

                DocumentOpened?.Invoke(this, new DocumentEventArgs(doc)); // Raise the event
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Exception choosing file: {ex.Message}");
        }
    }
    private void SaveDocument()
    {
        An.GetActiveDocument().Save();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DocumentEventArgs : EventArgs
    {
        public Document Document { get; private set; }

        public DocumentEventArgs(Document document)
        {
            Document = document;
        }
    }
}