using System;
using System.Diagnostics;

using Avalonia.Interactivity;

namespace UGSGit.Views
{
    public partial class ConfirmRestart : ChromelessWindow
    {
        public ConfirmRestart()
        {
            InitializeComponent();
        }

        private void Restart(object _1, RoutedEventArgs _2)
        {
            var selfExecFile = Environment.ProcessPath;
            Process.Start(selfExecFile);
            App.Quit(-1);
        }
    }
}
