using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace UGSGit.Views
{
    public class CommandPaletteDataTemplates : IDataTemplate
    {
        public Control Build(object param) => ControlExtensions.CreateFromViewModels(param);
        public bool Match(object data) => data is ViewModels.ICommandPalette;
    }
}
