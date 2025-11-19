using System.Diagnostics;
using System.Runtime.CompilerServices;
using mlCommand;
using mlLinuxPath;
using mlLogger;
using mlStackLib;
using mlStringValidation.Path;

public enum ProgramExitCodes { Sucesss, UnhandledExpcetion = -1, BadCommandLine = 99, IOError, NameMismatch }

internal static partial class Program
{
    public static Logger Logger { get; } = new();
    public static FileStack Stack { get; private set; }
    public static PathBase WorkingDir { get; }

    static Program()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;

        DDEncoder.DDEncoder.RegisterTypes(typeof(StackMetadata), typeof(LevelMetadata));

        WorkingDir = LinuxPath.GetWorkingDir();
    }

    private static void Main(string[] args)
    {
        if (Debugger.IsAttached)
        {
            args = "list-levels ~/test.stack".Split(' ');
            //args = "init ~/test.stack".Split(' ');
            //args = "stack -n ~/test.stack ~/stacktest".Split(' ');
            //args = "--help".Split(' ');
            //args = "recover-file -o ~/this.txt ~/test.stack 30".Split(' ');
        }
        
        var cmdLineTemplate = new CommandTree(CommandHelper.GetAssemblyStreamReader("cmd.main.template"));

        var cmdLine = new CommandLine(args);

        ParsedCommand cmd;

        try { cmd = cmdLineTemplate.ParseCommandLine(cmdLine); }
        catch (CommandParsingException e) { Exit(ProgramExitCodes.BadCommandLine, e.Message); return; }

        if (cmd.HasSwitch("help"))
        {
            CommandHelper.DisplayAssemblyFile("help.main.txt");
            return;
        }

        OpenStack(cmd.Nested);

        switch (cmd.Nested.Command)
        {
            case "stack":
                HandleStackCommand(cmd.Nested);
                break;
            case "list-levels":
                HandleListLevels(cmd.Nested);
                break;
            case "init":
                Console.WriteLine($"New stack initilised at ({StackPath}).");
                break;
            case "prune":
                HandlePrune(cmd.Nested);
                break;
            case "delete-level":
                HandleDeleteLevel(cmd.Nested);
                break;
            case "recover-file":
                HandleRecoverFile(cmd.Nested);
                break;
            default:
                throw new Exception();
        }

        Stack.Dispose();
    }

    private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;

        Logger.LogException(ex, LogLevel.Fatal);

        Environment.Exit((int)ProgramExitCodes.UnhandledExpcetion);
    }

    public static void Exit(ProgramExitCodes code, string msg)
    {
        if (code == ProgramExitCodes.Sucesss)
        {
            Environment.Exit(0);
        }
        else
        {
            Logger.LogMessage(msg, LogLevel.Fatal);
            Environment.Exit((int)code);
        }
    }
    
    public static List<LevelInfo> GetSortedLevels()
    {
        var ls = Stack.GetAllLevels();

        ls.Sort((li1, li2) =>
        {
            if (li1.TimeSaved > li2.TimeSaved) { return 1; }
            else { return -1; }
        });

        return ls;
    }
}