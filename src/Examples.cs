using System;
using System.Collections.Generic;
using Cairo;
using Geometry;
using Optimization;
using charlie;

namespace charlie
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
      return "Introduction";
    }

    public override string GetMeta()
    {
      return "Author: Jakob Rieke; Version: 1.0.0";
    }

    public override string GetDescr()
    {
      return
        "Charlie is multi purpose simulation app. It's goal is to provide a " +
        "clean and simple API as well as tools to debug, run and manage " +
        "your simulations." +
        "\nThis is the default demo simulation to give you a brief " +
        "introduction.\n" +
        "\n\nA simulation is made up of 9 functions:\n" +
        "- GetTitle() : string\n" +
        "- GetDescr() : string\n" +
        "- GetMeta() : string\n" +
        "- GetConfig() : string\n" +
        "- Init(model) : void\n" +
        "- Update(deltaTime) : void\n" +
        "- Render(width, height) : byte[]\n" +
        "- Log() : string\n" +
        "- End() : void\n";
    }

    public override string GetConfig()
    {
      return "# Here you can enter a textual description of the\n" +
             "# simulation model\n" +
             "# Change the values and initialize the simulation to\n" +
             "# see the changes\n" +
             "MinRadius = 0\n" +
             "MaxRadius = 100\n" +
             "GrowRate = 1";
    }

    public override void Init(Dictionary<string, string> model)
    {
      _minRadius = GetInt(model, "MinRadius", 0);
      _maxRadius = GetInt(model, "MaxRadius", 100);
      _growRate = GetDouble(model, "GrowRate", 1);
      _radius = _maxRadius;
      _grow = false;
    }

    public override void Update(long deltaTime)
    {
      if (_radius > _maxRadius || _radius < _minRadius) _grow = !_grow;
      if (_grow) _radius += _growRate;
      else _radius -= _growRate;
    }

    public override void Render(Context ctx, int width, int height)
    {
      ctx.SetSourceRGB(0.769, 0.282, 0.295);
      ctx.Arc((double) width / 2, (double) height / 2, 
        _radius, 0, 2 * Math.PI);
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

    public override void Init(Dictionary<string, string> model)
    {
      _wavelength = GetDouble(model, "Wavelength", 200);
      _amplitude = GetDouble(model, "Amplitude", 100);
      _shift = GetInt(model, "Shift", 0);
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

    public override void Render(Context ctx, int width, int height)
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

    public override void Init(Dictionary<string, string> model)
    {
      var cellRadius = GetDouble(model, "CellRadius", 1); 
      var maxLength = GetInt(model, "MaxLength", 200); 
      var branchProbability = GetDouble(model, "BranchProbability", 0.01);
      var variety = GetDouble(model, "Variety", 0.7);
      var angleStart = - variety / 2;
      var angleEnd = variety / 2;

      var rootCount = GetInt(model, "RootCount", 10);
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

    public override void Render(Context ctx, int width, int height)
    {
      foreach (var branch in _roots) branch.Render(ctx, 200, 350);
    }
  }
}

namespace Y
{
  public class AxisBehaviour : AbstractSimulation
  {
    private Color _debugColor;
    private Color _mainColor;
    private Color _wheel1Color;
    private Color _wheel2Color;
    private Color _wheel3Color;

    private Vector2 _reference;
    private Vector2 _center;
    private Vector2 _arm1;
    private Vector2 _arm2;
    private Vector2 _arm3;
    private double _arm1Rot;
    private double _arm2Rot;
    private double _arm3Rot;
    private double _wheelWidth;
    private double _wheelHeight;
    private double _wheelSuspension;
    private Vector2 _wheel1;
    private Vector2 _wheel2;
    private Vector2 _wheel3;
    private double _wheel1Rot;
    private double _wheel2Rot;
    private double _wheel3Rot;
    
    public override string GetTitle()
    {
      return "Three Wheel Behaviour";
    }

    public override string GetDescr()
    {
      return "Simulate the behaviour of three connected wheels";
    }

    public override string GetConfig()
    {
      return "# The length of each arm\n" +
             "ArmLength = 124\n" +
             "# The angle between arm 2 and 3\n" +
             "ArmAngle = 120";
    }

    public override void Init(Dictionary<string, string> model)
    {
      _debugColor = new Color(0, 0, 0);
      _mainColor = new Color(0, 0, 0);
      _wheel1Color = new Color(0.753, 0.274, 0.275);
      _wheel2Color = new Color(0.187, 0.815, 0.909);
      _wheel3Color = new Color(1, 0.861, 0);
      
      _reference = new Vector2(300, 300);
      _center = new Vector2(200, 200);
      var armAngle = GetDouble(model, "ArmAngle", 120);
      _arm1Rot = 180;
      _arm2Rot = -armAngle / 2;
      _arm3Rot = armAngle / 2;
      var armLength = GetDouble(model, "ArmLength", 124);
      _arm1 = _center.Move(_arm1Rot, armLength);
      _arm2 = _center.Move(_arm2Rot, armLength);
      _arm3 = _center.Move(_arm3Rot, armLength);
      
      _wheelWidth = 5.6 * 3;
      _wheelHeight = 21.4 * 3;
      _wheelSuspension = 10;
      _wheel1Rot = 0;
      _wheel2Rot = 0;
      _wheel3Rot = 0;
      _wheel1 = _arm1.Move(_wheel1Rot, _wheelSuspension); 
      _wheel2 = _arm2.Move(_wheel2Rot, _wheelSuspension); 
      _wheel3 = _arm3.Move(_wheel3Rot, _wheelSuspension); 
    }

    public override void Update(long deltaTime)
    {
    }

    private void RenderArm(Context ctx, double x, double y)
    {
      ctx.SetSourceColor(_debugColor);
      ctx.NewPath();
      ctx.MoveTo(_center.X, _center.Y);
      ctx.LineTo(x, y);
      ctx.LineWidth = 5;
      ctx.Stroke();
    }

    private void RenderBody(Context ctx, bool debug = true)
    {
      // Render reference
      ctx.SetSourceColor(_mainColor);
      ctx.Arc(_reference.X, _reference.Y, 5, 0, Math.PI * 2);
      ctx.Fill();
      
      if (debug)
      {
        ctx.SetSourceColor(_debugColor);
        ctx.NewPath();
        ctx.MoveTo(_center.X, _center.Y);
        ctx.LineTo(_arm2.X + 40, _center.Y);
        ctx.LineWidth = .5;
        ctx.Stroke();
        
        ctx.MoveTo(_arm3.X + 40 + 5, _center.Y);
        ctx.ShowText(_arm2Rot + "°");
        ctx.NewPath();
        ctx.Arc(_center.X, _center.Y, 40, 0, -Math.PI * _arm2Rot / 180);
        ctx.Stroke();
      }
      
      RenderArm(ctx, _arm1.X, _arm1.Y);
      RenderArm(ctx, _arm2.X, _arm2.Y);
      RenderArm(ctx, _arm3.X, _arm3.Y);
      
      // Render center
      ctx.SetSourceColor(_mainColor);
      ctx.Arc(_center.X, _center.Y, 9.75, 0, Math.PI * 2);
      ctx.Fill();
      ctx.Arc(_center.X, _center.Y, 13.1, 0, Math.PI * 2);
      ctx.LineWidth = 4;
      ctx.Stroke();
    }
    
    private void RenderWheel(Context ctx, double x, double y, 
      double rot, Color color, bool inverse = false)
    {
      var radius = _wheelHeight / 15.0;
      const double degrees = Math.PI / 180.0;
      y -= _wheelHeight / 2;
      if (inverse) x -= _wheelWidth;

      ctx.NewSubPath();
      ctx.Arc(x + _wheelWidth - radius, y + radius, radius, 
        -90 * degrees, 0 * degrees);
      ctx.Arc(x + _wheelWidth - radius, y + _wheelHeight - radius, 
        radius, 0 * degrees, 90 * degrees);
      ctx.Arc(x + radius, y + _wheelHeight - radius, 
        radius, 90 * degrees, 180 * degrees);
      ctx.Arc(x + radius, y + radius, radius, 
        180 * degrees, 270 * degrees);
      ctx.ClosePath();

      ctx.SetSourceColor(color);
      ctx.FillPreserve();
      ctx.SetSourceRGB(0, 0, 0);
      ctx.LineWidth = 4;
      ctx.Stroke();

      x = inverse ? x + _wheelWidth : x;
      y += _wheelHeight / 2;
      // Render top
      ctx.SetSourceColor(color);
      ctx.LineCap = LineCap.Round;
      ctx.Arc(x, y, 1.95 * 3, 0, Math.PI * 2);
      ctx.FillPreserve();
      ctx.SetSourceColor(_mainColor);
      ctx.Stroke();
      
      // Render wheel suspension
      var k = new Vector2(x, y).Move(rot, _wheelSuspension);
      ctx.NewPath();              
      ctx.MoveTo(x, y);
      ctx.LineTo(k.X, k.Y);
      ctx.SetSourceColor(_mainColor);
      ctx.Stroke();
    }
    
    public override void Render(Context ctx, int width, int height)
    {
      ctx.SetSourceRGB(0.816, 0.816, 0.816);
      ctx.Rectangle(0, 0, width, height);
      ctx.Fill();

      ctx.Translate(0, 100);
      ctx.Scale(1.5, 1.5);
      ctx.SelectFontFace("Andale Mono", FontSlant.Normal, FontWeight.Normal);
      
      RenderWheel(ctx, _arm1.X, _arm1.Y, 0, _wheel1Color, true);
      RenderWheel(ctx, _arm2.X, _arm2.Y, -30, _wheel2Color);
      RenderWheel(ctx, _arm3.X, _arm3.Y, 30, _wheel3Color);
      RenderBody(ctx);
    }
  }
}