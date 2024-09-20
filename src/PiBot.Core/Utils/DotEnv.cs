namespace PiBot.Core.Utils;

public static class DotEnv
{
    public static bool TryLoad(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split(
                '=',
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                continue;

            Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }

        return true;
    }
}