using mlCommand;
using mlStringValidation.Path;
using mlStringValidation.Regex;

internal static partial class Program
{
    public static void HandleRecoverFile(ParsedCommand cmd)
    {
        if (!cmd.HasArgument("pattern"))
        {
            Exit(ProgramExitCodes.BadCommandLine, "You must specifiy a pattern to use to search for files, see help.");
            return;
        }

        if (!cmd.HasSwitch("output"))
        {
            Exit(ProgramExitCodes.BadCommandLine, "Please use --output to specifiy the output location for the recovered file.");
            return;
        }

        //check destination

        PathBase dest;

        try { dest = PathBase.FindCommandPath(WorkingDir, cmd.GetSwitch("output").GetArgumentValue("dest").First()); }
        catch (PathException e)
        {
            Exit(ProgramExitCodes.BadCommandLine, e.Message);
            return;
        }

        if (dest.Exists)
        {
            if (dest.IsDir)
            {
                Exit(ProgramExitCodes.BadCommandLine, $"Cannot output file to {dest}, dir already exists; please specify a file path.");
                return;
            }
            else if (dest.IsFile)
            {
                if (CommandHelper.AskYesNoQuestion($"Are you sure you want to override the file at '{dest}' ?") == CommandAnswer.No) { return; }
            }

            if (!dest.Access.HasFlag(AccessLevel.Write))
            {
                Exit(ProgramExitCodes.IOError, $"Cannot output file to {dest}, no permission to write.");
                return;
            }
        }
        else
        {
            if (!dest.GetContainingDir().Access.HasFlag(AccessLevel.Write))
            {
                Exit(ProgramExitCodes.IOError, $"Cannot output file to {dest}, no permission to write.");
                return;
            }
        }

        //find file

        var wildcard = new WildcardRegexBuilder(cmd.GetArgumentValue("pattern").First());

        var searchResults = Stack.FindFile(wildcard.Regex, false);
        Stream stream;

        if (searchResults.Count == 0)
        {
            Exit(ProgramExitCodes.IOError, $"Could not find a file matching the pattern '{cmd.GetArgumentValue("pattern").First()}' in {StackPath}.");
            return;
        }
        else if (searchResults.Count == 1)
        {
            stream = dest.OpenStream(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Stack.RetriveFile(searchResults[0].BulkID, stream);
            stream.Flush();
            return;
        }

        var levels = GetSortedLevels();
        var levelIDs = new List<string>();

        foreach (var fsr in searchResults)
        {
            if (!levelIDs.Contains(fsr.LevelID))
            {
                levelIDs.Add(fsr.LevelID);
            }
        }

        string levelID;

        if (cmd.HasSwitch("level"))
        {
            levelID = cmd.GetSwitch("level").GetArgumentValue("levelID").First().ToLower();

            if (!levels.Any((li) => li.ID.Equals(levelID)))
            {
                Exit(ProgramExitCodes.IOError, $"The level ID provided ({levelID}) is not valid - no such level.");
                return;
            }
            else if (!levelIDs.Contains(levelID))
            {
                Exit(ProgramExitCodes.IOError, $"The level ID provided ({levelID}) does not contain a file matching the pattern provided - but another level does.");
                return;
            }
        }
        else
        {
            if (levelIDs.Count == 1)
            {
                levelID = levelIDs[0];
            }
            else
            {
                int index = CommandHelper.AskListQuestion("The pattern provided matches files found on multiple levels, which level would you like to retrive from?", levels.Where((li) => levelIDs.Contains(li.ID)));

                levelID = levelIDs[index];
            }
        }

        searchResults = searchResults.Where((fsr) => fsr.LevelID.Equals(levelID)).ToList();

        if (searchResults.Count == 1)
        {
            stream = dest.OpenStream(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Stack.RetriveFile(searchResults[0].BulkID, stream);
            stream.Flush();
            return;
        }
        else
        {
            int index = CommandHelper.AskListQuestion("The pattern provided matches multiple files, which file woild you like to retrive?", searchResults);

            stream = dest.OpenStream(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Stack.RetriveFile(searchResults[index].BulkID, stream);
            stream.Flush();
            return;
        }
    }
}