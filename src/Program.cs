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
                var app = new CharlieGtkApp();
                Application.Run();
            }
            else CharlieConsole.Run(args);
        }
    }
}
