using System;
using System.Collections.Generic;
using Cairo;

namespace run_charlie
{
  public class DefaultSimulation : AbstractSimulation
  {
    private int _minRadius;
    private int _maxRadius;
    private double _growRate;
    private double _radius;
    private bool _grow;
    
    public override string GetTitle()
    {
      return "Run Charlie";
    }

    public override string GetDescr()
    {
      return 
        "RunCharlie is multi purpose simulation app. It tries not to apply " +
        "to many rules on how the simulation is run and structured.";
    }

    public override string GetConfig()
    {
      return "# This is an example configuration\n" +
             "# Change the values and initialize the simulation to\n" +
             "# see the changes\n" +
             "MinRadius = 0\n" +
             "MaxRadius = 60\n" +
             "GrowRate = 0.03";
    }

    public override void Init(Dictionary<string, string> config)
    {
      if (config.ContainsKey("MinRadius"))
      {
        _minRadius = int.TryParse(config["MinRadius"], out var minRadius)
          ? minRadius : 0;
      }
      if (config.ContainsKey("MaxRadius"))
      {
        _maxRadius = int.TryParse(config["MaxRadius"], out var maxRadius)
          ? maxRadius : 60;
      }
      if (config.ContainsKey("GrowRate"))
      {
        _growRate = float.TryParse(config["GrowRate"], out var growRate)
          ? growRate : 0.03;
      }
      
      _radius = _maxRadius;
      _grow = false;
    }

    public override void Update(long deltaTime)
    {
      if (_radius > _maxRadius || _radius < _minRadius) _grow = !_grow;
      if (_grow) _radius += _growRate * deltaTime;
      else _radius -= _growRate * deltaTime;
    }

    public override void Render(Context ctx)
    {
      ctx.SetSourceRGB(0.769, 0.282, 0.295);
      ctx.Arc(200, 200, _radius, 0, 2 * Math.PI);
      ctx.ClosePath();
      ctx.Fill();
    }
  }

  public class SineExample : AbstractSimulation
  {
    private double _time;
    private double _amplitude;
    private double _wavelength;
    private double _y;
    private List<double> _trail;
    private double[] _trailBuffer;
    private int _trailLength;
    private int _shift;
    
    public override string GetTitle()
    {
      return "Sine Example";
    }

    public override string GetDescr()
    {
      return "The sine example allows to render and modify a sine wave.";
    }

    public override string GetConfig()
    {
      return "Wavelength = 200\n" +
             "Amplitude = 100\n" +
             "Shift = 0";
    }

    private static double GetDouble(Dictionary<string, string> config,
      string key, double backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return float.TryParse(config[key], out var x) ? x : backup;
    }

    private static int GetInt(Dictionary<string, string> config,
      string key, int backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return int.TryParse(config[key], out var x) ? x : backup;
    }

    public override void Init(Dictionary<string, string> config)
    {
      _y = 200;
      _time = 0;
      _trailLength = 40;
      _shift = 0;
      _trail = new List<double>(_trailLength) {_y};
      _trailBuffer = _trail.ToArray();
      _wavelength = GetDouble(config, "Wavelength", 200);
      _amplitude = GetDouble(config, "Amplitude", 100);
      _shift = GetInt(config, "Shift", 0);
    }

    public override void Update(long deltaTime)
    {
      _time += deltaTime / _wavelength;
      _y = 200 + Math.Sin(_time) * _amplitude;
      if (_trail.Count == _trailLength) _trail.RemoveAt(_trailLength - 1);
      _trail.Insert(0, _y);
      _trailBuffer = _trail.ToArray();
    }

    public override void Render(Context ctx)
    {
      for (var i = 0; i < _trailBuffer.Length; i++)
      {
        ctx.SetSourceRGB(0.769, 0.282, 0.295);
        ctx.Arc(200 + _shift - 10 * i, _trailBuffer[i], 
          Math.Log(20.0, i + 2), 0, 2 * Math.PI);
        ctx.ClosePath();
        ctx.Fill();
      }
    }
  }
  
