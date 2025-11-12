using mlCommand;

internal static partial class Program
{
    public static void HandlePrune(ParsedCommand cmd)
    {
        if (!cmd.TryGetArgumentValue("keep-levels", out IEnumerable<string> args))
        {
            Exit(ProgramExitCodes.BadCommandLine, "You must specifiy the number of levels to keep, please see help.");
            return;
        }

        var levels = GetSortedLevels();
        bool hasYes = cmd.HasSwitch("yes");

        if (int.TryParse(args.First(), out int keepLevels) && keepLevels >= 0 && keepLevels <= levels.Count)
        {
            if (keepLevels == 0 && !hasYes)
            {
                var ans = CommandHelper.AskYesNoQuestion("Are you sure that you want to delete all levels in the stack? Use the switch --yes to answer this question by default.");

                if (ans == CommandAnswer.No) { return; }
            }
            else if (!hasYes)
            {
                var ans = CommandHelper.AskYesNoQuestion($"Are you sure that you want to delete {levels.Count - keepLevels} levels from this stack? Use the switch --yes to answer this question by default.");

                if (ans == CommandAnswer.No) { return; }
            }

            for (int x = keepLevels; x < levels.Count; x++)
            {
                Stack.DeleteLevel(levels[x].ID);
            }

            Stack.PruneBulk();
        }
        else
        {
            Exit(ProgramExitCodes.BadCommandLine, "The number of levels provided is either invalid or is greater than the current number of levels in the stack: " + levels.Count);
            return;
        }
    }
}