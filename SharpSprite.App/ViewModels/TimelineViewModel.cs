using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpSprite.Core.Document;

namespace SharpSprite.App.ViewModels
{
    /// <summary>
    /// Represents one row in the timeline: a layer.
    /// </summary>
    public partial class LayerRowViewModel : ObservableObject
    {
        [ObservableProperty] private string _name = "Layer";
        [ObservableProperty] private bool _isVisible = true;
        [ObservableProperty] private bool _isLocked = false;
        [ObservableProperty] private bool _isSelected = false;
        [ObservableProperty] private bool _isContinuous = true; // linked-cel icon
        [ObservableProperty] private int _depth = 0; // indent for groups

        public Layer? Layer { get; init; }

        // Which frames have cels
        public ObservableCollection<bool> HasCelAtFrame { get; } = new();
    }

    public partial class TimelineViewModel : ObservableObject
    {
        // ── Layers ────────────────────────────────────────────────────────
        public ObservableCollection<LayerRowViewModel> Layers { get; } = new();

        // ── Frame info ────────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrameLabel))]
        private int _frameCount = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrameLabel))]
        private int _currentFrame = 0;

        public string FrameLabel => $"{CurrentFrame + 1} / {FrameCount}";

        [ObservableProperty]
        private int _fps = 10;

        // ── Playback ──────────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotPlaying))]
        private bool _isPlaying = false;

        public bool IsNotPlaying => !IsPlaying;

        [ObservableProperty]
        private bool _looping = true;

        // ── Selected layer ────────────────────────────────────────────────
        [ObservableProperty]
        private LayerRowViewModel? _selectedLayer;

        // ── Commands ──────────────────────────────────────────────────────
        [RelayCommand]
        public void GoToFirstFrame() => CurrentFrame = 0;

        [RelayCommand]
        public void PreviousFrame()
        {
            if (CurrentFrame > 0) CurrentFrame--;
        }

        [RelayCommand]
        public void TogglePlay() => IsPlaying = !IsPlaying;

        [RelayCommand]
        public void NextFrame()
        {
            if (CurrentFrame < FrameCount - 1) CurrentFrame++;
            else if (Looping) CurrentFrame = 0;
        }

        [RelayCommand]
        public void GoToLastFrame() => CurrentFrame = FrameCount - 1;

        [RelayCommand]
        public void SelectLayer(LayerRowViewModel layer)
        {
            if (SelectedLayer != null) SelectedLayer.IsSelected = false;
            SelectedLayer = layer;
            layer.IsSelected = true;
        }

        [RelayCommand]
        public void ToggleLayerVisibility(LayerRowViewModel layer)
        {
            layer.IsVisible = !layer.IsVisible;
            if (layer.Layer != null)
                layer.Layer.IsVisible = layer.IsVisible;
        }

        [RelayCommand]
        public void ToggleLayerLock(LayerRowViewModel layer)
        {
            layer.IsLocked = !layer.IsLocked;
            if (layer.Layer != null)
                layer.Layer.IsEditable = !layer.IsLocked;
        }

        // ── Sync from document ────────────────────────────────────────────
        public void SyncFromDocument(Document doc)
        {
            Layers.Clear();
            FrameCount = doc.Sprite.FrameCount;
            CurrentFrame = 0;

            // Walk layers top-to-bottom for display (reverse of compositing)
            var allLayers = doc.Sprite.GetLayersForCompositing().Reverse().ToList();
            foreach (var layer in allLayers)
            {
                var row = new LayerRowViewModel
                {
                    Name = layer.Name,
                    IsVisible = layer.IsVisible,
                    IsLocked = !layer.IsEditable,
                    Layer = layer,
                };

                for (int f = 0; f < doc.Sprite.FrameCount; f++)
                    row.HasCelAtFrame.Add(layer.GetCel(f) != null);

                Layers.Add(row);
            }

            if (Layers.Count > 0)
                SelectLayer(Layers[0]);
        }
    }
}
