using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace Sen.Utilities.Console
{
    public class SimpleCommander : IConsoleCommand
    {
        string quitSecret;
        
        public async Task RunAsync()
        {
            quitSecret = Sen.Utilities.ThreadSafeRandom.Current.Next(1000, 9999).ToString();
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Commander is running. Type \"Help\" for help of available commands.");
            ResetColor();
            MethodInfo[] methods = this.GetAllCommands();
            Regex regex = new Regex("[^\b]\b{1}");
            int length;
            while (true)
            {
                Write(">> ");
                string line = ReadLine();
                do
                {
                    length = line.Length;
                    line = regex.Replace(line, string.Empty);
                }
                while (length != line.Length);
                string[] commandParams = line
                    .ToLower()
                    .Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if  (commandParams.Length == 1 && commandParams[0] == quitSecret)
                {
                    ForegroundColor = ConsoleColor.DarkRed;
                    WriteLine("Closed.");
                    ResetColor();
                    return;
                }
                if (commandParams.Length == 0)
                {
                    continue;
                }
                MethodInfo command = methods.FirstOrDefault(method => 
                        method.Name.ToLower() == commandParams[0]
                        && method.GetParameters().Length == commandParams.Length - 1);
                
                if (TreatControlCAsInput)
                {
                    WriteLine();
                }
                if (command == null)
                {
                    Write("Not recognized command: ");
                    ForegroundColor = ConsoleColor.Cyan;
                    Write(commandParams[0]);
                    ResetColor();
                    WriteLine(" with {0} argument{1}. Type 'help' for more.", commandParams.Length - 1, commandParams.Length == 1 ? string.Empty : "s");
                    continue;
                }
                try
                {
                    if (command.ReturnType == typeof(Task))
                    {
                        await (Task)command.Invoke(this, commandParams.Skip(1).ToArray());
                    }
                    else
                    {
                        command.Invoke(this, commandParams.Skip(1).ToArray());
                    }
                    WriteLine();
                }
                catch (Exception e)
                {
                    ForegroundColor = ConsoleColor.Red;
                    WriteLine(e);
                    WriteLine(e.StackTrace);
                }
                ResetColor();
            }
        }

        [CommandHelper("Print help for all commands")]
        protected void Help()
        {
            WriteLine("All available commands:");
            MethodInfo[] methods = this.GetAllCommands();
            foreach (var method in methods)
            {
                Helper.PrintHelp(method, method == methods[^1]);
            }
        }

        [CommandHelper("Print help for a command")]
        protected void Help([ParameterHelper("Name of command (case-insensitive).")]string command)
        {
            MethodInfo[] methods = this.GetAllCommands().Where(method => method.Name.Equals(command, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (methods.Length == 0)
            {
                Write("No command name " + command);
            }
            else
            {
                foreach (var method in methods)
                {
                    Helper.PrintHelp(method, method == methods[^1]);
                }
            }
        }
    }

    static class Helper
    {
        internal static MethodInfo[] GetAllCommands(this SimpleCommander commander)
        {
            Type type = commander.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(IsCommand).ToArray();
            return methods;
        }

        private static readonly Type objectType = typeof(object);

        private static bool IsCommand(MethodInfo method)
        {
            return method.Name != nameof(SimpleCommander.RunAsync)
                && method.DeclaringType != objectType
                && method.GetParameters().All(p => p.ParameterType == typeof(string));
        }

        internal static void PrintHelp(MethodInfo method, bool lastMethod)
        {
            Write("- ");
            ForegroundColor = ConsoleColor.Cyan;
            Write(method.Name);
            ResetColor();
            CommandHelperAttribute helper = method.GetCustomAttribute<CommandHelperAttribute>();
            if (helper != null)
            {
                Write(": ");
                Write(helper.HelpText ?? string.Empty);
            }
            ParameterInfo[] parameterInfos = method.GetParameters();
            IEnumerable<ParameterHelperAttribute> parameterHelpers = method.GetCustomAttributes<ParameterHelperAttribute>();
            if (parameterHelpers.Count() > 0)
            {
                WriteLine();
                int count = 0;
                foreach (ParameterHelperAttribute ah in parameterHelpers)
                {
                    if (parameterInfos.Length <= count)
                    {
                        break;
                    }
                    ForegroundColor = ConsoleColor.DarkGray;
                    Write("  arg{0}", count);
                    ForegroundColor = ConsoleColor.Magenta;
                    Write(" {0}", parameterInfos[count].Name);
                    ResetColor();
                    if (!string.IsNullOrWhiteSpace(ah.HelpText))
                    {
                        WriteLine(": {0}", ah.HelpText);
                    }
                    count++;
                }
            }
            else if (parameterInfos.Length > 0)
            {
                WriteLine();
                foreach (ParameterInfo parameter in parameterInfos)
                {
                    ParameterHelperAttribute parameterHelper =
                        parameter.GetCustomAttributes<ParameterHelperAttribute>().FirstOrDefault();
                    string helperText = parameterHelper?.HelpText ?? null;
                    ForegroundColor = ConsoleColor.DarkGray;
                    Write("  arg{0}", parameter.Position);
                    ForegroundColor = ConsoleColor.Magenta;
                    Write(" {0}", parameter.Name);
                    ResetColor();
                    if (helperText != null)
                    {
                        WriteLine(": {0}", helperText);
                    }
                    else
                    {
                        WriteLine();
                    }
                }
            }
            if (!lastMethod && parameterInfos.Length == 0 && parameterHelpers.Count() == 0)
            {
                WriteLine();
            }
        }
    }

    public class CommandHelperAttribute: Attribute
    {
        public string HelpText { get; set; }

        public CommandHelperAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
    public class ParameterHelperAttribute : Attribute
    {
        public string HelpText { get; set; }

        public ParameterHelperAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }
}
