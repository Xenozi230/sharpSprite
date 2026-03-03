using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.App.Tools;

namespace SharpSprite.App.ViewModels
{
    public partial class ToolbarViewModel : ObservableObject
    {
        // ── Active tool ───────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPencilActive))]
        [NotifyPropertyChangedFor(nameof(IsEraserActive))]
        [NotifyPropertyChangedFor(nameof(IsPanActive))]
        [NotifyPropertyChangedFor(nameof(IsZoomActive))]
        [NotifyPropertyChangedFor(nameof(IsEyedropperActive))]
        [NotifyPropertyChangedFor(nameof(IsFillActive))]
        [NotifyPropertyChangedFor(nameof(IsSelectionActive))]
        [NotifyPropertyChangedFor(nameof(IsLineActive))]
        [NotifyPropertyChangedFor(nameof(IsRectangleActive))]
        [NotifyPropertyChangedFor(nameof(IsEllipseActive))]
        private ToolType _activeToolType = ToolType.Pencil;

        public bool IsPencilActive => ActiveToolType == ToolType.Pencil;
        public bool IsEraserActive => ActiveToolType == ToolType.Eraser;
        public bool IsPanActive => ActiveToolType == ToolType.Pan;
        public bool IsZoomActive => ActiveToolType == ToolType.Zoom;
        public bool IsEyedropperActive => ActiveToolType == ToolType.Eyedropper;
        public bool IsFillActive => ActiveToolType == ToolType.Fill;
        public bool IsSelectionActive => ActiveToolType == ToolType.Selection;
        public bool IsLineActive => ActiveToolType == ToolType.Line;
        public bool IsRectangleActive => ActiveToolType == ToolType.Rectangle;
        public bool IsEllipseActive => ActiveToolType == ToolType.Ellipse;

        // ── Brush / tool options ──────────────────────────────────────────
        [ObservableProperty]
        private int _brushSize = 1;

        [ObservableProperty]
        private bool _pixelPerfect = true;

        [ObservableProperty]
        private bool _filled = false;

        // ── Commands ──────────────────────────────────────────────────────
        [RelayCommand] public void SelectPencil() => ActiveToolType = ToolType.Pencil;
        [RelayCommand] public void SelectEraser() => ActiveToolType = ToolType.Eraser;
        [RelayCommand] public void SelectPan() => ActiveToolType = ToolType.Pan;
        [RelayCommand] public void SelectZoom() => ActiveToolType = ToolType.Zoom;
        [RelayCommand] public void SelectEyedropper() => ActiveToolType = ToolType.Eyedropper;
        [RelayCommand] public void SelectFill() => ActiveToolType = ToolType.Fill;
        [RelayCommand] public void SelectSelection() => ActiveToolType = ToolType.Selection;
        [RelayCommand] public void SelectLine() => ActiveToolType = ToolType.Line;
        [RelayCommand] public void SelectRectangle() => ActiveToolType = ToolType.Rectangle;
        [RelayCommand] public void SelectEllipse() => ActiveToolType = ToolType.Ellipse;

        [RelayCommand]
        public void IncreaseBrushSize()
        {
            if (BrushSize < 64) BrushSize++;
        }

        [RelayCommand]
        public void DecreaseBrushSize()
        {
            if (BrushSize > 1) BrushSize--;
        }
    }
}