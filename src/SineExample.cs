using System;
using System.Collections.Generic;
using Cairo;
using run_charlie;

[Serializable]
public class SineExample : MarshalByRefObject, ISimulation
{
  public string GetTitle()
  {
    return "Sine Example";
  }

  public string GetDescr()
  {
    return "The sine example allows to render and modify a sine wave.";
  }

  public string GetConfig()
  {
    return "y-factor: 1\nx-factor: 1";
  }

  public void Init(Dictionary<string, string> config)
  {
    Console.WriteLine("This is your sine example");
  }

  public void Update(long dt)
  {
  }

  public void Render(Context ctx)
  {
  }

  public string Log()
  {
    return null;
  }
}