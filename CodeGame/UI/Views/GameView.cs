using CodeGame.Entities;
using CodeGame.Level;
using Terminal.Gui;

namespace CodeGame.UI.Views;

public class GameView : View
{
    private Level.Level? _level;
    private Character? _character;

    public GameView()
    {
        CanFocus = false;
    }

    public void SetLevel(Level.Level level, Character character)
    {
        _level = level;
        _character = character;
        SetNeedsDisplay();
    }

    public void Refresh(Character character)
    {
        _character = character;
        SetNeedsDisplay();
    }

    public override void Redraw(Rect bounds)
    {
        base.Redraw(bounds);
        if (_level == null || _character == null) return;

        for (int y = 0; y < _level.Height && y < bounds.Height; y++)
        {
            for (int x = 0; x < _level.Width && x < bounds.Width; x++)
            {
                int charX = (int)Math.Round(_character.X);
                int charY = (int)Math.Round(_character.Y);

                if (x == charX && y == charY)
                {
                    Driver.SetAttribute(new TAttr(Color.BrightYellow, Color.Black));
                    AddRune(x, y, '@');
                    continue;
                }

                var tile = _level.GetTile(x, y);
                switch (tile)
                {
                    case TileType.Platform:
                        Driver.SetAttribute(new TAttr(Color.Gray, Color.Black));
                        AddRune(x, y, '=');
                        break;
                    case TileType.Start:
                        Driver.SetAttribute(new TAttr(Color.Green, Color.Black));
                        AddRune(x, y, '=');
                        break;
                    case TileType.Spike:
                        Driver.SetAttribute(new TAttr(Color.BrightRed, Color.Black));
                        AddRune(x, y, '^');
                        break;
                    case TileType.Checkpoint:
                        Driver.SetAttribute(new TAttr(Color.BrightGreen, Color.Black));
                        AddRune(x, y, 'E');
                        break;
                    default:
                        Driver.SetAttribute(new TAttr(Color.Black, Color.Black));
                        AddRune(x, y, ' ');
                        break;
                }
            }
        }
    }

    private void AddRune(int x, int y, char c)
    {
        Move(x, y);
        Driver.AddRune(c);
    }
}
