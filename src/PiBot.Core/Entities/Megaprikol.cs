namespace PiBot.Core.Entities;

public class Megaprikol : BaseEntity
{
    public string Content { get; private set; } = "";
    public int PingsCount { get; private set; }
    public int UsageCount { get; private set; }

    public void Use() => ++UsageCount;
}
