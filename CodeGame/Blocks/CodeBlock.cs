namespace CodeGame.Blocks;

public class CodeBlock
{
    public BlockType Type { get; set; }
    public int RepeatCount { get; set; } = 1; // only used for Repeat blocks

    public CodeBlock(BlockType type, int repeatCount = 1)
    {
        Type = type;
        RepeatCount = repeatCount;
    }

    public string DisplayName => Type switch
    {
        BlockType.Walk   => "Walk",
        BlockType.Jump   => "Jump",
        BlockType.Repeat => $"Repeat x{RepeatCount}",
        BlockType.End    => "End",
        BlockType.Pause  => "Pause",
        _                => Type.ToString()
    };

    public string Color => Type switch
    {
        BlockType.Walk   => "#4FC3F7",
        BlockType.Jump   => "#81C784",
        BlockType.Repeat => "#FFB74D",
        BlockType.End    => "#FFB74D",
        BlockType.Pause  => "#CE93D8",
        _                => "#FFFFFF"
    };
}
