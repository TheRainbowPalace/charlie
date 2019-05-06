using System;
using System.Collections.Generic;
using Cairo;

namespace charlie
{
  public interface ISimulation
  {
    string GetTitle();
    string GetDescr();
    string GetMeta();
    string GetConfig();
    void Init(Dictionary<string, string> model);
    void End();
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

    public virtual string GetMeta()
    {
      return null;
    }

    public static string GetAnyOf(Dictionary<string, string> model,
      string key, List<string> possibleValues, string backup)
    {
      if (!model.TryGetValue(key, out var value)) return backup;
      return possibleValues.Contains(value) ? value : backup;
    }
    
    public static double GetDoubleInRange(Dictionary<string, string> config,
      string key, double min, double max, double backup)
    {
      var value = GetDouble(config, key, backup);
      return value > min && value < max ? value : backup;
    }
    
    public static double GetDouble(Dictionary<string, string> config,
      string key, double backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return float.TryParse(config[key], out var x) ? x : backup;
    }
    
    public static int GetIntInRange(Dictionary<string, string> config,
      string key, int min, int max, int backup)
    {
      var value = GetInt(config, key, backup);
      return value > min && value < max ? value : backup;
    }

    public static int GetInt(Dictionary<string, string> config,
      string key, int backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return int.TryParse(config[key], out var x) ? x : backup;
    }
    
    public abstract void Init(Dictionary<string, string> model);
    
    public void End()
    {
      _surface.Dispose();
      _ctx.Dispose();
    }

    public abstract void Update(long deltaTime);
    public abstract void Render(Context ctx, int width, int height);
    
    public virtual byte[] Render(int width, int height)
    {
      try
      {
        if (_surface != null)
        {
          _surface.Dispose();
          _ctx.Dispose();
        }
        _surface = new ImageSurface(Format.ARGB32, width, height);
        _ctx = new Context(_surface);
        Render(_ctx, width, height);
        return _surface.Data;
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        return null;
      }
    }

    public virtual string Log()
    {
      return null;
    }
  }
}