using System;
using System.Collections.Generic;
using Cairo;
using Geometry;
using charlie;
using static System.Math;

namespace charlie
{
  internal struct AstronomicalObject
  {
    public string Name;
    public double X;
    public double Y;
    public double Phase;
    /// <summary>
    /// The distance to an astronomical object which is used to calculate
    /// the position of this object e.g. the distance from earth to moon.
    /// </summary>
    public double Distance;
    public double Diameter;
    public double RotationSpeed;
  }

  public class HelloWorld : AbstractSimulation
  {
    private AstronomicalObject _sun;
    private AstronomicalObject _moon;
    private AstronomicalObject[] _planets;

    public override string GetTitle()
    {
      return "Hello World";
    }

    public override string GetMeta()
    {
      return "Author: Jakob Rieke; Version: 1.0.0";
    }

    public override string GetDescr()
    {
      return
        "This is a hello world demo for the charlie simulation framework.\n" +
        "Charlie is multi purpose simulation app. It's goal is to provide a " +
        "clean and simple API as well as tools to debug, run and manage " +
        "your simulations.\n" +
        "The framework consists of a definition on how a simulation is " +
        "structured as well as libraries and applications to build and manage" +
        "simulations.\n" +
        "\nA simulation is modeled as a tiny finite state machine made up of " +
        "4 states and 3 transition functions:\n" +
        "* Created\n" +
        "* Initialized\n" +
        "* Updated\n" +
        "* Ended\n" +
        "- Init(model)\n" +
        "- Update(deltaTime)\n" +
        "- End()\n" +
        "This structure can be found in a variety of other simulation " +
        "software and while it gives a clear and simple frame on how to " +
        "build a simulation, it's in no way restrictive \n\n" +
        "After the simulation code has been loaded there are multiple " +
        "functions available to get information about the simulation.\n" +
        "Note here, that the first four are static and can be called before " +
        "the simulation is initialized while the later are dynamic and " +
        "depend on the current state of the simulation.\n" +
        "- GetTitle() : string\n" +
        "- GetDescr() : string\n" +
        "- GetMeta() : string\n" +
        "- GetConfig() : string\n" +
        "- GetTextData() : string\n" +
        "- GetImageData(width, height) : byte[]\n" +
        "- GetAudioData() : byte[]\n" +
        "- GetByteData() : byte[]\n" +
        "- GetState() : byte[]";
    }

    public override string GetConfig()
    {
      return "# Here you can enter a start configuration for\n" +
             "# your simulation. It is loaded as soon as\n" +
             "the simulation is re-initialized\n\n" +
             "MinRadius = 0\n" +
             "MaxRadius = 100\n" +
             "GrowRate = 1";
    }

    public override void Init(Dictionary<string, string> model)
    {
      _sun = new AstronomicalObject
      {
        Name = "Sun",
        Diameter = 1392684
      };
      _moon = new AstronomicalObject 
      {
        Name = "Moon",
        Distance = 20,
        Diameter = 4,
        RotationSpeed = 2.5
      };
      
      var mercury = new AstronomicalObject
      {
        Name = "Mercury",
        Distance = 57909000,
        Diameter = 4879.4,
        RotationSpeed = 4.5
      };
      var venus = new AstronomicalObject
      {
        Name = "Venus",
        Distance = 109160000,
        Diameter = 12103.6,
        RotationSpeed = 1.9
      };
      var earth = new AstronomicalObject
      {
        Name = "Earth",
        Distance = 149600000,
        Diameter = 12756.3,
        RotationSpeed = 1
      };
      var mars = new AstronomicalObject
      {
        Name = "Mars",
        Distance = 227990000,
        Diameter = 6792.4,
        RotationSpeed = 1.0 / 2
      };
      var jupiter = new AstronomicalObject
      {
        Name = "Jupiter",
        Distance = 778360000,
        Diameter = 142984,
        RotationSpeed = 1.0 / 11
      };
      var saturn = new AstronomicalObject
      {
        Name = "Saturn",
        Distance = 1433500000,
        Diameter = 120536,
        RotationSpeed = 1.0 / 13
      };
      var uranus = new AstronomicalObject
      {
        Name = "Uranus",
        Distance = 2872400000,
        Diameter = 51118,
        RotationSpeed = 1.0 / 15
      };
      var neptune = new AstronomicalObject
      {
        Name = "Neptune",
        Distance = 4498400000,
        Diameter = 49528,
        RotationSpeed = 1.0 / 18
      };
      _planets = new []
      {
        mercury, venus, earth, mars, jupiter, saturn, uranus, neptune
      };
      
      for (var i = 0; i < _planets.Length; i++)
      {
        var planet = _planets[i];
        planet.Distance /= 1000000;
        planet.X = planet.Distance;
        planet.Diameter /= 1000;
        _planets[i] = planet;
      }
      
      _sun.Diameter /= 40000;
      _moon.X = earth.X + _moon.Distance;
    }

