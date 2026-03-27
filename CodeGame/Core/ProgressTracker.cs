namespace CodeGame.Core;

public class ProgressTracker
{
    private readonly HashSet<int> _completed = new();
    private readonly string _filePath;

    public ProgressTracker()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(appData, "CodeGame");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "progress.txt");
        Load();
    }

    public bool IsCompleted(int level) => _completed.Contains(level);

    public void MarkCompleted(int level)
    {
        if (_completed.Add(level))
            Save();
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        foreach (var line in File.ReadAllLines(_filePath))
        {
            if (int.TryParse(line.Trim(), out int n))
                _completed.Add(n);
        }
    }

    private void Save()
    {
        var lines = _completed.OrderBy(n => n).Select(n => n.ToString());
        File.WriteAllLines(_filePath, lines);
    }
}
