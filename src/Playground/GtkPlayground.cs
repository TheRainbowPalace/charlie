using System;
using System.IO;
using Gdk;
using Gtk;
using Application = Gtk.Application;
using Window = Gtk.Window;

namespace charlie.Playground
{
  public class PlaygroundApp : Window
  {
//    [Builder.Object]
//    private Button SendButton;
//
//    [Builder.Object]
//    private Entry StdInputTxt;

    public PlaygroundApp(IntPtr raw) : base(raw)
    {}

    public void Init()
    {
      DeleteEvent += (o, args) => Application.Quit();
//      SendButton.Clicked += (o, args) => StdInputTxt.Text = "Hello World";
    }
  }
  
  public class GtkPlayground
  {
    public static string GetResource(string resourceName)
    {
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "resources", resourceName);
    }
    
    public static void Play()
    {
      Application.Init();
      
      var provider = new CssProvider();
      provider.LoadFromPath(GetResource("playground.css"));
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);

      var buffer = File.ReadAllText(GetResource("playground.glade"));
      var builder = new Builder();
      builder.AddFromString(buffer);

      var window = (ApplicationWindow)builder.GetObject("window");
      var root = (Box)builder.GetObject("root");
      var leftArea = (Box)builder.GetObject("left-area");
      var rightArea = (Box)builder.GetObject("right-area");

      root.SizeAllocated += (o, args) =>
      {
        leftArea.Visible = args.Allocation.Width >= 700;
      };
      
      var app = new PlaygroundApp(window.Handle);
      builder.Autoconnect(app);
      
//      var scrollArea = (Box)builder.GetObject("scrolled-window");

      app.Init();
      app.Show();
      
      Application.Run();
    }
  }
}