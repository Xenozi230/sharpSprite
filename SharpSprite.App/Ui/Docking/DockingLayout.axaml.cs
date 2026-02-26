using Avalonia.Controls;
using System.Diagnostics;
using System.Linq;

namespace SharpSprite.App.Ui.Docking
{
    public partial class DockingLayout : UserControl
    {
        private DockingManager? _dockingManager;

        public DockingLayout()
        {
            InitializeComponent();
            this.DataContextChanged += (s, e) => OnDataContextChanged();
        }

        private void OnDataContextChanged()
        {
            if (DataContext is DockingManager manager)
            {
                _dockingManager = manager;
                LoadPanels();
            }
        }


        public void LoadPanels()
        {
            if (_dockingManager == null)
            {
                Debug.WriteLine("❌ DockingManager is null");
                return;
            }

            var leftPanel = this.FindControl<StackPanel>("LeftPanel");
            var rightPanel = this.FindControl<StackPanel>("RightPanel");

            if (leftPanel == null || rightPanel == null)
            {
                Debug.WriteLine("❌ LeftPanel or RightPanel not found");
                return;
            }


            leftPanel.Children.Clear();
            rightPanel.Children.Clear();

            Debug.WriteLine($"📊 Total panels: {_dockingManager.RegisteredPanels.Count}");

            // Ajouter les panneaux à gauche
            foreach (var panel in _dockingManager.RegisteredPanels.Where(p => p.DockPosition == DockPosition.Left))
            {
                Debug.WriteLine($"📍 Adding LEFT: {panel.DisplayName}");
                leftPanel.Children.Add(panel.View);
            }

            // Ajouter les panneaux à droite
            foreach (var panel in _dockingManager.RegisteredPanels.Where(p => p.DockPosition == DockPosition.Right))
            {
                Debug.WriteLine($"📍 Adding RIGHT: {panel.DisplayName}");
                rightPanel.Children.Add(panel.View);
            }
        }

        public void SetCentralContent(Control content)
        {
            var centralContent = this.FindControl<ContentControl>("CentralContent");
            if (centralContent != null)
            {
                centralContent.Content = content;
                Debug.WriteLine("✅ Central content set");
            }
            else
            {
                Debug.WriteLine("❌ CentralContent not found");
            }
        }
    }
}