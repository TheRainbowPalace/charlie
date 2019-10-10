using System.Collections.Generic;

namespace charlie.Simulator
{
  /// <summary>
  /// A simulation instance, created from a Simulation.
  /// </summary>
  public class SimInstance : ISimulation
  {
    public readonly SimulationProxy Proxy;
    public readonly int Id;

    
    public SimInstance(SimulationProxy proxy)
    {
      Proxy = proxy;
      Id = proxy.Spawn();
    }


    public string GetTitle()
    {
      return Proxy.GetTitle(Id);
    }

    public string GetDescr()
    {
      return Proxy.GetDescr(Id);
    }

    public string GetMeta()
    {
      return Proxy.GetMeta(Id);
    }

    public string GetConfig()
    {
      return Proxy.GetConfig(Id);
    }

    public string GetState()
    {
      return Proxy.GetState(Id);
    }

    public void Init(Dictionary<string, string> model)
    {
      Proxy.Init(Id, model);
//      Console.WriteLine("TypeTree:");
//      Console.WriteLine(Proxy.GetDataTree(Id));
    }

    public void End()
    {
      Proxy.End(Id);
    }

    public void Update(long deltaTime)
    {
      Proxy.Update(Id, deltaTime);
    }

    public byte[] Render(int width, int height)
    {
      return Proxy.Render(Id, width, height);
    }

    public string Log()
    {
      return Proxy.Log(Id);
    }
  }
}