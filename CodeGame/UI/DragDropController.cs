using CodeGame.Blocks;
using CodeGame.UI.Views;
using Terminal.Gui;

namespace CodeGame.UI;

public enum DragSource { Palette, Sequence }

public class DragState
{
    public DragSource Source { get; set; }
    public BlockType BlockType { get; set; }
    public int SequenceIndex { get; set; } // only valid when Source == Sequence
    public int GhostX { get; set; }
    public int GhostY { get; set; }
}

/// <summary>
/// Coordinates mouse drag-and-drop between BlockPaletteView and BlockSequenceView.
/// </summary>
public class DragDropController
{
    private readonly BlockPaletteView _palette;
    private readonly BlockSequenceView _sequence;
    private readonly View _root;

    private DragState? _drag;
    private View? _ghostView;

    public event Action? SequenceChanged;

    public DragDropController(BlockPaletteView palette, BlockSequenceView sequence, View root)
    {
        _palette = palette;
        _sequence = sequence;
        _root = root;

        _palette.BlockDragStarted += OnPaletteDragStarted;
        _sequence.BlockReorderStarted += OnSequenceReorderStarted;

        // Hook global mouse for move and release
        Application.RootMouseEvent += OnGlobalMouse;
    }

    private void OnPaletteDragStarted(BlockType type, MouseEvent me)
    {
        if (_drag != null) return;
        _drag = new DragState
        {
            Source = DragSource.Palette,
            BlockType = type,
            GhostX = me.X + _palette.Frame.X,
            GhostY = me.Y + _palette.Frame.Y
        };
        ShowGhost(new CodeBlock(type));
    }

    private void OnSequenceReorderStarted(int index, MouseEvent me)
    {
        if (_drag != null) return;
        _drag = new DragState
        {
            Source = DragSource.Sequence,
            BlockType = _sequence.Blocks[index].Type,
            SequenceIndex = index,
            GhostX = me.X + _sequence.Frame.X,
            GhostY = me.Y + _sequence.Frame.Y
        };
        ShowGhost(_sequence.Blocks[index]);
    }

    private void OnGlobalMouse(MouseEvent me)
    {
        if (_drag == null) return;

        _drag.GhostX = me.X;
        _drag.GhostY = me.Y;

        if (_ghostView != null)
        {
            _ghostView.Frame = new Rect(me.X, me.Y, _ghostView.Frame.Width, _ghostView.Frame.Height);
        }

        // Highlight drop target in sequence
        int slot = _sequence.SlotAtScreenY(me.Y);
        _sequence.DropTargetIndex = slot;
        _sequence.SetNeedsDisplay();

        if (me.Flags.HasFlag(MouseFlags.Button1Released))
            OnDrop(me);
    }

    private void OnDrop(MouseEvent me)
    {
        HideGhost();
        _sequence.DropTargetIndex = -1;

        if (_drag == null) return;

        // Determine drop slot
        int slot = _sequence.SlotAtScreenY(me.Y);

        // Check if dropped outside sequence (to delete)
        bool overSequence = me.X >= _sequence.Frame.X && me.X < _sequence.Frame.X + _sequence.Frame.Width
                         && me.Y >= _sequence.Frame.Y && me.Y < _sequence.Frame.Y + _sequence.Frame.Height;

        if (_drag.Source == DragSource.Sequence)
        {
            // Remove from original position
            var block = _sequence.Blocks[_drag.SequenceIndex];
            _sequence.Blocks.RemoveAt(_drag.SequenceIndex);

            if (overSequence && slot >= 0)
            {
                // Adjust index after removal
                int insertAt = slot;
                if (slot > _drag.SequenceIndex) insertAt--;
                insertAt = Math.Clamp(insertAt, 0, _sequence.Blocks.Count);
                _sequence.Blocks.Insert(insertAt, block);
            }
            // If dropped outside, block is deleted (already removed)
        }
        else if (_drag.Source == DragSource.Palette && overSequence && slot >= 0)
        {
            int insertAt = Math.Clamp(slot, 0, _sequence.Blocks.Count);
            int repeatCount = _drag.BlockType == BlockType.Repeat ? 2 : 1;
            _sequence.Blocks.Insert(insertAt, new CodeBlock(_drag.BlockType, repeatCount));
        }

        _drag = null;
        _sequence.SetNeedsDisplay();
        SequenceChanged?.Invoke();
    }

    private void ShowGhost(CodeBlock block)
    {
        HideGhost();
        string label = $"[ {block.DisplayName} ]";
        _ghostView = new Label(label)
        {
            Frame = new Rect(_drag!.GhostX, _drag.GhostY, label.Length, 1),
            ColorScheme = new ColorScheme
            {
                Normal = BlockPaletteView.BlockColor(block.Type)
            }
        };
        _root.Add(_ghostView);
        _root.SetNeedsDisplay();
    }

    private void HideGhost()
    {
        if (_ghostView != null)
        {
            _root.Remove(_ghostView);
            _ghostView = null;
            _root.SetNeedsDisplay();
        }
    }
}
