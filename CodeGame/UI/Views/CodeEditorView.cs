using CodeGame.Blocks;
using Terminal.Gui;

namespace CodeGame.UI.Views;

/// <summary>
/// The full code-editor screen: palette on the left, sequence on the right, Run button at the bottom.
/// </summary>
public class CodeEditorView : View
{
    public BlockPaletteView Palette { get; }
    public BlockSequenceView Sequence { get; }

    public event Action? RunRequested;
    public event Action? ClearRequested;

    private readonly DragDropController _dragDrop;

    public CodeEditorView()
    {
        CanFocus = true;
        WantMousePositionReports = true;

        Palette = new BlockPaletteView
        {
            Frame = new Rect(0, 0, 18, 22),
            ColorScheme = Colors.Base
        };

        Sequence = new BlockSequenceView
        {
            Frame = new Rect(22, 0, 40, 22),
            ColorScheme = Colors.Base
        };

        var runButton = new Button("  RUN (R)  ")
        {
            Frame = new Rect(22, 23, 14, 1),
            ColorScheme = new ColorScheme
            {
                Normal   = new TAttr(Color.Black, Color.BrightGreen),
                Focus    = new TAttr(Color.Black, Color.Green),
                HotNormal = new TAttr(Color.Black, Color.BrightGreen),
                HotFocus  = new TAttr(Color.Black, Color.Green)
            }
        };
        runButton.Clicked += () => RunRequested?.Invoke();

        var clearButton = new Button("  CLEAR (C)  ")
        {
            Frame = new Rect(38, 23, 16, 1),
            ColorScheme = new ColorScheme
            {
                Normal    = new TAttr(Color.White, Color.BrightRed),
                Focus     = new TAttr(Color.White, Color.Red),
                HotNormal = new TAttr(Color.White, Color.BrightRed),
                HotFocus  = new TAttr(Color.White, Color.Red)
            }
        };
        clearButton.Clicked += () => ClearRequested?.Invoke();

        Add(Palette, Sequence, runButton, clearButton);

        _dragDrop = new DragDropController(Palette, Sequence, this);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        if (keyEvent.Key == Key.r || keyEvent.Key == Key.R)
        {
            RunRequested?.Invoke();
            return true;
        }
        if (keyEvent.Key == Key.c || keyEvent.Key == Key.C)
        {
            ClearRequested?.Invoke();
            return true;
        }
        return base.ProcessKey(keyEvent);
    }
}
