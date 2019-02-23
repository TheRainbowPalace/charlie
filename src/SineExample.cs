using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Cairo;

public class SineExample
{
  public static readonly ConcurrentDictionary<string, object> Objects = 
    new ConcurrentDictionary<string, object>();

  public static readonly string Title = "Sine Example";

  public static readonly string Descr =
    "The sine example allows to render and modify a sine wave.";

  public static readonly string Config =
    "y-factor: 1\n" +
    "x-factor: 1";
  
  
  public static void Init(Dictionary<string, string> config)
  {
    Console.WriteLine("This is your sine example");
//    Console.WriteLine(config.ToString());
  }

  public static void Update(long dt)
  {
    
  }

  public static void Render(Context ctx)
  {
    
  }

  public static void Log(object o)
  {
    
  }
}