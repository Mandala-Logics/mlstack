using mlCommand;
using mlLinuxPath;
using mlStringValidation.Path;

internal static partial class Program
{
    public static void HandleStackCommand(ParsedCommand cmd)
    {
        PathBase path;

        try { path = PathBase.FindCommandPath(WorkingDir, cmd.GetArgumentValue("src").First()); }
        catch (PathException e)
        {
            Exit(ProgramExitCodes.BadCommandLine, e.Message);
            return;
        }

        if (!path.IsDir)
        {
            Exit(ProgramExitCodes.BadCommandLine, $"Cannot stack the provided path ({path.Path}), path must point to a dir.");
            return;
        }
        
        // check that the path is equal to the previous path

        if (!cmd.HasSwitch("force"))
        {
            var smd = (StackMetadata)Stack.Metadata[0];

            if (!string.IsNullOrEmpty(smd.LastStackedDir))
            {
                var prevPath = new LinuxPath(smd.LastStackedDir);
    
                if (prevPath != path)
                {
                    Exit(ProgramExitCodes.BadCommandLine, $"The path provided to stack ({path.Path}), does not match the previous path used to stack ({prevPath.Path}), use --force to bypass this check.");
                    return;
                }
            }
        }
        
        mlStringValidation.Path.ObjectTreeNode<PathBase> tree;

        try { tree = path.Tree(); }
        catch (PathAccessException e)
        {
            Exit(ProgramExitCodes.IOError, e.Message);
            return;
        }

        var ls = new List<PathBase>();

        foreach (var node in tree)
        {
            var file = node.Value;

            if (!file.IsFile) { continue; }

            var access = file.Access;

            if (!access.HasFlag(AccessLevel.Read))
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Unable to read from file ({file.Path}), cannot stack.");
                return;
            }

            ls.Add(file);
        }

        if (ls.Count == 0)
        {
            Exit(ProgramExitCodes.IOError, $"Supplied path to stack ({path}) does not contain any files, cannot stack.");
            return;
        }

        try { Stack.CreateLevel(ls, new LevelMetadata(path.Path), Logger); }
        catch (PathAccessException e)
        {
            Exit(ProgramExitCodes.IOError, e.Message);
            return;
        }

        Stack.Metadata.Set(0, new StackMetadata(path.Path));
    }
}