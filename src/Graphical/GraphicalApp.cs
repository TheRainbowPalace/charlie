using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Cairo;
using charlie.Simulator;
using Gdk;
using Gtk;
using Json.Net;
using Application = Gtk.Application;
using Path = System.IO.Path;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace charlie.Graphical
{
  public class GraphicalApp
  {
    private readonly CharlieModel _model;

    private readonly Window _window;
    private DrawingArea _canvas;
    private LogOutput _logOutput;
    private Label _iterationLbl;
    private TextBuffer _configBuffer;
    

    public GraphicalApp()
    {
      _model = CharlieModel.LoadFromFile();
//      var accelGroup = new AccelGroup();

      var provider = new CssProvider();
      provider.LoadFromPath(GetResource("style.css"));
      StyleContext.AddProviderForScreen(Screen.Default, provider, 800);

      var window = new Window(WindowType.Toplevel)
      {
        DefaultWidth = _model.WindowWidth,
        DefaultHeight = _model.WindowHeight,
        Title = "",
        Role = "Charlie",
        Resizable = true
      };
      window.Move(_model.WindowX, _model.WindowY);
      window.SetIconFromFile(GetResource("logo.png"));
//      window.AddAccelGroup(accelGroup);

      window.Destroyed += (sender, args) => Quit();
      window.Show();

      _window = window;
      window.Child = CreateRoot();
      window.Child.ShowAll();
    }

    private void Quit()
    {
      if (_model.ActiveRun.Get() != null)
      {
        if (_model.ActiveRun.Get().Running) 
          ShowMessageDialog("Please stop the simulation first");
        
        _model.ActiveRun.Get().Stop();
        _model.ActiveRun.Get().End();
      }
      
      Application.Quit();
      CharlieModel.SaveToFile(_model);
    }
    
    private void LoadDefault()
    {
      Load(_model.DefaultSimPath, _model.DefaultSimClass);
    }
    
    /// <summary>
    /// Load a simulation from a given .dll file and a classname, create
    /// a new simulation run, initialize it and update the start configuration. 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    private bool Load(string path, string className)
    {
      try
      {
        _model.Sim?.Unload();
        _model.Sim = new Simulation(path, className);
        
        // -- Create run from simulation
        
        var run = new SimRun(_model.Sim.Spawn())
        {
          RenderHeight = _model.RenderHeight, 
          RenderWidth = _model.RenderWidth,
          IsWriteLogToFile = _model.WriteLogToFile,
          SimDelay = _model.SimDelay
        };
        run.OnUpdate += (sender, args) =>
        {
          Application.Invoke((s, a) => AfterUpdate());
        };
        run.OnLog += (sender, args) =>
        {
          Application.Invoke((s, a) => _logOutput.Log(args.Message));
        };
        run.Init(run.Instance.GetConfig());
        
        _model.ActiveRun.Set(run);
        
        return true;
      }
      catch (ArgumentException e)
      {
        ShowMessageDialog(e.Message);
        _model.Sim = null;
        LoadDefault();
        
        return false;
      }
    }

    private static string GetResource(string resource)
    {
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "resources", resource);
    }

    private void AfterUpdate()
    {
      _canvas.QueueDraw();
      _iterationLbl.Text = 
        "i = " + _model.ActiveRun.Get().Iteration + 
        ", e = " + _model.ActiveRun.Get().ElapsedTime / 1000 + "s" +
        ", a = " + Math.Round(_model.ActiveRun.Get().AverageIterationTime, 2) + 
        "ms";
    }
    
    private Widget CreateRoot()
    {
      _logOutput = new LogOutput();
      
      var content = new VBox(false, 20) {Name = "root"};
      content.PackStart(CreateLoadArea(), false, false, 0);
      content.PackStart(CreateTitle(), false, false, 0);
      content.PackStart(CreateStartConfigArea(), false, false, 0);
      content.PackStart(CreateCanvasArea(), false, false, 0);
      content.PackStart(CreateControlArea(), false, false, 0);
//      content.PackStart(CreateStateDebugArea(), false, false, 0);
      content.PackStart(CreateConfigArea(), false, false, 0);  
//      content.PackStart(CreateScheduleArea(), false, false, 0);  
      content.PackStart(CreateAboutArea(), false, false, 0);
      content.SizeAllocated += (o, a) => content.QueueDraw();

      var mainArea = new ScrolledWindow
      {
        OverlayScrolling = false,
        VscrollbarPolicy = PolicyType.External,
        HscrollbarPolicy = PolicyType.Never,
        MinContentWidth = 380,
        MinContentHeight = 420,
        Child = content
      };

      var result = new VBox(false, 0);
      result.PackStart(mainArea, true, true, 0);
      result.PackEnd(_logOutput, false, false, 0);
      return result;
    }

    private void ShowMessageDialog(string message)
    {
      ShowDialog(new Label(message)
      {
        LineWrap = true, 
        LineWrapMode = Pango.WrapMode.Word,
        MaxWidthChars = 200
      });
    }

    private void ShowDialog(Widget content)
    {
      var dialog = new Dialog("Charlie Dialog", _window,
        DialogFlags.Modal)
      {
        WidthRequest = 300,
        Decorated = false,
        WindowPosition = WindowPosition.CenterOnParent,
        BorderWidth = 0,
        SkipTaskbarHint = true
      };
      _window.GetPosition(out var winX, out var winY);
      dialog.GetPosition(out var x, out var y);
      dialog.Move(x, winY + 30);

      dialog.AddActionWidget(new Button("Ok"), ResponseType.Close);
      dialog.ContentArea.Spacing = 10;
      dialog.ContentArea.Add(content);

      dialog.ShowAll();
      dialog.Run();
      dialog.Destroy();
    }

    private Widget CreateTitle()
    {
      var title = new VBox(false, 5)
      {
        new Label("No simulation loaded") {Name = "title", Xalign = 0},
        new Label
        {
          Text = "Enter a path to a simulation e.g.\n" +
                 "- '/my/simulation.dll : some_namespace.MySimulation'\n" +
                 "- '/my/simulation.dll : MySimulation'",
          Wrap = true,
          UseMarkup = true,
          Halign = Align.Start, 
          Xalign = 0
        }
      };
      _model.ActiveRun.OnSet += (sender, args) =>
      {
        var simTitle = _model.ActiveRun.Get().Instance.GetTitle();
        var simDesc = _model.ActiveRun.Get().Instance.GetDescr();
        ((Label) title.Children[0]).Text = string.IsNullOrEmpty(simTitle) ?
            "Some Random Simulation" : simTitle;
        ((Label) title.Children[1]).Text = string.IsNullOrEmpty(simDesc) ? 
            "This simulation does not contain a description" : simDesc;
      };
      return title;
    }

    /// <summary>
    /// Removes an element from an and returns a new copy. If the the provided
    /// array is null, null is returned.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] Remove<T>(T[] data, T value)
    {
      return data?.Where(val => !val.Equals(value)).ToArray();
    }

    private Box CreateLoadArea()
    {
      var pathEntry = new Entry(
        $"{_model.DefaultSimPath}:{_model.DefaultSimClass}")
      {
        PlaceholderText = "/path/to/your/module.dll", 
        HasFocus = false,
        HasFrame = false
      };
      pathEntry.FocusGrabbed += (sender, args) => 
        pathEntry.SelectRegion(pathEntry.TextLength, pathEntry.TextLength);

      var previousLoadedArea = new VBox(false, 0)
      {
        Name = "previousLoadedArea"
      };

      void AddEntry(string complexPath)
      {
        var parsed = Simulation.ParseComplexPath(complexPath);
        var loadEntryBtn = new Button($"- {parsed[1]}")
        {
          BorderWidth = 0, Xalign = 0
        };
        loadEntryBtn.Clicked += (o, eventArgs) =>
        {
          pathEntry.Text = complexPath;
          Load(parsed[0], parsed[1]);
        };

        var removeEntryBtn = new Button("x");
        removeEntryBtn.Clicked += (sender, args) =>
        {
          var previousLoaded = _model.PreviousLoaded.Get();
          _model.PreviousLoaded.Set(Remove(previousLoaded, complexPath));
        };

        var entry = new HBox(false, 10);
        entry.PackStart(loadEntryBtn, false, false, 0);
        entry.PackEnd(removeEntryBtn, false, false, 0);
        
        previousLoadedArea.Add(entry);
      }
      
      foreach (var complexPath in _model.PreviousLoaded.Get()) 
        AddEntry(complexPath);
      
      previousLoadedArea.ShowAll();
      
      _model.PreviousLoaded.OnSet += (sender, args) =>
      {
        foreach (var entry in previousLoadedArea.Children)
          previousLoadedArea.Remove(entry);

        foreach (var complexPath in _model.PreviousLoaded.Get())
        {
          AddEntry(complexPath);
        }

        previousLoadedArea.ShowAll();
      };

      var loadBtn = new Button("_Load");

      loadBtn.Clicked += (sender, args) =>
      {
        if (_model.ActiveRun.Get() != null && _model.ActiveRun.Get().Running)
        {
          ShowMessageDialog("Please stop the simulation first");
          return;
        }
        
        var parsed = Simulation.ParseComplexPath(pathEntry.Text);
        if (parsed == null)
        {
          ShowMessageDialog("Invalid path, please specify class inside dll");
          return;
        }
          
        var path = parsed[0];
        var className = parsed[1];
        var complexPath = $"{path}:{className}";
        var isNewSimLoaded = Load(path, className);

        if (!isNewSimLoaded 
            || _model.PreviousLoaded.Get().Contains(complexPath)) 
          return;
        
        var buffer = new List<string>(_model.PreviousLoaded.Get());
        buffer.Insert(0, complexPath);
        if (buffer.Count > _model.PrevLoadedMaxLength) 
          buffer.RemoveAt(buffer.Count - 1);
        
        _model.PreviousLoaded.Set(buffer.ToArray());
      };
      
      var loadControl = new HBox(false, 10);
      loadControl.PackStart(pathEntry, true, true, 0);
      loadControl.PackStart(loadBtn, false, false, 0);
      
      var loadArea = new VBox(false, 5);
      loadArea.PackStart(loadControl, false, false, 0);
      loadArea.PackStart(previousLoadedArea, false, false, 0);
      
      return loadArea;
    }
    
    private Box CreateConfigArea()
    {
      var title = new Label("Simulator Configuration") 
      {
        Name = "configTitle",
        Halign = Align.Start,
        TooltipText = "Reinitialize the simulation to activate changes."
      };
      
      var delayEntry = new Entry("" + _model.SimDelay)
      {
        PlaceholderText = "" + _model.SimDelay,
        HasFrame = false
      };
      delayEntry.Activated += (s, a) =>
      {
        if (int.TryParse(delayEntry.Text, out var value) && value >= 0)
        {
          _model.SimDelay = value;
        }
      };
      var delayControl = new HBox(false, 0);
      delayControl.PackStart(new Label(
          "Delay between iterations in ms "), false, false, 0);
      delayControl.PackStart(delayEntry, false, false, 0);

      var logToFileLabel = new Label("Write log to file ");
      var logToFileButton = new ToggleButton(
        _model.WriteLogToFile ? "Yes" : "No");
      logToFileButton.Clicked += (o, a) =>
      {
        _model.WriteLogToFile = !_model.WriteLogToFile;
        logToFileButton.Label = _model.WriteLogToFile ? "Yes" : "No";
      };
      var logToFileControl = new HBox(false, 0);
      logToFileControl.PackStart(logToFileLabel, false, false, 0);
      logToFileControl.PackStart(logToFileButton, false, false, 0);

      var logEveryIterationLabel = new Label(
        "Log ");
      var logEveryIterationBtn = new ToggleButton(
        _model.LogEveryIteration ? "Every iteration" : "Only last iteration");
      logEveryIterationBtn.Clicked += (o, a) =>
      {
        _model.LogEveryIteration = !_model.LogEveryIteration;
        logEveryIterationBtn.Label = _model.LogEveryIteration ? 
          "Every iteration" : "Only last iteration";
      };
      var logEveryIterControl = new HBox(false, 0);
      logEveryIterControl.PackStart(logEveryIterationLabel, false, false, 0);
      logEveryIterControl.PackStart(logEveryIterationBtn, false, false, 0);

      var result = new VBox (false, 0) {Name = "config"};
      result.PackStart(title, false, false, 0);
      result.PackStart(delayControl, false, false, 0);
//      result.PackStart(logEveryIterControl, false, false, 0);
      result.PackStart(logToFileControl, false, false, 0);
      result.ShowAll();
      
      return result;
    }

    private Box CreateControlArea()
    {
      var startBtn = new Button("_Start") {HasFocus = true};
      startBtn.Clicked += Start;

      void Stop(object sender, EventArgs args)
      {
        if (_model.ActiveRun.IsNull()) return;
        
        _model.ActiveRun.Get().Stop();
        startBtn.Label = "Start";
        startBtn.Clicked -= Stop;
        startBtn.Clicked += Start;
      }

      void Start(object sender, EventArgs args)
      {
        if (_model.ActiveRun.IsNull()) return;
        
        _model.ActiveRun.Get().Update();
        startBtn.Label = "Stop ";
        startBtn.Clicked -= Start;
        startBtn.Clicked += Stop;
      }
      
      var initBtn = new Button("_Init");
      initBtn.Clicked += (sender, args) =>
      {
        if (_model.ActiveRun.IsNull()) return;
        if (_model.ActiveRun.Get().Started) Stop(this, EventArgs.Empty);
        
        var timer = new Timer(20);
        timer.Elapsed += (o, eventArgs) =>
        {
          if (_model.ActiveRun.Get().Running)
          {
            Logger.Say("Waiting for update thread to finish.");
            return;
          }

          timer.Enabled = false;
          _model.ActiveRun.Get().End();
          _model.ActiveRun.Get().IsWriteLogToFile = _model.WriteLogToFile;
          _model.ActiveRun.Get().SimDelay = _model.SimDelay;
          _model.ActiveRun.Get().Init(_configBuffer.Text);
        };
        timer.Enabled = true;
      };

      // -- Steps controls
      
      var stepsEntry = new Entry("100")
      {
        WidthChars = 5,
        HasFrame = false,
        Alignment = 0.5f
      };
      var stepsBtn = new Button("R") {TooltipText = "Run x iterations"};
      stepsBtn.Clicked += (sender, args) =>
      {
        if (_model.ActiveRun.IsNull()) return;
        if (_model.ActiveRun.Get().Running) return;
        if (int.TryParse(stepsEntry.Text, out var x)) 
          _model.ActiveRun.Get().Update(x);
        else ShowMessageDialog("Please enter a number >= 0.");
      };
      var stepsBox = new HBox(false, 0) {stepsEntry, stepsBtn};
      stepsBox.Name = "runSteps";

      var pictureBtn = new Button("P")
      {
        TooltipText = "Save the current render output as PNG",
        Name = "pictureBtn"
      };
      pictureBtn.Clicked += (s, a) =>
      {
        if (_model.ActiveRun.IsNull()) return;
        
        _model.ActiveRun.Get().SaveImage();
      };

      var result = new HBox(false, 10);
      result.PackStart(initBtn, false, false, 0);
      result.PackStart(startBtn, false, false, 0);
      result.PackStart(stepsBox, false, false, 0);
      result.PackEnd(pictureBtn, false, false, 0);
      result.HeightRequest = 10;
      return result;
    }
    
    private Widget CreateCanvasArea()
    {
      var renderTitle = new Label("Rendering") {Halign = Align.Start};
      
      _canvas = new DrawingArea {Name = "canvas"};
      _canvas.SetSizeRequest(400, 400);
      _canvas.Drawn += (o, args) =>
      {
        args.Cr.Rectangle(0, 0, 
          _canvas.AllocatedWidth, 
          _canvas.AllocatedHeight);
        args.Cr.LineWidth = 3;
        args.Cr.SetSourceRGB(0.2, 0.2, 0.2);
        args.Cr.FillPreserve();
        args.Cr.SetSourceRGB(0, 0, 0);
        args.Cr.Stroke();

        if (_model.ActiveRun.Get()?.ImageData == null) return;

        var surface = new ImageSurface(
          _model.ActiveRun.Get().ImageData, Format.ARGB32,
          _model.ActiveRun.Get().RenderWidth, 
          _model.ActiveRun.Get().RenderHeight,
          4 * _model.ActiveRun.Get().RenderWidth);
        args.Cr.Scale(.5, .5);
        args.Cr.SetSourceSurface(surface, 0, 0);
        args.Cr.Paint();
        surface.Dispose();
      };

      _iterationLbl = new Label("i, e, a")
      {
        Halign = Align.Start,
        TooltipText = "Iterations, Elapsed time, Average elapsed time"
      };
      
      var result = new VBox(false, 7)
      {
        renderTitle, _canvas, _iterationLbl
      };
      return result;
    }

    private Widget CreateStartConfigArea()
    {
      _configBuffer = new TextBuffer(new TextTagTable())
      {
        Text = "# No simulation loaded\n"
      };

      _model.ActiveRun.OnSet += (sender, args) =>
      {
        _configBuffer.Text = _model.ActiveRun.Get().Instance.GetConfig();
      };

      var title = new Label("Start Configuration")
      {
        Xalign = 0, Valign = Align.Start
      };
      var textView = new TextView(_configBuffer)
      {
        WidthRequest = 400,
        Indent = 3,
        WrapMode = WrapMode.WordChar
      };
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      return result;
    }

    private Widget CreateScheduleArea()
    {
      var result = new VBox(false, 5);

      var title = new Label("Schedule") {Halign = Align.Start};
      
      var scheduleEntry = new Entry("200 x 50")
      {
        WidthChars = 8,
        HasFrame = false,
        Alignment = 0.5f
      };
      var scheduleBtn = new Button("S")
      {
        TooltipText = "Schedule i iterations times n runs e.g. '100 x 20'"
      };
      scheduleBtn.Clicked += (sender, args) =>
      {
        if (_model.ActiveRun == null)
        {
          Logger.Say("No simulation loaded.");
          return;
        }
        
        var values = scheduleEntry.Text.Split('x');
        if (values.Length == 2 && 
            int.TryParse(values[0], out var iterations) &&
            int.TryParse(values[1], out var runs))
        {
          var totalIterations = iterations * runs;
          
          var task = new HBox(false, 5);
          var taskTitle = new Label(
            $"- {iterations} x {runs}, 0% from {totalIterations} in total");
          
//          var deleteTaskBtn = new Button("x") { Name = "textBtn"};
//          deleteTaskBtn.Clicked += (o, a) =>
//          {
//            result.Remove(task);
//          };
          
          task.PackStart(taskTitle, false, false, 0);
//          task.PackEnd(deleteTaskBtn, false, false, 0);
          task.ShowAll();
          
          result.Add(task);
          
          var run = new SimRun(_model.Sim.Spawn())
          {
            RenderHeight = _model.RenderHeight, 
            RenderWidth = _model.RenderWidth,
            IsWriteLogToFile = _model.WriteLogToFile,
            SimDelay = _model.SimDelay
          };
          run.OnEnd += (o, a) => result.Remove(task);
          run.OnUpdate += (o, a) =>
          {
//            var percentage = 
            taskTitle.Text =
              $"- {iterations} x {runs}, {0}% from {totalIterations} in total";
          };
          _model.ScheduledRuns.Add(run);
        }
        else ShowMessageDialog("Please enter a iterations " +
                               "x runs e.g. '100 x 20'");
      };
      var scheduleBox = new HBox(false, 0) {Name = "runSteps"};
      scheduleBox.PackStart(scheduleEntry, true, true, 0);
      scheduleBox.PackStart(scheduleBtn, false, false, 0);

      var startStopScheduleBtn = new Button("Start");
      startStopScheduleBtn.Clicked += (sender, args) =>
      {
        if (_model.ScheduledRuns.Count == 0) return;
        var activeRun = _model.ScheduledRuns[0];
        activeRun.OnEnd += (o, a) =>
        {
//          _model.ScheduledRuns
        };
      };
      
      var clearScheduleBtn = new Button("Clear");
      
      var scheduleControls = new HBox(false, 10);
      scheduleControls.PackStart(scheduleBox, true, true, 0);
      scheduleControls.PackEnd(clearScheduleBtn, false, false, 0);
      scheduleControls.PackEnd(startStopScheduleBtn, false, false, 0);
      
      result.PackStart(title, false, false, 0);
      result.PackStart(scheduleControls, false, false, 5);
      return result;
    }

    private Widget CreateStateDebugArea()
    {
      var buffer = new TextBuffer(new TextTagTable())
      {
        Text = "Simulation State 🤔"
      };

      _model.ActiveRun.OnSet += (sender, args) =>
      {
//        _model.ActiveRun.Get().OnInit += (s2, args2) =>
//        {
//          buffer.Text = _model.ActiveRun.Get().Instance.GetState();
//        };
//        _model.ActiveRun.Get().OnUpdate += (s2, args2) =>
//        {
//          buffer.Text = _model.ActiveRun.Get().Instance.GetState();
//        };
      };
      
      var title = new Label("Simulation State")
      {
        Xalign = 0, Valign = Align.Start
      };
      var textView = new TextView(buffer)
      {
        WidthRequest = 400,
        Indent = 3,
        WrapMode = WrapMode.WordChar
      };
      var result = new VBox(false, 7);
      result.PackStart(title, false, false, 0);
      result.PackStart(textView, true, true, 0);
      
      return result;
    }
    
    private Box CreateAboutArea()
    {
      var aboutTitle = new Label("Charlie")
      {
        Name = "aboutTitle", Halign = Align.Start
      };
      var result = new VBox(false, 1)
      {
        aboutTitle,
        new Label("Version " + _model.Version) {Halign = Align.Start},
        new Label($"Author {_model.Author}") {Halign = Align.Start},
        new Label(_model.Copyright) {Halign = Align.Start}
      };
      result.Name = "about";
      return result;
    }
  }
}