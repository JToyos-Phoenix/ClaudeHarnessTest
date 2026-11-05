using System.Text;
using NotesCli;

Console.OutputEncoding = Encoding.UTF8;
return Cli.Run(args, Console.Out, Console.Error);
