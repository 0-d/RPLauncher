using clipr;
using System;
using System.Text.RegularExpressions;

namespace RPLauncher
{
    [ApplicationInfo(Description = "This is a set of options.")]
    public class Options
    {
        [NamedArgument('c', "command", Action = ParseAction.Store,
            Description = "Command to execute.", Required = true)]
        public string Command { get; set; }

        [NamedArgument('s', "server", Action = ParseAction.Store,
            Description = "Remote computer name.")]
        public string Server { get; set; }

        [NamedArgument('t', "timeout", Action = ParseAction.Store,
            Description = "Timeout in seconds.")]
        public int Timeout { get; set; }
    }

    class RPLauncher
    {
        public static string commandLine;
        public static string remoteMachine = ".";
        public static int timeout = 300 * 1000;

        static void Main(string[] args)
        {
            try
            {
                ParseArgs(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            Console.WriteLine("\r\n\r\n" + commandLine + "\r\n\r\n");
            
            ProcessWMI p = new ProcessWMI();
            p.ExecuteRemoteProcessWMI(remoteMachine, commandLine, timeout);
            Environment.ExitCode = p.ExitCode;

            Environment.Exit(Environment.ExitCode);
        }

        static void ParseArgs(string[] ArgumentList)
        {
            var opt = CliParser.Parse<Options>(ArgumentList);

            if (string.IsNullOrEmpty(opt.Command))
            {
                Console.WriteLine("Command not provided! Please use --help for usage information.");
                Environment.Exit(1);
            }

            commandLine = opt.Command;

            if (!string.IsNullOrEmpty(opt.Server))
                remoteMachine = opt.Server;
            else
                Console.WriteLine("WARNING: ComputerName not provided! Running on local machine...");

            if (opt.Timeout > 0)
                timeout = opt.Timeout * 1000;

            commandLine = Regex.Replace(commandLine, @"^cmd\s/c\s", string.Empty);
        }
    }
}
