using System.Security.Cryptography.X509Certificates;
using mlCommand;

internal static partial class Program
{
    public static void HandleListLevels(ParsedCommand cmd)
    {
        var levels = GetSortedLevels();

        if (levels.Count == 0)
        {
            Console.WriteLine("No levels to show.");
        }
        else
        {
            Console.WriteLine($"Stack: {StackPath.Path}");
            Console.WriteLine($"{levels.Count} level(s):");
            Console.WriteLine();

            Console.WriteLine("ID\tDate\t\t\tFile Count\tOriginal Path");

            foreach (var li in levels)
            {
                Console.WriteLine($"{li.ID}\t{li.TimeSaved}\t{li.Count}\t\t{((LevelMetadata)li.Metadata).OriginalDirPath}");
            }

            Console.WriteLine();
        }
    }
}