﻿using System;
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
        public async Task RunAsync()
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Commander is running. Type \"Help\" for help of available commands.");
            ResetColor();
            MethodInfo[] methods = this.GetAllCommands();
            while (true)
            {
                Write(">> ");
                string[] commandParams = ReadLine().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                MethodInfo command = methods.FirstOrDefault(method => 
                        method.Name.ToLower() == commandParams[0]
                        && method.GetParameters().Length == commandParams.Length - 1);

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

        [CommandHelper("Print this help")]
        public Task Help()
        {
            WriteLine("All available commands:");
            MethodInfo[] methods = this.GetAllCommands();
            foreach (var method in methods)
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
                if (parameterInfos.Length == 0 && parameterHelpers.Count() == 0 && method != methods[^1])
                {
                    WriteLine();
                }
            }
            return Task.CompletedTask;
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
