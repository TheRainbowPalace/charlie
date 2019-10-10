using System;
using Cairo;
using Gdk;
using Gtk;
using Color = Cairo.Color;
using Context = Cairo.Context;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace charlie.gui
{
  public class Label : DrawingArea
  {
    public string Text;

    public Color BackgroundColor = new Color(1, 1, 1);
    public int PaddingLeft = 0;
    public int PaddingTop = 0;
    public int PaddingRight = 0;
    public int PaddingBottom = 0;
    
    public Color Color = new Color(.1, .1, .1);
    
    /// <summary>
    /// [ font-style font-variant font-weight font-size/line-height
    /// font-family ] | caption | icon | menu | message-box |
    /// small-caption | status-bar
    /// </summary>
    public string Font;
    public string FontFamily = "Andale Mono";
    public int FontSize = 12;
    public int FontSizeAdjust;
    public int FontStretch;

    /// <summary>
    /// normal | italic | oblique
    /// </summary>
    public FontSlant FontStyle = FontSlant.Normal;

    /// <summary>
    /// normal | small-caps
    /// </summary>
    public int FontVariant;

    /// <summary>
    /// normal | bold | bolder | lighter |
    /// 100 | 200 | 300 | 400 | 500 | 600 | 700 | 800 | 900
    /// </summary>
    public int FontWeight;

    public Label()
    {
      Events = EventMask.ButtonPressMask | EventMask.KeyPressMask |
               EventMask.ScrollMask;
      CanFocus = true;
      Focused += (o, args) => Console.WriteLine("Focus received");
      ButtonPressEvent += (o, args) =>
      {
        IsFocus = true;
      };
    }
    
    public static void Test()
    {
      Application.Init();
      
      var root = new VBox(false, 0);

      var lbl1 = new Label
      {
        Text = "Hello",
        PaddingLeft = 10
      };

      var lbl2 = new Label
      {
        Text = "In Euclidean space, a Euclidean vector is a geometric " +
               "object that possesses both a magnitude and a direction. " +
               "A vector can be pictured as an arrow. Its magnitude is " +
               "its length, and its direction is the direction that the " +
               "arrow points to. The magnitude of a vector a is denoted " +
               "by ‖a‖. The dot product of two Euclidean vectors a and b " +
               "is defined by[2][3]",
        BackgroundColor = new Color(0, 0, 0),
        Color = new Color(1, 1, 1),
        Margin = 10
      };
      
      root.PackStart(lbl1, false, false, 0);
      root.PackStart(lbl2, false, false, 0);
      
      var window = new Window(WindowType.Toplevel)
      {
        HeightRequest = 200,
        WidthRequest = 200,
        Child = root
      };
      window.Destroyed += (sender, args) => Application.Quit();
      window.ShowAll();
      
      Application.Run();
    }
    
    protected override bool OnDrawn(Context cr)
    {
      cr.Rectangle(0, 0, AllocatedWidth, AllocatedHeight);
      cr.SetSourceColor(BackgroundColor);
      cr.Fill();
      
      cr.SelectFontFace(FontFamily, FontStyle, Cairo.FontWeight.Normal);
      cr.SetFontSize(FontSize);
      cr.SetSourceColor(Color);

      var te = cr.TextExtents(Text);
      var charsPerLine = AllocatedWidth / te.Height;
      var lineCount = Text.Length * charsPerLine;
      var totalHeight = te.Height * lineCount + PaddingBottom + PaddingTop;
      
      if (totalHeight < AllocatedHeight)
      {
//        SetSizeRequest();
      }
      if (te.Width > AllocatedWidth)
      {
        
      }
      
      cr.MoveTo(PaddingLeft, PaddingTop + FontSize);
      cr.ShowText(Text);
//      var g = new Glyph();
      
//      Pango.CairoHelper.ShowGlyphString();
      Console.WriteLine(cr.TextExtents(Text).Width);
      Console.WriteLine(cr.TextExtents(Text).Height);
      
      var fontWidth = cr.GetScaledFont().FontExtents.MaxXAdvance;
      var fontHeight = cr.GetScaledFont().FontExtents.Height;
      
//      7.201171875
//      13.5

//      cr.ShowGlyphs();
//      cr.ShowGlyphs();
      
//      for (var i = 0; i < lineCount; i++)
//      {
//        cr.ShowText(Text.Substring(
//          i * charsPerLine, 
//          (i + 1) * charsPerLine));
//      }
      
      return true;
    }
  }
}