  public class SoccerSimulation : AbstractSimulation
  {
    // A player is marked as a vector (x, y, rotation, velocity)
    private double[][] _teamA;
    private double[][] _teamB;
    private Color _colorTeamA;
    private Color _colorTeamB;
    private int _sizeTeamA;
    private int _sizeTeamB;
    
    public override string GetTitle()
    {
      return "Soccer Simulation";
    }

    public override string GetDescr()
    {
      return "A tiny soccer simulation.";
    }

    public override string GetConfig()
    {
      return "SizeTeamA = 3\n" +
             "SizeTeamB = 3";
    }

    private static double GetDouble(Dictionary<string, string> config,
      string key, double backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return float.TryParse(config[key], out var x) ? x : backup;
    }

    private static int GetInt(Dictionary<string, string> config,
      string key, int backup)
    {
      if (!config.ContainsKey(key)) return backup;
      return int.TryParse(config[key], out var x) ? x : backup;
    }

    public override void Init(Dictionary<string, string> config)
    {
      _sizeTeamA = 4;
      _sizeTeamB = 4;
      _teamA = new double[_sizeTeamA][];
      _teamB = new double[_sizeTeamB][];
      for (var i = 0; i < _sizeTeamA; i++)
      {
        _teamA[i] = new[] {20, 20 * (i + 1), 1.0, 0};
      }
      for (var i = 0; i < _sizeTeamB; i++)
      {
        _teamB[i] = new[] {380, 20 * (i + 1), 1.0, 0};
      }
      _colorTeamA = new Color(0.769, 0.282, 0.295);
      _colorTeamB = new Color(0.004, 1, 0.854);
    }

    public override void Update(long deltaTime) {}

    private static void RenderPlayer(Context ctx, double[] player, Color color)
    {
      ctx.SetSourceColor(color);
      ctx.LineWidth = 1;
      ctx.Arc(player[0], player[1], 5, 0, 2 * Math.PI);
      ctx.ClosePath();
      ctx.Fill();
      ctx.Arc(player[0], player[1], 7, 0, 2 * Math.PI);
      ctx.ClosePath();
      ctx.Stroke();
    }
    
    public override void Render(Context ctx)
    {
      var surface = new ImageSurface(Format.ARGB32, 400, 400);
      var ct = new Context(surface);
      
      foreach (var player in _teamA) RenderPlayer(ct, player, _colorTeamA);
//      foreach (var player in _teamB) RenderPlayer(ctx, player, _colorTeamB);
      
      var s = new ImageSurface(surface.Data, Format.ARGB32, 400, 400, 1600);
      ctx.SetSourceSurface(s, 0, 0);
      ctx.Paint();
      ct.Dispose();
      surface.Dispose();
      s.Dispose();
    }
  }
  
//  public class Drawing
//  {
//    private void DrawPolygon(Context cr, Polygon p)
//    {
//      if (p.Count == 0) return;
//
//      cr.SetSourceRGBA(0, 0, 0, 0.7);
//      cr.NewPath();
//      cr.MoveTo(p[0].X,
//        p[0].Y);
//
//      for (var i = 1; i < p.Count; i++)
//      {
//        cr.LineTo(p[i].X,
//          p[i].Y);
//      }
//
//      if (p[0] != p[p.Count - 1])
//      {
//        cr.MoveTo(p[0].X,
//          p[0].Y);
//      }
//
//      cr.ClosePath();
//      cr.Fill();
//    }
//
//    private void DrawSegment(Context cr, Segment s)
//    {
//      cr.SetSourceRGBA(1, 1, 1, 0.5);
//      cr.NewPath();
//      cr.MoveTo(s.Start.X, s.Start.Y);
//      cr.LineTo(s.End.X, s.End.Y);
//      cr.ClosePath();
//      cr.Stroke();
//    }
//  }
}