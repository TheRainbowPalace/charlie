namespace simulation_template
{
  public class SimulationTemplate
  {
    #region Meta functions
    
    public string GetTitle()
    {
      return "Simulation-Template";
    }

    public string GetDescr()
    {
      return "A new simulation.";
    }

    public string GetConfig()
    {
      return "";
    }

    public string GetMeta()
    {
      return "Author: Jakob Rieke;" +
             "Version: 1.0.0;" +
             "Licence: MIT";
    }

    #endregion
    
    #region State Variables
    
    #endregion
    
    #region State functions
    
    public void Init(string model)
    {
    }

    public void Update(long deltaTime)
    {
    }

    public void End()
    {
    }

    public void Manipulate()
    {
    }

    #endregion
    
    #region Output functions 
    
    public byte[] Render(int width, int height)
    {
      return null;
    }

    public string Log()
    {
      return null;
    }

    public byte[] RenderByteData()
    {
      return null;
    }

    public byte[] RenderAudioData()
    {
      return null;
    }
    
    #endregion
  }
}