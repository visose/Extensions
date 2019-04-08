using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;
using Rhino.PlugIns;
using Grasshopper.Kernel;
using System.Linq;

[assembly: AssemblyDescription("Assorted components for Grasshopper")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("66d4e86f-ec84-40c9-8e32-8cf8875c3feb")]

[assembly: GH_Loading(GH_LoadingDemand.ForceDirect)]

namespace Extensions.View
{
    public class ExtensionsInfo : GH_AssemblyInfo
    {
        internal static bool IsRobotsInstalled;

        public ExtensionsInfo()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string folder = Path.GetDirectoryName(path);
            const string resName = "Robots.gha";
            var dllPath = Path.Combine(folder, resName);

            IsRobotsInstalled = File.Exists(dllPath);

            if (IsRobotsInstalled)
            {
                Assembly.LoadFile(dllPath);
            }
            else
            {
                Rhino.RhinoApp.WriteLine("Extensions plugin: Some components that require the Robots plugin will not be loaded.");
            }
        }

        public override string Name => "Extensions";
        public override Bitmap Icon => Properties.Resources.DCLLogo;
        public override string Description => Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public override Guid Id => new Guid("29035877-56b2-45cd-b65d-bf19a046d30b");
        public override string AuthorName => Assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
        public override string AuthorContact => "v.soler@ucl.ac.uk";
    }
}