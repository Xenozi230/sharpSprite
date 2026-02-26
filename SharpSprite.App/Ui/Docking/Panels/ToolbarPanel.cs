using Avalonia.Controls;
using SharpSprite.App.ViewModels;
using SharpSprite.App.Ui.Widgets;

namespace SharpSprite.App.Ui.Docking.Panels
{

    public partial class ToolbarPanel : DockPanelBase
    {
        private readonly ToolbarView _view;
        private MainWindowViewModel? _mainVM;

        public override string PanelId => "Toolbar";
        public override string DisplayName => "Tools";
        public override UserControl View => _view;
        public override DockPosition DockPosition => DockPosition.Left;
        public override double? PreferredWidth => 50;
        public override double? PreferredHeight => null;
        public override bool IsClosable => false; 

        public ToolbarPanel(MainWindowViewModel? mainVM = null)
        {
            _mainVM = mainVM;
            _view = new ToolbarView();

            if (_mainVM != null)
            {
                _view.DataContext = _mainVM.Toolbar;
            }
        }
        public void SetMainViewModel(MainWindowViewModel mainVM)
        {
            _mainVM = mainVM;
            _view.DataContext = mainVM.Toolbar;
        }
    }
}