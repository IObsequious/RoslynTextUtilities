using System.Collections.Generic;
using System.Text;

namespace Roslyn.Utilities
{
    public static class CommandLineUtilities
    {
        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
        {
            char? unused;
            return SplitCommandLineIntoArguments(commandLine, removeHashComments, out unused);
        }

        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, out char? illegalChar)
        {
            StringBuilder builder = new StringBuilder(commandLine.Length);
            List<string> list = new List<string>();
            int i = 0;
            illegalChar = null;
            while (i < commandLine.Length)
            {
                while (i < commandLine.Length && char.IsWhiteSpace(commandLine[i]))
                {
                    i++;
                }

                if (i == commandLine.Length)
                {
                    break;
                }

                if (commandLine[i] == '#' && removeHashComments)
                {
                    break;
                }

                int quoteCount = 0;
                builder.Length = 0;
                while (i < commandLine.Length && (!char.IsWhiteSpace(commandLine[i]) || quoteCount % 2 != 0))
                {
                    char current = commandLine[i];
                    switch (current)
                    {
                        case '\\':
                        {
                            int slashCount = 0;
                            do
                            {
                                builder.Append(commandLine[i]);
                                i++;
                                slashCount++;
                            }
                            while (i < commandLine.Length && commandLine[i] == '\\');

                            if (i >= commandLine.Length || commandLine[i] != '"')
                            {
                                break;
                            }

                            if (slashCount % 2 == 0)
                            {
                                quoteCount++;
                            }

                            builder.Append('"');
                            i++;
                            break;
                        }
                        case '"':
                            builder.Append(current);
                            quoteCount++;
                            i++;
                            break;
                        default:
                            if ((current >= 0x1 && current <= 0x1f) || current == '|')
                            {
                                if (illegalChar == null)
                                {
                                    illegalChar = current;
                                }
                            }
                            else
                            {
                                builder.Append(current);
                            }

                            i++;
                            break;
                    }
                }

                if (quoteCount == 2 && builder[0] == '"' && builder[builder.Length - 1] == '"')
                {
                    builder.Remove(0, 1);
                    builder.Remove(builder.Length - 1, 1);
                }

                if (builder.Length > 0)
                {
                    list.Add(builder.ToString());
                }
            }

            return list;
        }
    }
}
