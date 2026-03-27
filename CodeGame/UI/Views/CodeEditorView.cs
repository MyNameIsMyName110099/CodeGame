using CodeGame.Blocks;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// Right-side panel: block palette on the left, sequence ("Your Program") on the right
/// with a fixed gap between them so there is room for more block types in the future.
/// </summary>
public class CodeEditorView : View
{
    public BlockPaletteView Palette { get; }
    public BlockSequenceView Sequence { get; }

    private readonly DragDropController _dragDrop;

    private const int PaletteWidth = 18;
    private const int PaletteSequenceGap = 4;

    public CodeEditorView()
    {
        CanFocus = true;
        WantMousePositionReports = true;

        Palette  = new BlockPaletteView  { ColorScheme = Colors.Base };
        Sequence = new BlockSequenceView { ColorScheme = Colors.Base };

        Add(Palette, Sequence);

        _dragDrop = new DragDropController(Palette, Sequence, this);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        int w = Bounds.Width;
        int h = Bounds.Height;

        Palette.Frame = new Rect(0, 0, PaletteWidth, h);

        int seqX = PaletteWidth + PaletteSequenceGap;
        int seqW = Math.Max(10, w - seqX);
        Sequence.Frame = new Rect(seqX, 0, seqW, h);
    }
}
