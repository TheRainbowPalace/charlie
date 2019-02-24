using System.Collections.Generic;
using Cairo;

namespace run_charlie
{
  public interface ISimulation
  {
    string GetTitle();
    string GetDescr();
    string GetConfig();
    void Init(Dictionary<string, string> config);
    void Update(long deltaTime);
    void Render(Context ctx);
    string Log();
  }
}