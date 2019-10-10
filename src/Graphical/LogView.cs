using System.Collections.Generic;
using Cairo;
using Gdk;
using Gtk;

namespace charlie.Graphical
{
  internal class LogView : DrawingArea
  {
    public int MaxNumberOfLines = 100;
    public int FontSize = 10;
    public int Padding = 3;
    private double _scrollX = 0;
    private double _scrollY = 0;
    private List<string> _lines = new List<string>();

    public LogView()
    {
      Events = EventMask.ButtonPressMask | EventMask.KeyPressMask |
               EventMask.ScrollMask;
      CanFocus = true;
      Focused += (o, args) => System.Console.WriteLine("Focus received");
      ButtonPressEvent += (o, args) =>
      {
        IsFocus = true;
//        Console.WriteLine(args.Event.X + " " + args.Event.Y);
      };
//      KeyPressEvent += (o, args) => Console.WriteLine("KP: " + args.Event.Key);
//      KeyReleaseEvent += (o, args) => Console.WriteLine("KR: " + args.Event.Key);
//      ScrollEvent += (o, args) =>
//      {
//        _scrollY += args.Event.Direction == ScrollDirection.Down
//          ? args.Event.DeltaY : -args.Event.DeltaY;
//        if (_scrollY < 0) _scrollY = 0;
//        else if (_scrollY > MaxNumberOfLines)
//          _scrollY = MaxNumberOfLines;
//        Console.WriteLine("Scroll: " + _scrollY);
//      };
    }

    public void Log(string message)
    {
      if (string.IsNullOrEmpty(message)) return;
      if (_lines.Count == 0) _lines.Add("");
      
      var split = message.Split('\n');
      _lines[_lines.Count - 1] += split[0];
      for (var i = 1; i < split.Length; i++)
      {
        _lines.Add(split[i]);
        if (_lines.Count > MaxNumberOfLines) _lines.Remove(_lines[0]);
      }
      QueueDraw();
    }

    public void Clear()
    {
      _lines.Clear();
      QueueDraw();
    }
    
    protected override bool OnDrawn(Context ctx)
    {
      ctx.SelectFontFace("Andale Mono", FontSlant.Normal, FontWeight.Normal);
      ctx.SetFontSize(FontSize);
      ctx.SetSourceRGB(.65, .65, .65);
      
      var lineHeight = FontSize + Padding;
      var visibleLines = AllocatedHeight / lineHeight;
      if (visibleLines > MaxNumberOfLines) visibleLines = MaxNumberOfLines;
      for (var i = 1; i <= visibleLines; i++)
      {
        ctx.MoveTo(0, AllocatedHeight - i * lineHeight);
        ctx.ShowText(i - 1 < _lines.Count ? _lines[_lines.Count - i] : ".");
      }
      
      return true;
    }
  }
}