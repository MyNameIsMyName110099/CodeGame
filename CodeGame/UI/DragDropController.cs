using CodeGame.Blocks;
using CodeGame.UI.Views;
using Terminal.Gui;

namespace CodeGame.UI;

public enum DragSource { Palette, Sequence }

public class DragState
{
    public DragSource Source { get; set; }
    public BlockType BlockType { get; set; }
    public CodeBlock? DraggedBlock { get; set; }
    public List<CodeBlock>? SourceList { get; set; }
    public int SourceIndex { get; set; }
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

    private void OnSequenceReorderStarted(CodeBlock block, List<CodeBlock> sourceList, int sourceIndex, MouseEvent me)
    {
        if (_drag != null) return;
        _drag = new DragState
        {
            Source = DragSource.Sequence,
            BlockType = block.Type,
            DraggedBlock = block,
            SourceList = sourceList,
            SourceIndex = sourceIndex,
            GhostX = me.X + _sequence.Frame.X,
            GhostY = me.Y + _sequence.Frame.Y
        };
        ShowGhost(block);
    }

    private void OnGlobalMouse(MouseEvent me)
    {
        if (_drag == null) return;

        int localX = me.X - _root.Frame.X;
        int localY = me.Y - _root.Frame.Y;

        _drag.GhostX = localX;
        _drag.GhostY = localY;

        if (_ghostView != null)
        {
            _ghostView.Frame = new Rect(localX, localY, _ghostView.Frame.Width, _ghostView.Frame.Height);
        }

        int visualRow = _sequence.VisualRowAtScreenY(me.Y);
        _sequence.DropTargetIndex = visualRow;
        _sequence.SetNeedsDisplay();

        if (me.Flags.HasFlag(MouseFlags.Button1Released))
            OnDrop(me);
    }

    private void OnDrop(MouseEvent me)
    {
        HideGhost();
        _sequence.DropTargetIndex = -1;

        if (_drag == null) return;

        var dropSlot = _sequence.GetDropSlotAtScreenY(me.Y);

        int seqScreenX = _root.Frame.X + _sequence.Frame.X;
        int seqScreenY = _root.Frame.Y + _sequence.Frame.Y;
        bool overSequence = me.X >= seqScreenX && me.X < seqScreenX + _sequence.Frame.Width
                         && me.Y >= seqScreenY && me.Y < seqScreenY + _sequence.Frame.Height;

        if (_drag.Source == DragSource.Sequence)
        {
            var block = _drag.DraggedBlock!;

            bool selfDrop = block.Type == BlockType.Repeat && dropSlot != null
                         && IsDescendantList(block, dropSlot.List);

            _drag.SourceList!.RemoveAt(_drag.SourceIndex);

            if (overSequence && dropSlot != null && !selfDrop)
            {
                int insertAt = dropSlot.Index;
                if (ReferenceEquals(dropSlot.List, _drag.SourceList) && dropSlot.Index > _drag.SourceIndex)
                    insertAt--;
                insertAt = Math.Clamp(insertAt, 0, dropSlot.List.Count);
                dropSlot.List.Insert(insertAt, block);
            }
            else if (!overSequence)
            {
                // Dropped outside — block is deleted
            }
            else
            {
                int restoreAt = Math.Min(_drag.SourceIndex, _drag.SourceList.Count);
                _drag.SourceList.Insert(restoreAt, block);
            }
        }
        else if (_drag.Source == DragSource.Palette && overSequence && dropSlot != null)
        {
            int insertAt = Math.Clamp(dropSlot.Index, 0, dropSlot.List.Count);
            int repeatCount = _drag.BlockType == BlockType.Repeat ? 2 : 1;
            dropSlot.List.Insert(insertAt, new CodeBlock(_drag.BlockType, repeatCount));
        }

        _drag = null;
        _sequence.SetNeedsDisplay();
        SequenceChanged?.Invoke();
    }

    private static bool IsDescendantList(CodeBlock repeat, List<CodeBlock> list)
    {
        if (ReferenceEquals(repeat.Children, list))
            return true;
        foreach (var child in repeat.Children)
        {
            if (child.Type == BlockType.Repeat && IsDescendantList(child, list))
                return true;
        }
        return false;
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
