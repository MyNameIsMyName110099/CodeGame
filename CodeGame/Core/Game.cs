using CodeGame.Entities;
using CodeGame.Execution;
using CodeGame.Level;
using CodeGame.UI.Views;
using Terminal.Gui;

namespace CodeGame.Core;

public class Game
{
    private readonly Toplevel _top;
    private readonly ProgressTracker _progress;
    private readonly MenuView _menuView;

    // ── Per-level state (recreated when a level is loaded) ─────────
    private GameState _state = GameState.Menu;
    private int _currentLevel;
    private Level.Level? _level;
    private Character? _character;
    private CodeExecutor? _executor;

    private GameView? _gameView;
    private CodeEditorView? _editorView;
    private Button? _runButton;
    private Button? _clearButton;
    private Button? _menuButton;
    private Label? _statusLabel;

    public Game()
    {
        Application.Init();

        _top = Application.Top;
        _top.ColorScheme = Colors.Base;

        _progress = new ProgressTracker();

        int screenW = _top.Frame.Width;
        int screenH = _top.Frame.Height;

        _menuView = new MenuView(_progress)
        {
            Frame = new Rect(0, 0, screenW, screenH),
            ColorScheme = new ColorScheme { Normal = new TAttr(Color.White, Color.Black) }
        };
        _menuView.LevelSelected += OnLevelSelected;

        _top.Add(_menuView);
        _top.KeyPress += OnKeyPress;
    }

    public void Run() => Application.Run();

    // ── Navigation ─────────────────────────────────────────────────

    private void OnLevelSelected(int levelNumber)
    {
        _currentLevel = levelNumber;
        LoadLevel(levelNumber);
    }

    private void LoadLevel(int levelNumber)
    {
        _top.Remove(_menuView);

        _level = LevelGenerator.Generate(levelNumber);
        _character = new Character(_level.StartPosition.X, _level.StartPosition.Y);
        _executor = new CodeExecutor(_level);

        int screenW = _top.Frame.Width;
        int screenH = _top.Frame.Height;
        int editorX = _level.Width + 1;
        int editorW = Math.Max(22, screenW - editorX);

        _statusLabel = new Label($"{LevelGenerator.GetLevelName(levelNumber)} -- Drag blocks then press RUN")
        {
            Frame = new Rect(0, screenH - 1, screenW, 1),
            ColorScheme = new ColorScheme { Normal = new TAttr(Color.White, Color.DarkGray) }
        };

        _gameView = new GameView
        {
            Frame = new Rect(0, 0, _level.Width, _level.Height),
            ColorScheme = new ColorScheme { Normal = new TAttr(Color.White, Color.Black) }
        };
        _gameView.SetLevel(_level, _character);

        _runButton = new Button("RUN (R)")
        {
            X = 1,
            Y = _level.Height + 1,
            ColorScheme = new ColorScheme
            {
                Normal    = new TAttr(Color.Black, Color.BrightGreen),
                Focus     = new TAttr(Color.Black, Color.Green),
                HotNormal = new TAttr(Color.Black, Color.BrightGreen),
                HotFocus  = new TAttr(Color.Black, Color.Green)
            }
        };
        _runButton.Clicked += OnRunRequested;

        _clearButton = new Button("CLEAR (C)")
        {
            X = Pos.Right(_runButton) + 2,
            Y = _level.Height + 1,
            ColorScheme = new ColorScheme
            {
                Normal    = new TAttr(Color.White, Color.BrightRed),
                Focus     = new TAttr(Color.White, Color.Red),
                HotNormal = new TAttr(Color.White, Color.BrightRed),
                HotFocus  = new TAttr(Color.White, Color.Red)
            }
        };
        _clearButton.Clicked += OnClearRequested;

        _menuButton = new Button("MENU (M)")
        {
            X = Pos.Right(_clearButton) + 2,
            Y = _level.Height + 1,
            ColorScheme = new ColorScheme
            {
                Normal    = new TAttr(Color.White, Color.DarkGray),
                Focus     = new TAttr(Color.White, Color.Gray),
                HotNormal = new TAttr(Color.White, Color.DarkGray),
                HotFocus  = new TAttr(Color.White, Color.Gray)
            }
        };
        _menuButton.Clicked += OnReturnToMenu;

        _editorView = new CodeEditorView
        {
            Frame = new Rect(editorX, 0, editorW, screenH - 1),
            ColorScheme = Colors.Base
        };

        _top.Add(_gameView, _runButton, _clearButton, _menuButton, _editorView, _statusLabel);

        _state = GameState.CodeEditor;
        _editorView.SetFocus();
        Application.Refresh();
    }

