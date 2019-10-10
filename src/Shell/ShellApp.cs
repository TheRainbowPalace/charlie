using System;
using System.IO;
using charlie.Simulator;

namespace charlie.Shell
{
  public static class ShellApp
  {
    public static void Run(string[] args)
    {
      if (args[0] == "--help" || args[0] == "-h")
		  {
		    Logger.Say("Commands:");
		    Logger.Say("--help --> Print help information");
        Logger.Say("--get (config|descr|meta) --> Get information about " +
		               "the simulation");
		    Logger.Say("--run <simulation> <iterations> <runs> <out-dir> " +
		               "--> Run a simulation for a number of runs, each " +
		               "for a certain number of iterations. The results are " +
		               "stored in ~/.charlie/<out-dir>");
		  }
		  else if (args[0] == "--get")
		  {
		    var sim = new Simulation(args[1]);
		    var instance = sim.Spawn();
		    if (args[2] == "config") Logger.Say(instance.GetConfig());
		    else if (args[2] == "descr") Logger.Say(instance.GetDescr());
		    else if (args[2] == "meta") Logger.Say(instance.GetMeta());
		  }
      else if (args[0] == "--run")
      {
        if (args.Length < 4)
        {
          Logger.Warn("Invalid number of parameters to run a simulation.");
          Logger.Warn("See --help for more information.");
          return;
        }
        if (!int.TryParse(args[2], out var iterations))
        {
          Logger.Warn($"'{args[2]}' is not a number.");
        }
        if (!int.TryParse(args[3], out var runs))
        {
          Logger.Warn($"'{args[3]}' is not a number.");
        }

        Simulation sim;
        try
        {
          sim = new Simulation(args[1]);
        }
        catch (Exception e)
        {
          Logger.Warn(e.ToString());
          return;
        }
        
        var run = new SimRun(sim.Spawn())
        {
          IsWriteLogToFile = true,
          RenderHeight = 800,
          RenderWidth = 800
        };

        var config = run.Instance.GetConfig();
        
        // -- Parse simulation start configuration
        
        if (args.Length > 4)
        {
          var configFile = args[4];
          if (!Path.IsPathRooted(configFile))
          {
            configFile = Path.Combine(Environment.CurrentDirectory, configFile);
          }
          if (!File.Exists(configFile))
          {
            Logger.Warn("Provide start configuration " +
                        $"file does not exist: '{configFile}'");
            return;
          }
          
          config = File.ReadAllText(configFile);
        }

        // -- Parse simulation data sub directory
        
        if (args.Length > 5) run.LogSubDirectory = args[5];
        
        // -- Run simulation
        
        Logger.Say($"Running Simulation {sim.ClassName} {iterations}x{runs}");
        for (var i = 0; i < runs; i++)
        {
          run.Init(config);
          run.UpdateSync(iterations);
          run.SaveImage();
          run.End();
          
          var percentage = (double)(i + 1) / runs * 100;
          Logger.Say($"{percentage}% done, {run.Initializations} runs");
        }
        sim.Unload();
      }
      else
      {
        Logger.Say($"Unknown command '{args[0]}', use --help");
      }
    }
  }
}