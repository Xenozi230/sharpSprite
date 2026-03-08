using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.App.Tools;
using SharpSprite.Core.Commands;
using SharpSprite.Core.Document;
using SharpSprite.Infrastructure;

namespace SharpSprite.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // ══════════════════════════════════════════════════════════════════
        // Panel view models
        // ══════════════════════════════════════════════════════════════════

        public ToolbarViewModel Toolbar { get; } = new();
        public PaletteViewModel Palette { get; } = new();
        public TimelineViewModel TimelineVM { get; } = new();
        public StatusBarViewModel StatusBar { get; } = new();
        public ContextBarViewModel ContextBar { get; } = new();

        // ══════════════════════════════════════════════════════════════════
        // Active document
        // ══════════════════════════════════════════════════════════════════

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TitleText))]
        private Document? _activeDocument;

        [ObservableProperty]
        private UndoStack _undoStack = new UndoStack(capacity: 100);

        // ══════════════════════════════════════════════════════════════════
        // Tool selection
        // ══════════════════════════════════════════════════════════════════

        public ToolType ActiveToolType
        {
            get => Toolbar.ActiveToolType;
            set
            {
                Toolbar.ActiveToolType = value;
                ContextBar.ActiveTool = value;
                OnPropertyChanged();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Colors
        // ══════════════════════════════════════════════════════════════════

        public Rgba32 ForegroundColor
        {
            get => Palette.ForegroundColor;
            set { Palette.ForegroundColor = value; OnPropertyChanged(); }
        }

        public Rgba32 BackgroundColor
        {
            get => Palette.BackgroundColor;
            set { Palette.BackgroundColor = value; OnPropertyChanged(); }
        }


        // ══════════════════════════════════════════════════════════════════
        // Frame navigation
        // ══════════════════════════════════════════════════════════════════

        public int ActiveFrame
        {
            get => TimelineVM.CurrentFrame;
            set
            {
                TimelineVM.CurrentFrame = value;
                StatusBar.CurrentFrame = value + 1;
                OnPropertyChanged();
            }
        }


        // ══════════════════════════════════════════════════════════════════
        // Zoom (forwarded to StatusBar)
        // ══════════════════════════════════════════════════════════════════

        private int _zoomLevel = 0; // 0 = auto-fit

        public int ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = value;
                StatusBar.Zoom = value <= 0 ? 1 : value;
                ContextBar.CurrentZoom = value <= 0 ? 1 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasZoom));
            }
        }

        /// <summary>Passed directly to PixelCanvasControl.Zoom.</summary>
        public int CanvasZoom => _zoomLevel;

        // ══════════════════════════════════════════════════════════════════
        // Status / title
        // ══════════════════════════════════════════════════════════════════

        private string _statusText = "Ready";

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; StatusBar.StatusMessage = value; OnPropertyChanged(); }
        }

        public string TitleText
        {
            get
            {
                var name = ActiveDocument?.DisplayName ?? "Untitled";
                var dirty = (ActiveDocument?.IsModified ?? false) ? "●  " : "";
                return $"{dirty}{name} – SharpSprite";
            }
        }

        public string UndoLabel => UndoStack.CanUndo ? $"Undo {UndoStack.NextUndoName}" : "Undo";
        public string RedoLabel => UndoStack.CanRedo ? $"Redo {UndoStack.NextRedoName}" : "Redo";

        // ══════════════════════════════════════════════════════════════════
        // Construction
        // ══════════════════════════════════════════════════════════════════

        public MainWindowViewModel()
        {
            // Forward toolbar tool changes → ContextBar + canvas
            Toolbar.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ToolbarViewModel.ActiveToolType))
                {
                    //ContextBar.ActiveTool = Toolbar.ActiveToolType;
                    OnPropertyChanged(nameof(ActiveToolType));
                }
            };

            // Forward palette color changes → canvas
            Palette.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(PaletteViewModel.ForegroundColor)
                                   or nameof(PaletteViewModel.BackgroundColor))
                {
                    OnPropertyChanged(nameof(ForegroundColor));
                    OnPropertyChanged(nameof(BackgroundColor));
                }
            };

            SetDocument(CreateDefaultDocument());
            Palette.LoadDefaultPalette();
        }

        // ══════════════════════════════════════════════════════════════════
        // Cursor position (called by canvas control)
        // ══════════════════════════════════════════════════════════════════

        public void UpdateCursorPosition(int x, int y)
        {
            StatusBar.CursorX = x;
            StatusBar.CursorY = y;
        }

        // ══════════════════════════════════════════════════════════════════
        // File filter for Avalonia storage provider dialogs
        // ══════════════════════════════════════════════════════════════════

        private static readonly FilePickerFileType AsepriteFilter = new("Aseprite Files")
        {
            Patterns = new[] { "*.aseprite", "*.ase" },
            MimeTypes = new[] { "application/octet-stream" },
        };

        private Window? MainWindow =>
            (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        // ══════════════════════════════════════════════════════════════════
        // Commands - MenuBar
        // ══════════════════════════════════════════════════════════════════

        // FILE 
        [RelayCommand]
        private void NewDocument()
        {
            SetDocument(CreateDefaultDocument());
            Palette.LoadDefaultPalette();
            StatusText = "New document created (32x32)";
        }

        [RelayCommand]
        private async Task OpenDocument()
        {
            var window = MainWindow;
            if (window == null) return;

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Aseprite File",
                AllowMultiple = false,
                FileTypeFilter = new[] { AsepriteFilter },
            });

            if (files.Count == 0) return;

            string path = files[0].TryGetLocalPath() ?? string.Empty;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var doc = DocumentIO.Load(path);
                SetDocument(doc);
                StatusText = $"Opened: {Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening file: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SaveDocument()
        {
            var doc = ActiveDocument;
            if (doc == null) return;

            if (!string.IsNullOrEmpty(doc.FilePath) && DocumentIO.IsSupported(doc.FilePath))
            {
                try
                {
                    DocumentIO.Save(doc, doc.FilePath);
                    StatusText = $"Saved: {Path.GetFileName(doc.FilePath)}";
                    OnPropertyChanged(nameof(TitleText));
                }
                catch (Exception ex)
                {
                    StatusText = $"Error saving: {ex.Message}";
                }
            }
            else
            {
                // No valid path yet – fall through to Save As
                _ = SaveAsDocumentAsync();
            }
        }

        [RelayCommand]
        private async Task SaveAsDocument() => await SaveAsDocumentAsync();

        private async Task SaveAsDocumentAsync()
        {
            var doc = ActiveDocument;
            if (doc == null) return;

            var window = MainWindow;
            if (window == null) return;

            string suggestedName = doc.DisplayName.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase)
                ? doc.DisplayName
                : doc.DisplayName + ".aseprite";

            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Aseprite File",
                SuggestedFileName = suggestedName,
                DefaultExtension = ".aseprite",
                FileTypeChoices = new[] { AsepriteFilter },
            });

            if (file == null) return;

            string path = file.TryGetLocalPath() ?? string.Empty;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                DocumentIO.Save(doc, path);
                StatusText = $"Saved: {Path.GetFileName(path)}";
                OnPropertyChanged(nameof(TitleText));
            }
            catch (Exception ex)
            {
                StatusText = $"Error saving: {ex.Message}";
            }
        }

        [RelayCommand] private void ExportDocument() => StatusText = "Export Document — not yet implemented";
        [RelayCommand] private void ShareDocument() => StatusText = "Share Document — not yet implemented";
        [RelayCommand] private void CloseDocument() => StatusText = "Close Document — not yet implemented";
        [RelayCommand] private void CloseAllDocument() => StatusText = "Close All Document — not yet implemented";
        [RelayCommand] private void ImportSpriteSheet() => StatusText = "Import Sprite Sheet — not yet implemented";
        [RelayCommand] private void ExportSpriteSheet() => StatusText = "Export Sprite Sheet — not yet implemented";
        [RelayCommand] private void RepeatLastExport() => StatusText = "Repeat Last Export — not yet implemented";
        [RelayCommand] private void ExportTileset() => StatusText = "Export Tileset — not yet implemented";

        [RelayCommand]
        private void Exit()
        {
            if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Shutdown();
        }

        // ══════════════════════════════════════════════════════════════════
        // Commands – EDIT
        // ══════════════════════════════════════════════════════════════════

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            UndoStack.Undo();
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
            StatusText = UndoStack.CanUndo ? $"Undid: {UndoStack.NextRedoName}" : "Nothing to undo.";
        }
        private bool CanUndo() => UndoStack.CanUndo;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            UndoStack.Redo();
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
            StatusText = UndoStack.CanRedo ? $"Redid: {UndoStack.NextUndoName}" : "Nothing to redo.";
        }
        private bool CanRedo() => UndoStack.CanRedo;

        [RelayCommand] private void UndoHistory() => StatusText = "Undo History — not yet implemented";
        [RelayCommand] private void Cut() => StatusText = "Cut — not yet implemented";
        [RelayCommand] private void Copy() => StatusText = "Copy — not yet implemented";
        [RelayCommand] private void CopyMerged() => StatusText = "Copy Merged — not yet implemented";
        [RelayCommand] private void Paste() => StatusText = "Paste — not yet implemented";
        [RelayCommand] private void PasteasNewSprite() => StatusText = "Paste as New Sprite — not yet implemented";
        [RelayCommand] private void PasteasNewLayer() => StatusText = "Paste as New Layer — not yet implemented";
        [RelayCommand] private void PasteasNewReferenceLayer() => StatusText = "Paste as New Reference Layer — not yet implemented";
        [RelayCommand] private void Delete() => StatusText = "Delete — not yet implemented";
        [RelayCommand] private void Fill() => StatusText = "Fill — not yet implemented";
        [RelayCommand] private void Stroke() => StatusText = "Stroke — not yet implemented";
        [RelayCommand] private void Rotate180() => StatusText = "Rotate 180 — not yet implemented";
        [RelayCommand] private void Rotate90CW() => StatusText = "Rotate 90 CW — not yet implemented";
        [RelayCommand] private void Rotate90CCW() => StatusText = "Rotate 90 CCW — not yet implemented";
        [RelayCommand] private void FlipHorizontal() => StatusText = "Flip Horizontal — not yet implemented";
        [RelayCommand] private void FlipVertical() => StatusText = "Flip Vertical — not yet implemented";
        [RelayCommand] private void Transform() => StatusText = "Transform — not yet implemented";
        [RelayCommand] private void ShiftLeft() => StatusText = "Shift Left — not yet implemented";
        [RelayCommand] private void ShiftRight() => StatusText = "Shift Right — not yet implemented";
        [RelayCommand] private void ShiftUp() => StatusText = "Shift Up — not yet implemented";
        [RelayCommand] private void ShiftDown() => StatusText = "Shift Down — not yet implemented";
        [RelayCommand] private void NewBrush() => StatusText = "New Brush — not yet implemented";
        [RelayCommand] private void NewSpriteFromSelection() => StatusText = "New Sprite From Selection — not yet implemented";
        [RelayCommand] private void ReplaceColor() => StatusText = "Replace Color — not yet implemented";
        [RelayCommand] private void Invert() => StatusText = "Invert — not yet implemented";
        [RelayCommand] private void AdjustmentsBrighnessContrast() => StatusText = "Brightness/Contrast — not yet implemented";
        [RelayCommand] private void AdjustmentsHueSaturation() => StatusText = "Hue/Saturation — not yet implemented";
        [RelayCommand] private void AdjustmentsColorCurve() => StatusText = "Color Curve — not yet implemented";
        [RelayCommand] private void FXOutline() => StatusText = "FX Outline — not yet implemented";
        [RelayCommand] private void FXConvulutionMatrix() => StatusText = "FX Convolution Matrix — not yet implemented";
        [RelayCommand] private void FXDespeckle() => StatusText = "FX Despeckle — not yet implemented";
        [RelayCommand] private void KeyboardShortcuts() => StatusText = "Keyboard Shortcuts — not yet implemented";
        [RelayCommand] private void Preferences() => StatusText = "Preferences — not yet implemented";

        // SPRITE 
        [RelayCommand] private void SpriteProperties() => StatusText = "Sprite Properties — not yet implemented";
        [RelayCommand] private void ColorModeRgbColor() => StatusText = "Color Mode: RGB";
        [RelayCommand] private void ColorModeGrayscaleColor() => StatusText = "Color Mode: Grayscale";
        [RelayCommand] private void ColorModeIndexed() => StatusText = "Color Mode: Indexed";
        [RelayCommand] private void ColorModeMoreOption() => StatusText = "Color Mode Options — not yet implemented";
        [RelayCommand] private void Duplicate() => StatusText = "Duplicate — not yet implemented";
        [RelayCommand] private void Spritesize() => StatusText = "Sprite Size — not yet implemented";
        [RelayCommand] private void Canvasize() => StatusText = "Canvas Size — not yet implemented";
        [RelayCommand] private void RotateCanva180() => StatusText = "Rotate Canvas 180 — not yet implemented";
        [RelayCommand] private void RotateCanva90CW() => StatusText = "Rotate Canvas 90 CW — not yet implemented";
        [RelayCommand] private void RotateCanva90CCW() => StatusText = "Rotate Canvas 90 CCW — not yet implemented";
        [RelayCommand] private void FlipCanvaHorizontal() => StatusText = "Flip Canvas Horizontal — not yet implemented";
        [RelayCommand] private void FlipCanvaVertical() => StatusText = "Flip Canvas Vertical — not yet implemented";
        [RelayCommand] private void Crop() => StatusText = "Crop — not yet implemented";
        [RelayCommand] private void Trim() => StatusText = "Trim — not yet implemented";

        // LAYER 
        [RelayCommand] private void LayerProperties() => StatusText = "Layer Properties — not yet implemented";
        [RelayCommand] private void LayerVisible() => StatusText = "Layer Visible — not yet implemented";
        [RelayCommand] private void LockLayer() => StatusText = "Lock Layer — not yet implemented";
        [RelayCommand] private void OpenGroup() => StatusText = "Open Group — not yet implemented";
        [RelayCommand] private void NewLayer() => StatusText = "New Layer — not yet implemented";
        [RelayCommand] private void NewGroup() => StatusText = "New Group — not yet implemented";
        [RelayCommand] private void NewLayerViaCopy() => StatusText = "New Layer Via Copy — not yet implemented";
        [RelayCommand] private void NewLayerViaCut() => StatusText = "New Layer Via Cut — not yet implemented";
        [RelayCommand] private void NewReferenceLayerFromFile() => StatusText = "New Reference Layer From File — not yet implemented";
        [RelayCommand] private void NewTilemapLayer() => StatusText = "New Tilemap Layer — not yet implemented";
        [RelayCommand] private void DeleteLayer() => StatusText = "Delete Layer — not yet implemented";
        [RelayCommand] private void ConvertToBackgroundLayer() => StatusText = "Convert To Background Layer — not yet implemented";
        [RelayCommand] private void ConvertToTilemap() => StatusText = "Convert To Tilemap — not yet implemented";
        [RelayCommand] private void DuplicateLayer() => StatusText = "Duplicate Layer — not yet implemented";
        [RelayCommand] private void MergeDown() => StatusText = "Merge Down — not yet implemented";
        [RelayCommand] private void Flatten() => StatusText = "Flatten — not yet implemented";
        [RelayCommand] private void FlattenVisible() => StatusText = "Flatten Visible — not yet implemented";


        // FRAME 
        [RelayCommand] private void FrameProperties() => StatusText = "Frame Properties — not yet implemented";
        [RelayCommand] private void CelProperties() => StatusText = "Cel Properties — not yet implemented";
        [RelayCommand] private void NewFrame() => StatusText = "New Frame — not yet implemented";
        [RelayCommand] private void NewEmptyFrame() => StatusText = "New Empty Frame — not yet implemented";
        [RelayCommand] private void DuplicateCels() => StatusText = "Duplicate Cel(s) — not yet implemented";
        [RelayCommand] private void DuplicateLinkedCels() => StatusText = "Duplicate Linked Cel(s) — not yet implemented";
        [RelayCommand] private void DeleteFrame() => StatusText = "Delete Frame — not yet implemented";
        [RelayCommand] private void PlayAnimation() => TimelineVM.TogglePlayCommand.Execute(null);
        [RelayCommand] private void PlayPreviewAnimation() => StatusText = "Play Preview Animation — not yet implemented";
        [RelayCommand] private void PlaybackSpeed025() => StatusText = "Playback Speed: 0.25x";
        [RelayCommand] private void PlaybackSpeed05() => StatusText = "Playback Speed: 0.5x";
        [RelayCommand] private void PlaybackSpeed1() => StatusText = "Playback Speed: 1x";
        [RelayCommand] private void PlaybackSpeed15() => StatusText = "Playback Speed: 1.5x";
        [RelayCommand] private void PlaybackSpeed2() => StatusText = "Playback Speed: 2x";
        [RelayCommand] private void PlaybackSpeed3() => StatusText = "Playback Speed: 3x";
        [RelayCommand] private void PlayOnce() => StatusText = "Play Once — not yet implemented";
        [RelayCommand] private void PlayAllFrames() => StatusText = "Play All Frames — not yet implemented";
        [RelayCommand] private void PlaySubtags() => StatusText = "Play Subtags — not yet implemented";
        [RelayCommand] private void RewindOnStop() => StatusText = "Rewind on Stop — not yet implemented";
        [RelayCommand] private void TagProperties() => StatusText = "Tag Properties — not yet implemented";
        [RelayCommand] private void NewTag() => StatusText = "New Tag — not yet implemented";
        [RelayCommand] private void DeleteTag() => StatusText = "Delete Tag — not yet implemented";
        [RelayCommand] private void FirstFrame() => TimelineVM.GoToFirstFrameCommand.Execute(null);
        [RelayCommand] private void PreviousFrame() => TimelineVM.PreviousFrameCommand.Execute(null);
        [RelayCommand] private void NextFrame() => TimelineVM.NextFrameCommand.Execute(null);
        [RelayCommand] private void LastFrame() => TimelineVM.GoToLastFrameCommand.Execute(null);
        [RelayCommand] private void FirstFrameInTag() => StatusText = "First Frame in Tag — not yet implemented";
        [RelayCommand] private void LastFrameInTag() => StatusText = "Last Frame in Tag — not yet implemented";
        [RelayCommand] private void GoToFrame() => StatusText = "Go to Frame — not yet implemented";
        [RelayCommand] private void ConstantFrameRate() => StatusText = "Constant Frame Rate — not yet implemented";
        [RelayCommand] private void ReverseFrames() => StatusText = "Reverse Frames — not yet implemented";

        // SELECT
        [RelayCommand] private void SelectAll() => StatusText = "Select All — not yet implemented";
        [RelayCommand] private void Deselect() => StatusText = "Deselect — not yet implemented";
        [RelayCommand] private void Reselect() => StatusText = "Reselect — not yet implemented";
        [RelayCommand] private void InverseSelection() => StatusText = "Inverse Selection — not yet implemented";
        [RelayCommand] private void ColorRange() => StatusText = "Color Range — not yet implemented";
        [RelayCommand] private void ModifyBorder() => StatusText = "Modify Border — not yet implemented";
        [RelayCommand] private void ModifyExpand() => StatusText = "Modify Expand — not yet implemented";
        [RelayCommand] private void ModifyContract() => StatusText = "Modify Contract — not yet implemented";
        [RelayCommand] private void LoadFromMsk() => StatusText = "Load from MSK — not yet implemented";
        [RelayCommand] private void SaveToMsk() => StatusText = "Save to MSK — not yet implemented";

        // VIEW
        [RelayCommand] private void DuplicateView() => StatusText = "Duplicate View — not yet implemented";
        [RelayCommand] private void WorkspaceLayout() => StatusText = "Workspace Layout — not yet implemented";
        [RelayCommand] private void RunCommand() => StatusText = "Run Command — not yet implemented";
        [RelayCommand] private void Extras() => StatusText = "Extras — not yet implemented";
        [RelayCommand] private void ShowLayerEdges() => StatusText = "Show Layer Edges — not yet implemented";
        [RelayCommand] private void ShowSelectionEdges() => StatusText = "Show Selection Edges — not yet implemented";
        [RelayCommand] private void ShowGrid() => StatusText = "Show Grid — not yet implemented";
        [RelayCommand] private void ShowAutoGuides() => StatusText = "Show Auto Guides — not yet implemented";
        [RelayCommand] private void ShowSlices() => StatusText = "Show Slices — not yet implemented";
        [RelayCommand] private void ShowPixelGrid() => StatusText = "Show Pixel Grid — not yet implemented";
        [RelayCommand] private void ShowTileNumbers() => StatusText = "Show Tile Numbers — not yet implemented";
        [RelayCommand] private void ShowBrushPreview() => StatusText = "Show Brush Preview — not yet implemented";
        [RelayCommand] private void GridSettings() => StatusText = "Grid Settings — not yet implemented";
        [RelayCommand] private void SelectionAsGrid() => StatusText = "Selection as Grid — not yet implemented";
        [RelayCommand] private void SnapToGrid() => StatusText = "Snap to Grid — not yet implemented";
        [RelayCommand] private void TiledModeNone() => StatusText = "Tiled Mode: None";
        [RelayCommand] private void TiledBothAxes() => StatusText = "Tiled Mode: Both Axes";
        [RelayCommand] private void TiledXAxis() => StatusText = "Tiled Mode: X Axis";
        [RelayCommand] private void TiledYAxis() => StatusText = "Tiled Mode: Y Axis";
        [RelayCommand] private void SymmetryOptions() => StatusText = "Symmetry Options — not yet implemented";
        [RelayCommand] private void SetLoopSection() => StatusText = "Set Loop Section — not yet implemented";
        [RelayCommand] private void ShowOnionSkin() => StatusText = "Show Onion Skin — not yet implemented";
        [RelayCommand] private void Timeline() => StatusText = "Timeline — not yet implemented";
        [RelayCommand] private void Preview() => StatusText = "Preview — not yet implemented";
        [RelayCommand] private void PreviewHideOtherLayers() => StatusText = "Preview Hide Other Layers — not yet implemented";
        [RelayCommand] private void PreviewBrushPreview() => StatusText = "Preview Brush Preview — not yet implemented";
        [RelayCommand] private void AdvancedMode() => StatusText = "Advanced Mode — not yet implemented";
        [RelayCommand] private void FullScreenMode() => StatusText = "Full Screen Mode — not yet implemented";
        [RelayCommand] private void FullScreenPreview() => StatusText = "Full Screen Preview — not yet implemented";
        [RelayCommand] private void Home() => StatusText = "Home — not yet implemented";
        [RelayCommand] private void RefreshReloadTheme() => StatusText = "Refresh & Reload Theme — not yet implemented";

        // HELP 
        [RelayCommand] private void Readme() => StatusText = "Readme — not yet implemented";
        [RelayCommand] private void QuickReference() => StatusText = "Quick Reference — not yet implemented";
        [RelayCommand] private void Documentation() => StatusText = "Documentation — not yet implemented";
        [RelayCommand] private void Tutorial() => StatusText = "Tutorial — not yet implemented";
        [RelayCommand] private void ReleaseNotes() => StatusText = "Release Notes — not yet implemented";
        [RelayCommand] private void Twitter() => StatusText = "Twitter — not yet implemented";
        [RelayCommand] private void About() => StatusText = "About SharpSprite";

        // ══════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════

        private void SetDocument(Document doc)
        {
            // Unsubscribe old
            if (ActiveDocument != null)
                ActiveDocument.ModifiedChanged -= OnDocumentModifiedChanged;

            var newStack = new UndoStack(100);
            newStack.Changed += OnUndoStackChanged;

            // Replace undo stack first so the canvas sees the new one
            UndoStack = newStack;
            ActiveDocument = doc;
            doc.ModifiedChanged += OnDocumentModifiedChanged;

            TimelineVM.SyncFromDocument(doc);
            Palette.LoadFromPalette(doc.Sprite.GetPalette(0));

            StatusBar.SpriteWidth = doc.Sprite.Width;
            StatusBar.SpriteHeight = doc.Sprite.Height;
            StatusBar.ColorMode = doc.Sprite.ColorMode.ToString();
            StatusBar.TotalFrames = doc.Sprite.FrameCount;
            StatusBar.CurrentFrame = 1;

            OnPropertyChanged(nameof(TitleText));
            OnPropertyChanged(nameof(ActiveFrame));
            OnPropertyChanged(nameof(CanvasZoom));
        }

        private void OnDocumentModifiedChanged(object? sender, EventArgs e)
            => OnPropertyChanged(nameof(TitleText));

        private void OnUndoStackChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        private static Document CreateDefaultDocument()
        {
            var doc = SpriteFactory.CreateBlankRgba(32, 32);
            doc.IsModified = false;
            return doc;
        }
    }
}