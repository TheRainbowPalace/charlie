using System.Collections.Generic;
using Cairo;

namespace run_charlie
{
  public interface ISimulation
  {
    string GetTitle();
    string GetDescr();
    string GetConfig();
    void Init(Dictionary<string, string> config);
    void Update(long deltaTime);
    byte[] Render(int width, int height);
    string Log();
  }
  
  public abstract class AbstractSimulation : ISimulation
  {
    private ImageSurface _surface;
    private Context _ctx;
    public abstract string GetTitle();
    public abstract string GetDescr();
    public abstract string GetConfig();

    public abstract void Init(Dictionary<string, string> config);
    public abstract void Update(long deltaTime);
    public abstract void Render(Context ctx);
    
    public virtual byte[] Render(int width, int height)
    {
      if (_surface == null)
      {
        _surface = new ImageSurface(Format.ARGB32, width, height);
        _ctx = new Context(_surface);
      }
      else if (_surface.Width != width || _surface.Height != height)
      {
        _surface.Dispose();
        _ctx.Dispose();
        _surface = new ImageSurface(Format.ARGB32, width, height);
        _ctx = new Context(_surface);
      }
      _ctx.Save();
      _ctx.Operator = Operator.Clear;
      _ctx.Paint();
      _ctx.Restore();
      
      Render(_ctx);
      return _surface.Data;
    }

    public string Log()
    {
      return null;
    }
  }
}