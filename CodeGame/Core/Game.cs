using CodeGame.Entities;
using CodeGame.Execution;
using CodeGame.Level;
using CodeGame.UI.Views;
using Terminal.Gui;

namespace CodeGame.Core;

public class Game
{
    private GameState _state = GameState.CodeEditor;
    private readonly Level.Level _level;
    private readonly Character _character;
    private readonly CodeExecutor _executor;

    private readonly CodeEditorView _editorView;
    private readonly GameView _gameView;
    private readonly Label _statusLabel;
    private readonly Toplevel _top;

    public Game()
    {
        Application.Init();

        _level = LevelData.Level1();
        _character = new Character(_level.StartPosition.X, _level.StartPosition.Y);
        _executor = new CodeExecutor(_level);

        _top = Application.Top;
        _top.ColorScheme = Colors.Base;

        // Status bar at the bottom
        _statusLabel = new Label("Drag blocks to build your program, then press RUN")
        {
            Frame = new Rect(0, Application.Top.Frame.Height - 1, Application.Top.Frame.Width, 1),
            ColorScheme = new ColorScheme { Normal = new TAttr(Color.White, Color.DarkGray) }
        };

        // Code editor (shown first)
        _editorView = new CodeEditorView
        {
            Frame = new Rect(0, 0, Application.Top.Frame.Width, Application.Top.Frame.Height - 2)
        };
        _editorView.RunRequested += OnRunRequested;
        _editorView.ClearRequested += OnClearRequested;

        // Game view (shown during execution)
        _gameView = new GameView
        {
            Frame = new Rect(0, 0, _level.Width, _level.Height),
            Visible = false
        };
        _gameView.SetLevel(_level, _character);

        _top.Add(_editorView, _gameView, _statusLabel);
        _top.KeyPress += OnKeyPress;
    }

    private void OnKeyPress(View.KeyEventEventArgs args)
    {
        // Allow quitting with Ctrl+Q from anywhere
        if (args.KeyEvent.Key == (Key.Q | Key.CtrlMask))
            Application.RequestStop();
    }

    public void Run() => Application.Run();

    private void OnRunRequested()
    {
        if (_state != GameState.CodeEditor) return;

        var blocks = _editorView.Sequence.Blocks;
        if (blocks.Count == 0)
        {
            SetStatus("Add some blocks first!");
            return;
        }

        _character.Respawn();
        var steps = _executor.Execute(blocks, _character);

        _state = GameState.Running;
        _editorView.Visible = false;
        _gameView.Visible = true;
        _gameView.SetNeedsDisplay();

        SetStatus("Running... watch your character go!");
        Application.Refresh();

        // Animate steps
        _ = Task.Run(async () =>
        {
            foreach (var step in steps)
            {
                await Task.Delay(200);
                Application.MainLoop.Invoke(() =>
                {
                    _character.X = step.X;
                    _character.Y = step.Y;
                    _gameView.Refresh(_character);
                    Application.Refresh();
                });

                if (step.Died || step.Won)
                    break;
            }

            await Task.Delay(300);
            Application.MainLoop.Invoke(() => OnExecutionComplete(steps));
        });
    }

    private void OnExecutionComplete(List<ExecutionStep> steps)
    {
        bool died = steps.Count > 0 && steps[^1].Died;
        bool won  = steps.Count > 0 && steps[^1].Won;

        if (won)
        {
            _state = GameState.Win;
            ShowOverlay("You Win!", "You reached the checkpoint! Press any key...", OnReturnToEditor);
        }
        else if (died)
        {
            _state = GameState.Dead;
            ShowOverlay("You Died!", "Your character fell on the spikes. Press any key to try again...", OnReturnToEditor);
        }
        else
        {
            _state = GameState.Dead;
            ShowOverlay("Didn't make it!", "The program ended before reaching the checkpoint. Press any key...", OnReturnToEditor);
        }
    }

    private void ShowOverlay(string title, string message, Action onDismiss)
    {
        var dialog = new Dialog(title, 50, 7);
        var msgLabel = new Label(message) { X = 1, Y = 1 };
        var okButton = new Button("OK");
        okButton.Clicked += () =>
        {
            Application.RequestStop(dialog);
            onDismiss();
        };
        dialog.Add(msgLabel);
        dialog.AddButton(okButton);
        Application.Run(dialog);
    }

    private void OnClearRequested()
    {
        _editorView.Sequence.Blocks.Clear();
        _editorView.Sequence.SetNeedsDisplay();
        SetStatus("Sequence cleared.");
    }

    private void OnReturnToEditor()
    {
        _state = GameState.CodeEditor;
        _gameView.Visible = false;
        _editorView.Visible = true;
        _character.Respawn();
        _gameView.Refresh(_character);
        SetStatus("Drag blocks to build your program, then press RUN");
        Application.Refresh();
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        _statusLabel.SetNeedsDisplay();
    }
}
