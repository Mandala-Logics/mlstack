using mlCommand;
using mlLinuxPath;
using mlStackLib;
using mlStringValidation.Path;

internal static partial class Program
{
    public static PathBase StackPath { get; private set; }

    public static void OpenStack(ParsedCommand cmd)
    {
        PathBase path;
        AccessLevel access;

        if (!cmd.TryGetArgumentValue("stack", out _))
        {
            Exit(ProgramExitCodes.BadCommandLine, $"You must specifiy the stack to work on, see help.");
            return;
        }

        try { path = PathBase.FindCommandPath(WorkingDir, cmd.GetArgumentValue("stack").First()); }
        catch (PathException e)
        {
            Exit(ProgramExitCodes.BadCommandLine, e.Message);
            return;
        }

        if (cmd.HasSwitch("new") || cmd.Command == "init")
        {
            if (path.Exists)
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Cannot create new stack at supplied path ({path.Path}), file already exists.");
                return;
            }

            access = path.GetContainingDir().Access;

            if (!access.HasFlag(AccessLevel.Write))
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Cannot create new stack at supplied path ({path.Path}), no permission to create file.");
                return;
            }

            Stack = FileStack.CreateEmptyStack(path, FileMode.CreateNew);

            Stack.Metadata.Add(new StackMetadata(""));
        }
        else
        {
            if (!path.Exists)
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Cannot find the specified file ({path.Path}), use --new to create a new stack.");
                return;
            }
            else if (!path.IsFile)
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Supplied path ({path.Path}) for the stack cannot point to a dir.");
                return;
            }

            access = path.GetContainingDir().Access;

            if (!access.HasFlag(AccessLevel.Write))
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Cannot open stack at supplied path ({path.Path}), no permission to write to file.");
                return;
            }

            try { Stack = FileStack.OpenStack(path); }
            catch (ArgumentException e)
            {
                //Logger.LogException(e, LogLevel.Fatal);
                Exit(ProgramExitCodes.BadCommandLine, $"Supplied path ({path.Path}) does not appear to point to a valid stack.");
                return;
            }

            if (Stack.Metadata.Count != 1 || Stack.Metadata[0] is not StackMetadata)
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Supplied path ({path.Path}) does not appear to point to a valid stack.");
                return;
            }
        }

        StackPath = path;
    }
}