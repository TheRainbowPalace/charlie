using charlie.Graphical;
using charlie.Shell;
using Gtk;

namespace charlie
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Application.Init();
                var app = new GraphicalApp();
                Application.Run();
            }
            else ShellApp.Run(args);
        }
    }
}
