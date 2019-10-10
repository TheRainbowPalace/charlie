using System;
using System.IO;
using charlie.Simulator;
using Path = System.IO.Path;

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
		    Logger.Say("--init <folder> <name> --> Initialize a new simulation " +
		               "project.");
		    Logger.Say("--run <simulation> <iterations> <runs> " +
		               "--> Run a simulation for a number of runs, each " +
		               "for a certain number of iterations. The results are " +
		               "stored as files.");
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
          Logger.Warn("Correct usage:");
          Logger.Warn("--run simulation iterations runs");
          return;
        }
        if (!int.TryParse(args[2], out var iterations))
        {
          Logger.Warn("Provide a number for iterations");
        }
        if (!int.TryParse(args[3], out var runs))
        {
          Logger.Warn("Provide a number for runs");
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
		  else if (args[0] == "--init")
		  {
		    string GetAttribute(string text)
		    {
		      string attribute = null;
		      
		      while (string.IsNullOrEmpty(attribute))
		      {
			      System.Console.Write($"{text}: ");
		        attribute = System.Console.ReadLine();
		      }
		      
		      return attribute;
		    }

		    bool GetBool(string question)
		    {
			    System.Console.Write($"{question}? y/N: ");
		      var answer = System.Console.ReadLine();
		      
		      return answer != null && (answer == "y" || answer == "Y");
		    }

		    // -- Get attributes and folders
		    
		    var title = GetAttribute("Simulation title").Trim().Replace(' ', '-');
		    var titleUnderscored = title.Replace('-', '_');
		    
		    var titleCamelCased = "";
		    foreach (var part in title.Split('-'))
		    {
		      titleCamelCased += part[0].ToString().ToUpper() + part.Substring(1);
		    }

		    var author = GetAttribute("Author");
		    var license = GetAttribute("Licence");
		    
		    var templateDir = Path.Combine(
		      AppDomain.CurrentDomain.BaseDirectory,
		      "resources",
		      "simulation-template");
		    var destinationDir = Path.Combine(
		      Directory.GetCurrentDirectory(), title);
		    
		    // -- Replace directory if necessary
		    
		    if (Directory.Exists(destinationDir))
		    {
		      var isOverwrite = GetBool("Directory already exists, overwrite");
		      if (!isOverwrite) return;
		      
		      Directory.Delete(destinationDir, true);
		    }

		    // -- Generate files
		    
		    Directory.CreateDirectory(destinationDir);
		    
		    var files = Directory.GetFiles(templateDir);
		    foreach (var file in files)
		    {
		      var content = File.ReadAllText(file);
		      content = content
		        .Replace("simulation-template", title)
		        .Replace("simulation_template", titleUnderscored)
		        .Replace("SimulationTemplate", titleCamelCased)
		        .Replace("<author-placeholder>", author)
		        .Replace("<licence-placeholder>", license);
		      
		      var fileName = Path.GetFileName(file)
		        .Replace("simulation-template", title);
		      var destFile = Path.Combine(destinationDir, fileName);
		      File.WriteAllText(destFile, content);
		      
		      Logger.Say("- " + Path.Combine(title, fileName));
		    }
		  }
      else
      {
        Logger.Say($"Unknown command '{args[0]}', use --help");
      }
    }
  }
}