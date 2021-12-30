namespace Extensions.Document;

class Logger
{
    public static void Log(string message)
    {
        Rhino.RhinoApp.WriteLine(message);
    }
}
