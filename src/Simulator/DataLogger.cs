using System;
using System.IO;
using DateTime = System.DateTime;
using Path = System.IO.Path;

namespace charlie.Simulator
{
  public class LogEventArgs : EventArgs
  {
    public readonly string Message;

    public LogEventArgs(string message)
    {
      Message = message;
    }
  }
  
  public class DataLogger
  {
    /// <summary>
    /// The directory where all log output is stored.
    /// </summary>
    public readonly string FileDir;
    
    /// <summary>
    /// The name of the log file.
    /// </summary>
    public readonly string FileName;

    public DataLogger(string projectName, string projectInstanceName,
      int maxFileLength = 50000, int maxBufferLength = 1000)
    {
      MaxFileLength = maxFileLength;
      MaxBufferLength = maxBufferLength;
      _buffer = "";
      
      // -- Create output directory

      FileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".charlie",
        projectName,
        projectInstanceName);
      
      if (!Directory.Exists(FileDir)) Directory.CreateDirectory(FileDir);
      
      // -- Create file name
      
      var date = DateTime.Now.ToUniversalTime();
      FileName = "run-" + date.Year + 
                 "-" + (date.Month < 10 ? "0" : "") + date.Month + 
                 "-" + (date.Day < 10 ? "0" : "") + date.Day +
                 "T" + (date.Hour < 10 ? "0" : "") + date.Hour + 
                 "-" + (date.Minute < 10 ? "0" : "") + date.Minute + 
                 "-" + (date.Second < 10 ? "0" : "") + date.Second + "Z" +
                 date.Millisecond;
    }
    
    /// <summary>
    /// The maximum length of the internal log buffer. Duo to performance
    /// reasons log entries are first written to RAM and only to file if
    /// the buffer is full or WriteLogBuffer() is called manually. Defaults to
    /// 1000.
    /// </summary>
    public readonly int MaxBufferLength;
    
    /// <summary>
    /// The maximum number of log entries a log file may contain until a new
    /// one is created.
    /// </summary>
    public readonly int MaxFileLength;
    
    private string _buffer;
    private int _fileLength;
    private int _fileCount;

    
    /// <summary>
    /// Write a log entry into the log buffer. The buffer is written to file
    /// if it reaches the MaxBufferLength.
    /// </summary>
    /// <param name="entry"></param>
    public void Log(string entry)
    {
      if (string.IsNullOrEmpty(entry)) return;
      
      _buffer += entry;
      if (_buffer.Length >= MaxBufferLength) WriteBuffer();
    }

    public void WriteBuffer()
    {
      if (_buffer.Length == 0) return;
      
      if (_fileLength + _buffer.Length > MaxFileLength)
      {
        _fileCount++;
        _fileLength = 0;
      }

      var filePath = Path.Combine(
        FileDir,
        FileName + (_fileCount > 0 ? "-" + _fileCount : "") + ".log");
      
      File.AppendAllText(filePath, _buffer);
      
      _buffer = "";
      _fileLength += _buffer.Length;
    }
  }
}