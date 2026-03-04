using Avalonia.Controls;
using Avalonia.VisualTree;
using SharpSprite.App.Controls;
using SharpSprite.App.ViewModels;

namespace SharpSprite.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            Loaded += (_, _) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    // Find the PixelCanvasControl and hook it up
                    var canvas = this.FindDescendantOfType<PixelCanvasControl>();
                    if (canvas != null)
                    {
                        canvas.CursorMoved = vm.UpdateCursorPosition;
                        canvas.ZoomChanged = zoom => vm.StatusBar.Zoom = zoom;
                    }
                }
            };
        }
    }
}