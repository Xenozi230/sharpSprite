namespace SharpSprite.App.Tools
{
    /// <summary>
    /// All available tool types. The enum value is used as a key in
    /// <see cref="ToolRegistry"/> and is bindable from the ViewModel.
    /// </summary>
    public enum ToolType
    {
        Pencil,
        Eraser,
        Pan,
        Zoom,
        Eyedropper,
        Fill,
        Selection,
        Line,
        Rectangle,
        Ellipse,
    }
}
