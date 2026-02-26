using Avalonia.Controls;

namespace SharpSprite.App.Ui.Docking
{

    public interface IDockPanel
    {
   
        string PanelId { get; }


        string DisplayName { get; }

  
        UserControl View { get; }

        DockPosition DockPosition { get; }

        double? PreferredWidth { get; }

        double? PreferredHeight { get; }


        bool IsClosable { get; }

 
        bool IsVisible { get; set; }
    }


    public enum DockPosition
    {
        Left,
        Right,
        Top,
        Bottom,
        Center // Panneau central (le canvas)
    }
}