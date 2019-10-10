using System;

namespace charlie
{
  public static class Logger
  {
    public static void Say(string text)
    {
      Console.WriteLine(text);
    }
    
    public static void Warn(string text)
    {
      Console.WriteLine(text);
    }

    public static void Error(string text)
    {
      Console.WriteLine(text);
    }
  }
}