using System.Reflection;

namespace Mma.Helpers;

public static class BuildHelper
{
   
    public static string GetExecutablePath()
    {
        string executablePath = Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(executablePath)!;
    }

    public static string GetSetName(string componentName) =>
       componentName.EndsWith("s") ? $"{componentName}es" :
       componentName.EndsWith("y") ? $"{componentName.TrimEnd('y')}ies" :
       $"{componentName}s";

   
    
}
