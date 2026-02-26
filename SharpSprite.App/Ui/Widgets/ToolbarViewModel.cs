using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.App.Tools;

namespace SharpSprite.App.Ui.Widgets
{

    public partial class ToolbarViewModel : ObservableObject
    {
        [ObservableProperty]
        private ToolType _activeToolType = ToolType.Pencil;

        public bool IsPencilActive => ActiveToolType == ToolType.Pencil;

        public bool IsEraserActive => ActiveToolType == ToolType.Eraser;


        public bool IsPanActive => ActiveToolType == ToolType.Pan;

        public bool IsZoomActive => ActiveToolType == ToolType.Zoom;


        [RelayCommand]
        private void PickPencil() => ActiveToolType = ToolType.Pencil;

        [RelayCommand]
        private void PickEraser() => ActiveToolType = ToolType.Eraser;


        [RelayCommand]
        private void PickPan() => ActiveToolType = ToolType.Pan;


        [RelayCommand]
        private void PickZoom() => ActiveToolType = ToolType.Zoom;
    }
}