using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace charlie.Simulator
{
  /// <summary>
  /// The basic interface to load a C# simulation.
  /// Normally you would not use this class directly but the Simulation class
  /// instead.
  /// </summary>
  public class SimulationProxy : MarshalByRefObject
  {
    public string ClassName { get; private set; }
    public string Path { get; private set; }
    public string ComplexPath { get; private set; }
    private Dictionary<int, object> _instances;
    private int _idCounter;
    private Assembly _assembly;
    private Type _type;
    private MethodInfo _getTitle;
    private MethodInfo _getDescr;
    private MethodInfo _getConfig;
    private MethodInfo _getMeta;
    private MethodInfo _init;
    private MethodInfo _end;
    private MethodInfo _update;
    private MethodInfo _render;
    private MethodInfo _log;


    /// <summary>
    /// Load the simulation from an assembly.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="className"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Load(string path, string className)
    {
      Path = path;
      ClassName = className;
      ComplexPath = path + ":" + className;
      _instances = new Dictionary<int, object>();
      _idCounter = 0;
      _assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
      _type = _assembly.GetType(className);
      
      if (_type == null) throw new ArgumentException();

      _getTitle = _type.GetMethod("GetTitle");
      _getDescr = _type.GetMethod("GetDescr");
      _getConfig = _type.GetMethod("GetConfig");
      _getMeta = _type.GetMethod("GetMeta");
      _init = _type.GetMethod("Init");
      _end = _type.GetMethod("End");
      _update = _type.GetMethod("Update");
      _render = _type.GetMethod("Render", new []{typeof(int), typeof(int)});
      _log = _type.GetMethod("Log");
    }

    [Serializable]
    public struct DataTree
    {
      public string TypeName;
      public string Value;
      public string Description;
      public List<DataTree> Children;

      public override string ToString()
      {
        var result = $"{Value} : {TypeName}";
        if (Children == null) return result;
        
        foreach (var child in Children) result += "\n  - " + child;
        return result;
      }
    }

    private const BindingFlags Flags =
      BindingFlags.NonPublic | BindingFlags.Instance |
      BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty |
      BindingFlags.DeclaredOnly | BindingFlags.ExactBinding |
      BindingFlags.CreateInstance | BindingFlags.SetProperty |
      BindingFlags.SetField | BindingFlags.GetField;
    
    private readonly HashSet<object> _foundValues = new HashSet<object>();

    public string ObjectToString(object value)
    {
      return value.GetType().Name + ":\n" + 
             ObjectToString(value.GetType(), 0, value);
    }
    
    private string ObjectToString(Type type, int depth, object value)
    {
      if (value == null || _foundValues.Contains(value)) return "";
      
      _foundValues.Add(value);
      var result = "";

      var fields = type.GetFields(Flags);
//      var properties = type.GetProperties(Flags);
      
      foreach (var field in fields)
      {
        for (var i = 0; i < depth; i++) result += "  ";
        
        result += "- " + field.FieldType.Name + " : ";
        result += field.Name;

        var fieldType = field.FieldType;
        var fieldValue = field.GetValue(value);

        result += fieldValue != null ? $" = {fieldValue}" : " = null";

        if (fieldType.IsPrimitive)
        {
          result += " --> Primitive type\n";
        }
        else if (fieldType.IsEnum)
        {
          result += " --> Enumeration type\n";
        }
        else if (fieldType.IsValueType)
        {
          result += " --> Struct type\n";
          result += ObjectToString(fieldType, depth + 1, fieldValue);
        }
        else if (fieldType.IsGenericType)
        {
          result += " --> Generic type\n";
          result += ObjectToString(fieldType, depth + 1, fieldValue);
//          foreach (var argument in fieldType.GetGenericArguments())
//          {
//            PrintType(argument, depth + 1);
//          }
        }
        else if (fieldType.IsArray)
        {
          result += " --> Array type\n";
          
          if (fieldValue == null) continue;

          var array = (Array) fieldValue;
          var arrayType = array.GetType().GetElementType();
          
          var i = 0;
          foreach (var elem in array)
          {
            for (var j = 0; j < depth + 1; j++) result += "  ";

            // Use elem.GetType() in the following since array might not contain
            // elements of just one type e.g. an object[] might contain numbers
            // and strings
            
            if (arrayType.IsPrimitive)
            {
              result += $"{i}.";
              result += ObjectToString(elem.GetType(), 0, elem);
            }
            else
            {
              if (elem == null)
              {
                result += $"{i}. {arrayType.Name}: null\n";
              }
              else
              {
                result += $"{i}. {elem.GetType().Name}:\n";
                result += ObjectToString(elem.GetType(), depth + 2, elem);
              }
            }
            i++;
          }
        }
        else if (fieldType.IsClass)
        {
          result += " --> Class type\n";
          ObjectToString(fieldType, depth + 1, fieldValue);
        }
        else result += "\n";
      }
      
//      foreach (var property in properties)
//      {
//        for (var i = 0; i < depth; i++) Console.Write("  ");
//        
//        Console.WriteLine($"* {property.PropertyType.Name} : {property.Name}");
//      }
      _foundValues.Remove(value);
      
      return result;
    }

    /// <summary>
    /// Spawns a new simulation instance and returns an identifier (i).
    /// The instance is stored internally and can be used by calling
    /// Simulation.GetTitle(i), Simulation.GetDescr(i), etc. .
    /// </summary>
    public int Spawn()
    {
      _instances.Add(_idCounter, _assembly.CreateInstance(ClassName));
      _idCounter++;
      return _instances.Count - 1;
    }

    /// <summary>
    /// Remove a simulation instance from the internal buffer by its id.
    /// </summary>
    public void Remove(int id)
    {
      _instances.Remove(id);
    }

    /// <summary>
    /// Get a list of IDs for all spawned instances.
    /// </summary>
    /// <returns></returns>
    public int[] SimIds()
    {
      return _instances.Keys.ToArray();
    }
    
    /// <summary>
    /// The number of loaded simulations.
    /// </summary>
    /// <returns></returns>
    public int SimCount()
    {
      return _instances.Count;
    }
    
    public string GetTitle(int index)
    {
      return (string) _getTitle.Invoke(_instances[index], new object[0]);
    }

    public string GetDescr(int index)
    {
      return (string) _getDescr.Invoke(_instances[index], new object[0]);
    }
    
    public string GetConfig(int index)
    {
      return (string) _getConfig.Invoke(_instances[index], new object[0]);
    }

    public string GetMeta(int index)
    {
      return (string) _getMeta.Invoke(_instances[index], new object[0]);
    }

    public string GetState(int index)
    {
      return ObjectToString(_instances[index]);
    }

    public void Init(int index, Dictionary<string, string> config)
    {
      _init.Invoke(_instances[index], new object[] {config});
    }

    public void End(int index)
    {
      _end.Invoke(_instances[index], new object[0]);
    }

    public void Update(int index, long dt)
    {
      _update.Invoke(_instances[index], new object[] {dt});
    }

    public byte[] Render(int index, int width, int height)
    {
      return (byte[]) _render.Invoke(_instances[index], 
        new object[] {width, height});
    }

    public string Log(int index)
    {
      return (string) _log.Invoke(_instances[index], new object[0]);
    }
  }
}