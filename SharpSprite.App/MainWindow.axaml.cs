using Avalonia.Controls;
using SharpSprite.App.ViewModels;
using SharpSprite.App.Ui.Docking;
using SharpSprite.App.Ui.Docking.Panels;
using SharpSprite.App.Ui.Widgets;
using System.Diagnostics;

namespace SharpSprite.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var mainVM = new MainWindowViewModel();
            DataContext = mainVM;

            Debug.WriteLine("🚀 MainWindow initialized");


            this.Loaded += (sender, args) => InitializeDocking(mainVM);
        }

        private void InitializeDocking(MainWindowViewModel mainVM)
        {
            Debug.WriteLine("🔧 InitializeDocking started");

            var dockingManager = new DockingManager();
            Debug.WriteLine("✅ DockingManager created");


            var toolbarPanel = new ToolbarPanel(mainVM);
            dockingManager.RegisterPanel(toolbarPanel);
            Debug.WriteLine($"✅ ToolbarPanel registered (synced with MainVM)");

            mainVM.DockingManager = dockingManager;

            var dockingLayout = this.FindControl<Ui.Docking.DockingLayout>("DockingLayout");
            if (dockingLayout != null)
            {
                Debug.WriteLine("✅ DockingLayout found");

                dockingLayout.DataContext = dockingManager;
                Debug.WriteLine("✅ DockingLayout DataContext set");

                var canvasControl = new Controls.PixelCanvasControl
                {
                    Document = mainVM.ActiveDocument,
                    ActiveFrame = mainVM.ActiveFrame,
                    UndoStack = mainVM.UndoStack,
                    ActiveToolType = mainVM.ActiveToolType,
                    ForegroundColor = mainVM.ForegroundColor,
                    BackgroundColor = mainVM.BackgroundColor,
                    Zoom = 0
                };

                dockingLayout.SetCentralContent(canvasControl);
                Debug.WriteLine("✅ Canvas added");


                dockingLayout.LoadPanels();
                Debug.WriteLine("✅ Panels loaded");


                mainVM.Toolbar.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ToolbarViewModel.ActiveToolType))
                    {
                        mainVM.ActiveToolType = mainVM.Toolbar.ActiveToolType;
                        Debug.WriteLine($"🔄 Tool changed: {mainVM.ActiveToolType}");
                    }
                };


                mainVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.ActiveToolType))
                    {
                        mainVM.Toolbar.ActiveToolType = mainVM.ActiveToolType;
                        Debug.WriteLine($"🔄 MainVM tool changed: {mainVM.ActiveToolType}");
                    }
                };
            }
            else
            {
                Debug.WriteLine("❌ DockingLayout not found!");
            }

            Debug.WriteLine("✅ InitializeDocking completed");
        }
    }
}