    private static AstronomicalObject UpdateObject(
      AstronomicalObject target, 
      AstronomicalObject reference)
    {
      target.Phase += target.RotationSpeed;
      if (target.Phase >= 360) target.Phase = 0;
      
      target.X = reference.X + target.Distance * Cos(PI * target.Phase / 180);
      target.Y = reference.Y + target.Distance * Sin(PI * target.Phase / 180);
      return target;
    }
    
    public override void Update(long deltaTime)
    {
      for (var i = 0; i < _planets.Length; i++)
      {
        _planets[i] = UpdateObject(_planets[i], _sun);
      }
      
      _moon = UpdateObject(_moon, _planets[2]);
    }

    private static void RenderObject(Context ctx, AstronomicalObject obj, 
      double xOffset, double yOffset)
    {
      ctx.SetSourceRGB(0.769, 0.282, 0.295);
      ctx.Arc(xOffset + obj.X, yOffset + obj.Y, obj.Diameter, 0, 2 * PI);
      ctx.ClosePath();
      ctx.Fill();
    }
    
    public override void Render(Context ctx, int width, int height)
    {
      var xOffset = width / 2;
      var yOffset = height / 2;
      const double scale = 40.0 / 1392684;

//      ctx.Scale(scale, scale);
//      ctx.Translate(width * .5, height * .5);
      
      RenderObject(ctx, _sun, xOffset, yOffset);
      RenderObject(ctx, _moon, xOffset, yOffset);
      
      foreach (var planet in _planets)
      {
        RenderObject(ctx, planet, xOffset, yOffset);
      }
    }

    private static string AstrObjToString(AstronomicalObject obj)
    {
      return obj.Name + ", " + obj.X + ", " + obj.Y;
    }
    
    public override string Log()
    {
      var result = AstrObjToString(_sun) + "\n" + AstrObjToString(_moon) + "\n";
      
      foreach (var planet in _planets)
      {
        result += AstrObjToString(planet) + "\n";
      }
      
      return result.Substring(0, result.Length -  2);
    }
  }

  public class WaveExample : AbstractSimulation
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
             "Amplitude = 50\n" +
             "Shift = 60\n" +
             "TrailLength = 150";
    }

    public override void Init(Dictionary<string, string> model)
    {
      _wavelength = GetDouble(model, "Wavelength", 200);
      _amplitude = GetDouble(model, "Amplitude", 50);
      _shift = GetInt(model, "Shift", 60);
      _y = 0;
      _time = 0;
      _trailLength = GetInt(model, "TrailLength", 150);
      _trail = new List<double>(_trailLength) {_y};
    }

    public override void Update(long deltaTime)
    {
      _time += deltaTime / _wavelength;
      _y = Sin(_time) * _amplitude;
      _trail.Insert(0, _y);
      if (_trail.Count > _trailLength) _trail.RemoveAt(_trailLength - 1);
    }

    public override void Render(Context ctx, int width, int height)
    {
      for (var i = 0; i < _trail.Count; i++)
      {
        ctx.SetSourceRGB(0.769, 0.282, 0.295);
        ctx.Arc(width / 2 + _shift - 10 * i, height / 2.0 + _trail[i], 
          Math.Log(20.0, i + 2), 0, 2 * PI);
        ctx.ClosePath();
        ctx.Fill();
      }
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
      ctx.Arc(_reference.X, _reference.Y, 5, 0, PI * 2);
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
        ctx.Arc(_center.X, _center.Y, 40, 0, -PI * _arm2Rot / 180);
        ctx.Stroke();
      }
      
      RenderArm(ctx, _arm1.X, _arm1.Y);
      RenderArm(ctx, _arm2.X, _arm2.Y);
      RenderArm(ctx, _arm3.X, _arm3.Y);
      
      // Render center
      ctx.SetSourceColor(_mainColor);
      ctx.Arc(_center.X, _center.Y, 9.75, 0, PI * 2);
      ctx.Fill();
      ctx.Arc(_center.X, _center.Y, 13.1, 0, PI * 2);
      ctx.LineWidth = 4;
      ctx.Stroke();
    }
    
    private void RenderWheel(Context ctx, double x, double y, 
      double rot, Color color, bool inverse = false)
    {
      var radius = _wheelHeight / 15.0;
      const double degrees = PI / 180.0;
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
      ctx.Arc(x, y, 1.95 * 3, 0, PI * 2);
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