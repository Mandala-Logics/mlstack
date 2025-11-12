using mlCommand;
using mlStackLib;

internal static partial class Program
{
    public static void HandleDeleteLevel(ParsedCommand cmd)
    {
        LevelInfo level;
        var id = cmd.GetArgumentValue("levelID").First();

        try { level = Stack.GetLevel(id); }
        catch (ArgumentException)
        {
            Exit(ProgramExitCodes.BadCommandLine, $"The level ID specified ({id}) could not be found in the stack; use list-levels to see valid level IDs.");
            return;
        }

        Stack.DeleteLevel(id);
        Stack.PruneBulk();
    }
}