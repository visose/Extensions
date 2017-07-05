using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;
using Rhino.PlugIns;
using Grasshopper.Kernel;


// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Extensions")]
[assembly: AssemblyDescription("Extensions for Grasshopper")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Design Computation Lab - UCL")]
[assembly: AssemblyProduct("Extensions")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("66d4e86f-ec84-40c9-8e32-8cf8875c3feb")] // This will also be the Guid of the Rhino plug-in

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Extensions
{
    public class ExtensionsInfo : GH_AssemblyInfo
    {
        public ExtensionsInfo()
        {
            var dllPath = Path.Combine(Directory.GetCurrentDirectory(), "Robots.gha");
            Assembly.LoadFile(dllPath);
        }

        public override string Name => "Extensions";
        public override Bitmap Icon => Properties.Resources.DCLLogo;
        public override string Description => "Extensions for Grasshopper";
        public override Guid Id => new Guid("29035877-56b2-45cd-b65d-bf19a046d30b");
        public override string AuthorName => "Design Computation Lab - UCL";
        public override string AuthorContact => "v.soler@ucl.ac.uk";
    }
}