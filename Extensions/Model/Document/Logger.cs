using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Model.Document
{
    class Logger
    {
       public static void Log(string message)
        {
            Rhino.RhinoApp.WriteLine(message);
        }
    }
}
