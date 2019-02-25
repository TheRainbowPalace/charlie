using System.Collections.Generic;

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
}