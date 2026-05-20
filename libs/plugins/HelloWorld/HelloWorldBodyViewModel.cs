using CommunityToolkit.Mvvm.ComponentModel;

namespace UGSGit.ViewModels.Tabs
{
    public class HelloWorldBodyViewModel : ObservableObject
    {
        private string _status = "Ready";

        public string Message => "Hello from the plugin tab system!";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public void UpdateStatus(string status)
        {
            Status = status;
        }
    }
}
