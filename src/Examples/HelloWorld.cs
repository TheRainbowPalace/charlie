using System.Collections.Generic;
using Cairo;
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
}