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
        public async Task RunAsync()
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine("Type command to start. Type \"Help\" for helps.");
            ResetColor();
            Type type = GetType();
            MethodInfo[] methods = type.GetMethods().Where(IsCommand).ToArray();
            while (true)
            {
                string[] commandParams = ReadLine().ToLower().Split(' ');
                MethodInfo command = methods.FirstOrDefault(method => 
                        method.Name.ToLower() == commandParams[0]
                        && method.GetParameters().Length == commandParams.Length - 1);

                if (command == null)
                {
                    WriteLine("No command {0}", commandParams[0]);
                    continue;
                }
                if (command.ReturnType == typeof(Task))
                {
                    await (Task)command.Invoke(this, commandParams.Skip(1).ToArray());
                }
                else
                {
                    command.Invoke(this, commandParams.Skip(1).ToArray());
                }
            }
        }

        static bool IsCommand(MethodInfo method)
        {
            return !method.IsStatic && method.Name != nameof(RunAsync)
                && typeof(object).GetMethods().All(om => om.Name != method.Name)
                && method.GetParameters().All(p => p.ParameterType == typeof(string));
        }

        [Helper("Print Help")]
        public Task Help()
        {
            Type type = GetType();
            MethodInfo[] methods = type.GetMethods().Where(IsCommand).ToArray();
            foreach (var method in methods)
            {
                ForegroundColor = ConsoleColor.Cyan;
                Write(method.Name);
                ResetColor();
                HelperAttribute helper = method.GetCustomAttribute<HelperAttribute>();
                if (helper != null)
                {
                    Write(": ");
                    WriteLine(helper.HelpText ?? string.Empty);
                }
                ParameterInfo[] parameters = method.GetParameters();
                IEnumerable<ArgumentAttribute> argumentHelpers = method.GetCustomAttributes<ArgumentAttribute>();
                int count = 1;
                foreach (ArgumentAttribute ah in argumentHelpers)
                {
                    if (parameters.Length < count)
                    {
                        break;
                    }
                    WriteLine("   arg{0} {1}: {2}", count, parameters[count - 1].Name, ah.HelpText);
                    count++;
                }
            }
            return Task.CompletedTask;
        }
    }

    public class HelperAttribute: Attribute
    {
        public string HelpText { get; set; }

        public HelperAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ArgumentAttribute : Attribute
    {
        public string HelpText { get; set; }

        public ArgumentAttribute(string helpText)
        {
            HelpText = helpText;
        }
    }
}