    private void UnloadLevel()
    {
        if (_gameView != null) _top.Remove(_gameView);
        if (_runButton != null) _top.Remove(_runButton);
        if (_clearButton != null) _top.Remove(_clearButton);
        if (_menuButton != null) _top.Remove(_menuButton);
        if (_editorView != null) _top.Remove(_editorView);
        if (_statusLabel != null) _top.Remove(_statusLabel);

        _gameView = null; _editorView = null;
        _runButton = null; _clearButton = null; _menuButton = null;
        _statusLabel = null;
        _level = null; _character = null; _executor = null;
    }

    private void OnReturnToMenu()
    {
        UnloadLevel();
        _menuView.RefreshList();
        _top.Add(_menuView);
        _menuView.SetFocus();
        _state = GameState.Menu;
        Application.Refresh();
    }

    // ── Key handling ───────────────────────────────────────────────

    private void OnKeyPress(View.KeyEventEventArgs args)
    {
        if (args.KeyEvent.Key == (Key.Q | Key.CtrlMask))
        {
            Application.RequestStop();
            return;
        }

        if (_state == GameState.CodeEditor)
        {
            if (args.KeyEvent.Key is Key.r or Key.R) { OnRunRequested(); args.Handled = true; }
            if (args.KeyEvent.Key is Key.c or Key.C) { OnClearRequested(); args.Handled = true; }
            if (args.KeyEvent.Key is Key.m or Key.M) { OnReturnToMenu(); args.Handled = true; }
        }
    }

    // ── Execution ──────────────────────────────────────────────────

    private void OnRunRequested()
    {
        if (_state != GameState.CodeEditor) return;

        var blocks = _editorView!.Sequence.Blocks;
        if (blocks.Count == 0)
        {
            SetStatus("Add some blocks first!");
            return;
        }

        _character!.Respawn();
        var steps = _executor!.Execute(blocks, _character);

        _state = GameState.Running;
        _editorView.Enabled   = false;
        _runButton!.Enabled   = false;
        _clearButton!.Enabled = false;
        _menuButton!.Enabled  = false;

        SetStatus("Running...");
        Application.Refresh();

        _ = Task.Run(async () =>
        {
            foreach (var step in steps)
            {
                await Task.Delay(200);
                Application.MainLoop.Invoke(() =>
                {
                    _character.X = step.X;
                    _character.Y = step.Y;
                    _gameView!.Refresh(_character);
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
            _progress.MarkCompleted(_currentLevel);
            ShowOverlay("Level Complete!",
                $"You cleared {LevelGenerator.GetLevelName(_currentLevel)}! Press any key...",
                OnReturnToEditor);
        }
        else if (died)
        {
            _state = GameState.Dead;
            ShowOverlay("You Died!",
                "Your character hit a hazard. Press any key to try again...",
                OnReturnToEditor);
        }
        else
        {
            _state = GameState.Dead;
            ShowOverlay("Didn't make it!",
                "The program ended before reaching the checkpoint. Press any key...",
                OnReturnToEditor);
        }
    }

    private void ShowOverlay(string title, string message, Action onDismiss)
    {
        var dialog = new Dialog(title, 55, 7);
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
        if (_editorView == null) return;
        _editorView.Sequence.Blocks.Clear();
        _editorView.Sequence.SetNeedsDisplay();
        SetStatus("Sequence cleared.");
    }

    private void OnReturnToEditor()
    {
        _state = GameState.CodeEditor;
        _editorView!.Enabled  = true;
        _runButton!.Enabled   = true;
        _clearButton!.Enabled = true;
        _menuButton!.Enabled  = true;
        _character!.Respawn();
        _gameView!.Refresh(_character);
        SetStatus($"{LevelGenerator.GetLevelName(_currentLevel)} -- Drag blocks then press RUN");
        Application.Refresh();
    }

    private void SetStatus(string message)
    {
        if (_statusLabel == null) return;
        _statusLabel.Text = message;
        _statusLabel.SetNeedsDisplay();
    }
}
