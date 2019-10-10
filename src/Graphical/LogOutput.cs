using Gtk;

namespace charlie.Graphical
{
  internal class LogOutput : Box
  {
    private LogView _logView;
    
    public LogOutput() : base(Orientation.Vertical, 0)
    {
      _logView = new LogView {HeightRequest = 150};
      _logView.ShowAll();

      var title = new Label("Log output");
      var clearBtn = new Button("clear");
      clearBtn.Clicked += (sender, args) => _logView.Clear();
      var hideBtn = new Button("show");
      
      var titleBar = new HBox(false, 0) {Name = "titleBar"};
      titleBar.PackStart(title, false, false, 0);
      titleBar.PackEnd(hideBtn, false, false, 0);
      titleBar.PackEnd(clearBtn, false, false, 0);
      
      var visible = false;
      hideBtn.Clicked += (sender, args) =>
      {
        visible = !visible;
        if (visible)
        {
          PackStart(_logView, true, true, 0);
          hideBtn.Label = "hide";
        }
        else
        {
          Remove(_logView);
          hideBtn.Label = "show";
        }
      };

      PackStart(titleBar, false, false, 0);
      
      Name = "logOutput";
    }

    public void Log(string message)
    {
      _logView.Log(message);
    }
  }
}