using CommandLine;

namespace SoundMap
{
  public class AppCommandLine
  {
    [Option('l', "last", Required = false, HelpText = "Open last opened file")]
    public bool Last { get; set; } = false;

    [Option('f', "file", Required = false, HelpText = "Open file")]
    public string FileName { get; set; } = null;
  }
}
