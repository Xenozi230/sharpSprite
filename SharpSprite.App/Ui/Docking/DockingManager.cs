using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SharpSprite.App.Ui.Docking
{
    /// <summary>
    /// Gestionnaire centralisé pour tous les panneaux dockés.
    /// S'occupe de l'enregistrement, l'organisation et la visibilité des panneaux.
    /// </summary>
    public partial class DockingManager : ObservableObject
    {
        [ObservableProperty]
        private List<IDockPanel> registeredPanels = new();

        [ObservableProperty]
        private IDockPanel? activeCentralPanel;

        public DockingManager()
        {
            RegisteredPanels = new List<IDockPanel>();
        }

        /// <summary>
        /// Enregistre un nouveau panneau dans le système de docking.
        /// </summary>
        public void RegisterPanel(IDockPanel panel)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            if (RegisteredPanels.Any(p => p.PanelId == panel.PanelId))
                throw new InvalidOperationException($"Un panneau avec l'ID '{panel.PanelId}' est déjà enregistré.");

            RegisteredPanels.Add(panel);

            if (panel.DockPosition == DockPosition.Center)
                ActiveCentralPanel = panel;
        }


        public IDockPanel? GetPanel(string panelId)
            => RegisteredPanels.FirstOrDefault(p => p.PanelId == panelId);

        public IEnumerable<IDockPanel> GetPanelsByPosition(DockPosition position)
            => RegisteredPanels.Where(p => p.DockPosition == position && p.IsVisible);


        public void TogglePanelVisibility(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null && panel.IsClosable)
                panel.IsVisible = !panel.IsVisible;
        }

        public void ShowPanel(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null)
                panel.IsVisible = true;
        }

        public void HidePanel(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null && panel.IsClosable)
                panel.IsVisible = false;
        }

        public IEnumerable<IDockPanel> GetVisiblePanels()
            => RegisteredPanels.Where(p => p.IsVisible);

      
        public void ResetPanelsVisibility()
        {
            foreach (var panel in RegisteredPanels)
            {
                panel.IsVisible = !panel.IsClosable;
            }
        }
    }
}