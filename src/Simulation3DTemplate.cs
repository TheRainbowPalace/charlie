using System.Collections.Generic;
using Mogre;

namespace charlie
{
  public class Simulation3DTemplate
  {
    #region Meta functions
    
    public string GetTitle()
    {
      return "Simulation with 3D Output";
    }

    public string GetDescr()
    {
      return "Render something in 3D";
    }

    public string GetConfig()
    {
      return "";
    }

    public string GetMeta()
    {
      return "Author: Jakob Rieke; Version: 1.0.0";
    }

    #endregion
    
    #region State Variables
    
    private Root _root;
    private SceneManager _sceneManager;
    
    #endregion
    
    #region State functions
    
    public void Init(Dictionary<string, string> model)
    {
      _root = new Root();
      _sceneManager = _root.CreateSceneManager(SceneType.ST_EXTERIOR_CLOSE);
      _sceneManager.AmbientLight = ColourValue.White;
      
      var ent = _sceneManager.CreateEntity("Robot", "robot.mesh");
           
      // Create the Robot's SceneNode
      var node = _sceneManager.RootSceneNode.CreateChildSceneNode(
        "RobotNode",
        new Vector3(0.0f, 0.0f, 0.25f));
      node.AttachObject(ent);
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