using System;
using System.Collections.Generic;
using Cairo;
using Optimization;

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

    public override string Log()
    {
      return "Radius: " + _radius;
    }
  }

  public class SineExample : AbstractSimulation
  {
    private double _time;
    private double _amplitude;
    private double _wavelength;
    private double _y;
    private List<double> _trail;
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

    public override void Init(Dictionary<string, string> config)
    {
      _wavelength = GetDouble(config, "Wavelength", 200);
      _amplitude = GetDouble(config, "Amplitude", 100);
      _shift = GetInt(config, "Shift", 0);
      _y = 200;
      _time = 0;
      _trailLength = 40;
      _shift = 0;
      _trail = new List<double>(_trailLength) {_y};
    }

    public override void Update(long deltaTime)
    {
      _time += deltaTime / _wavelength;
      _y = 200 + Math.Sin(_time) * _amplitude;
      _trail.Insert(0, _y);
      if (_trail.Count > _trailLength) _trail.RemoveAt(_trailLength - 1);
    }

    public override void Render(Context ctx)
    {
      for (var i = 0; i < _trail.Count; i++)
      {
        ctx.SetSourceRGB(0.769, 0.282, 0.295);
        ctx.Arc(200 + _shift - 10 * i, _trail[i], 
          Math.Log(20.0, i + 2), 0, 2 * Math.PI);
        ctx.ClosePath();
        ctx.Fill();
      }
    }
  }
  
  public class SoccerExample : AbstractSimulation
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
      foreach (var player in _teamA) RenderPlayer(ctx, player, _colorTeamA);
      foreach (var player in _teamB) RenderPlayer(ctx, player, _colorTeamB);
    }
  }

  public class SwarmExample : AbstractSimulation
  {
    private Swarm _swarm;
    private int _xOffset;
    private int _yOffset;
    
    public override string GetTitle()
    {
      return "SWAAARM";
    }

    public override string GetDescr()
    {
      return "Present yourself with a particle swarm and never forget to be" +
             "amazing.";
    }

    public override string GetConfig()
    {
      return "XOffset = 200\n" +
             "YOffset = 200\n";
    }

    public override void Init(Dictionary<string, string> config)
    {
      _xOffset = GetInt(config, "XOffset", 200);
      _yOffset = GetInt(config, "YOffset", 200);
      
      var sp = new SearchSpace(2, 100);
      _swarm = Pso.SwarmSpso2011(sp, OptimizationFct.SphereFct);
      _swarm.Initialize();
    }

    public override void Update(long deltaTime)
    {
      _swarm.IterateOnce();
    }

    public override void Render(Context ctx)
    {
      foreach (var p in _swarm.Particles)
      {
        ctx.SetSourceColor(new Color(0.769, 0.282, 0.295));
        ctx.LineWidth = 1;
        ctx.Arc(
          _xOffset + p.Position[0], 
          _yOffset + p.Position[1], 
          5, 0, 2 * Math.PI);
        ctx.ClosePath();
        ctx.Fill();
      }
      
      var globalBest = "Best: (";
      for (var i = 0; i < _swarm.GlobalBest.Length; i++)
      {
        globalBest += Math.Round(_swarm.GlobalBest[i], 3);
        if (i < _swarm.GlobalBest.Length - 1) globalBest += " ";
      }

      globalBest += ") - " + Math.Round(_swarm.GlobalBestValue, 3);
      
      ctx.SetSourceColor(new Color(.7, .7, .7));
      ctx.SetFontSize(13);
      ctx.MoveTo(12, 20);
      ctx.ShowText(globalBest);
      
      ctx.Rectangle(_xOffset - 100, _xOffset - 100, 200, 200);
      ctx.LineWidth = 1;
      ctx.SetDash(new []{3.0}, 4);
      ctx.Stroke();
    }
  }

  internal class Branch
  {
    private int _maxLength;
    private double[] _cells;
    private double _cellRadius;
    private double _angleStart;
    private double _angleEnd;
    private int _length;
    private double _branchProbability;
    private List<Branch> _branches;
    private PcgRandom _rand;
    
    /// <summary>
    /// Initialize the branch.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="angleStart"></param>
    /// <param name="angleEnd"></param>
    /// <param name="cellRadius"></param>
    /// <param name="maxLength"></param>
    /// <param name="branchProbability"></param>
    public Branch(double x, double y, double angleStart, double angleEnd, 
      double cellRadius, int maxLength, double branchProbability)
    {
      _angleStart = angleStart;
      _angleEnd = angleEnd;
      _cellRadius = cellRadius;
      _maxLength = maxLength;
      _cells = new double[_maxLength * 2];
      _cells[0] = x;
      _cells[1] = y;
      _length = 0;
      _branchProbability = branchProbability;
      _rand = new PcgRandom();
      
      var expectedBranches = (int) Math.Truncate(maxLength * branchProbability);
      _branches = new List<Branch>(expectedBranches);
    }

    public void Update()
    {
      if (++_length > _maxLength - 1) return;
      
      var angle = _rand.GetDouble(_angleStart, _angleEnd);
      angle = angle * 2 * Math.PI - 0.25 * Math.PI;
      var x = Math.Cos(angle) * _cellRadius * 2 + _cells[(_length - 1) * 2];
      var y = Math.Sin(angle) * _cellRadius * 2 + _cells[(_length - 1) * 2 + 1];
      _cells[_length * 2] = x;
      _cells[_length * 2 + 1] = y;

      if (_rand.GetDouble() < _branchProbability)
      {
        _branches.Add(new Branch(x, y, _angleStart, _angleEnd, _cellRadius, 
          _maxLength, 0));
      }
      
      foreach (var branch in _branches) branch.Update();
    }
    
    public void Render(Context ctx, int offsetX, int offsetY)
    {
      for (var i = 0; i < _cells.Length; i += 2)
      {
        ctx.SetSourceRGB(0.556,  0.768, 0.466);
        ctx.Arc(
          offsetX + _cells[i],
          offsetY + _cells[i + 1],
          _cellRadius,
          0, 2 * Math.PI);
        ctx.ClosePath();
        ctx.Fill();
      }

      foreach (var branch in _branches) branch.Render(ctx, offsetX, offsetY);
    }
  }
  
  public class GrowthExample : AbstractSimulation
  {
    private Branch[] _roots;
    
    public override string GetTitle()
    {
      return "Growth";
    }

    public override string GetDescr()
    {
      return "Grow a weird artificial plant";
    }

    public override string GetConfig()
    {
      return "# The maximal angle between two cells\n" +
             "Variety = 0.7\n" +
             "# The size of a single cell\n" +
             "CellRadius = 1\n" +
             "# The maximal size of the plant\n" +
             "MaxLength = 200\n" +
             "# The probability for a branch to sprout\n" +
             "BranchProbability = 0.01\n" +
             "RootCount = 10";
    }

    public override void Init(Dictionary<string, string> config)
    {
      var cellRadius = GetDouble(config, "CellRadius", 1); 
      var maxLength = GetInt(config, "MaxLength", 200); 
      var branchProbability = GetDouble(config, "BranchProbability", 0.01);
      var variety = GetDouble(config, "Variety", 0.7);
      var angleStart = - variety / 2;
      var angleEnd = variety / 2;

      var rootCount = GetInt(config, "RootCount", 10);
      _roots = new Branch[rootCount];
      for (var i = 0; i < rootCount; i++)
      {
        _roots[i] = new Branch(0, 0, angleStart, angleEnd, cellRadius,
          maxLength, branchProbability);
      }
    }

    public override void Update(long deltaTime)
    {
      foreach (var branch in _roots) branch.Update();
    }

    public override void Render(Context ctx)
    {
      foreach (var branch in _roots) branch.Render(ctx, 200, 350);
    }
  }
}