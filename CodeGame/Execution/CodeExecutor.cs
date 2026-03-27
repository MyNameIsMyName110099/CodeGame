using CodeGame.Blocks;
using CodeGame.Entities;
using CodeGame.Level;

namespace CodeGame.Execution;

public class ExecutionStep
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool Died { get; set; }
    public bool Won { get; set; }
}

public class CodeExecutor
{
    private readonly Level.Level _level;

    public CodeExecutor(Level.Level level)
    {
        _level = level;
    }

    public List<ExecutionStep> Execute(List<CodeBlock> blocks, Character character)
    {
        var flat = FlattenBlocks(blocks);
        var steps = new List<ExecutionStep>();

        float x = character.X;
        float y = character.Y;

        foreach (var block in flat)
        {
            switch (block.Type)
            {
                case BlockType.Walk:
                    for (int step = 0; step < 2; step++)
                    {
                        float newX = Clamp(x + 1f, 0, _level.Width - 1);
                        int nx = (int)Math.Round(newX), cy = (int)Math.Round(y);
                        if (_level.IsSolid(nx, cy)) break; // wall in the way
                        x = newX;
                    }
                    ApplyGravity(ref x, ref y);
                    steps.Add(MakeStep(x, y));
                    if (steps[^1].Died || steps[^1].Won) return steps;
                    break;

                case BlockType.Jump:
                    var arc = JumpArc(x, y);
                    foreach (var s in arc)
                    {
                        steps.Add(s);
                        if (s.Died || s.Won) return steps;
                    }
                    x = steps[^1].X;
                    y = steps[^1].Y;
                    break;

                case BlockType.Pause:
                    steps.Add(new ExecutionStep { X = x, Y = y });
                    break;
            }
        }

        return steps;
    }

    // Arc: 7 tiles right, rises 3 then falls 3 — clears a 5-tile gap with margin
    private List<ExecutionStep> JumpArc(float startX, float startY)
    {
        var steps = new List<ExecutionStep>();
        float[] dxs = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        float[] dys = { -1f, -1f, -1f, 0f, 1f, 1f, 1f };

        float x = startX, y = startY;
        for (int i = 0; i < dxs.Length; i++)
        {
            float newX = Clamp(x + dxs[i], 0, _level.Width - 1);
            float newY = Clamp(y + dys[i], 0, _level.Height - 1);
            int tx = (int)Math.Round(newX), ty = (int)Math.Round(newY);

            if (_level.IsSolid(tx, ty))
            {
                // Blocked mid-arc — settle from current position
                ApplyGravity(ref x, ref y);
                steps.Add(MakeStep(x, y));
                return steps;
            }

            x = newX;
            y = newY;

            if (_level.IsSpike(tx, ty))
            {
                steps.Add(new ExecutionStep { X = x, Y = y, Died = true });
                return steps;
            }
            if (IsWin(tx, ty))
            {
                steps.Add(new ExecutionStep { X = x, Y = y, Won = true });
                return steps;
            }
            steps.Add(new ExecutionStep { X = x, Y = y });
        }

        // Settle onto platform after arc
        ApplyGravity(ref x, ref y);
        steps.Add(MakeStep(x, y));
        return steps;
    }

    private void ApplyGravity(ref float x, ref float y)
    {
        while (true)
        {
            int tx = (int)Math.Round(x);
            int ty = (int)Math.Round(y);

            if (_level.IsSolid(tx, ty))
                break;

            if (_level.IsSpike(tx, ty))
                break; // already on a spike, MakeStep will catch it

            if (ty >= _level.Height - 1)
                break;

            if (_level.IsSolid(tx, ty + 1))
                break; // one tile above solid ground — stop here

            if (_level.IsSpike(tx, ty + 1))
            {
                y += 1f; // fall into the spike
                break;
            }

            y += 1f;
        }
    }

    private ExecutionStep MakeStep(float x, float y)
    {
        int tx = (int)Math.Round(x);
        int ty = (int)Math.Round(y);

        bool died = _level.IsSpike(tx, ty);
        bool won  = IsWin(tx, ty);
        return new ExecutionStep { X = x, Y = y, Died = died, Won = won };
    }

    // Win when standing on or directly above a checkpoint tile
    private bool IsWin(int tx, int ty) =>
        _level.IsCheckpoint(tx, ty) ||
        (ty + 1 < _level.Height && _level.IsCheckpoint(tx, ty + 1));

    private float Clamp(float v, float min, float max) =>
        v < min ? min : v > max ? max : v;

    private List<CodeBlock> FlattenBlocks(List<CodeBlock> blocks)
    {
        var result = new List<CodeBlock>();
        FlattenInto(blocks, result);
        return result;
    }

    private void FlattenInto(List<CodeBlock> blocks, List<CodeBlock> result)
    {
        foreach (var block in blocks)
        {
            if (block.Type == BlockType.Repeat)
            {
                for (int r = 0; r < block.RepeatCount; r++)
                    FlattenInto(block.Children, result);
            }
            else
            {
                result.Add(block);
            }
        }
    }
}
