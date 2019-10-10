using System;
using System.Collections.Generic;
using Cairo;
using static System.Math;

namespace charlie
{
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