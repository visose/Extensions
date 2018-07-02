using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;
using Rhino.PlugIns;
using Grasshopper.Kernel;

[assembly: AssemblyTitle("Extensions")]
[assembly: AssemblyDescription("Assorted components for Grasshopper")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Design Computation Lab - UCL")]
[assembly: AssemblyProduct("Extensions")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("66d4e86f-ec84-40c9-8e32-8cf8875c3feb")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: GH_Loading(GH_LoadingDemand.ForceDirect)]

namespace Extensions
{
    public class ExtensionsInfo : GH_AssemblyInfo
    {
        public ExtensionsInfo()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string folder = Path.GetDirectoryName(path);
            const string resName = "Robots.gha";
            var dllPath = Path.Combine(folder, resName);
            Assembly.LoadFile(dllPath);

            var assembly = Assembly.GetExecutingAssembly();
            using (var input = assembly.GetManifestResourceStream(resName))
            {
                if (input == null) return;
                Assembly.Load(StreamToBytes(input));
            }
        }

        static byte[] StreamToBytes(Stream input)
        {
            if (input == null) return null;

            var capacity = input.CanSeek ? (int)input.Length : 0;
            using (var output = new MemoryStream(capacity))
            {
                int readLength;
                var buffer = new byte[4096];

                do
                {
                    readLength = input.Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, readLength);
                }
                while (readLength != 0);
                return output.ToArray();
            }
        }

        public override string Name => "Extensions";
        public override Bitmap Icon => Properties.Resources.DCLLogo;
        public override string Description => "Assorted components for Grasshopper";
        public override Guid Id => new Guid("29035877-56b2-45cd-b65d-bf19a046d30b");
        public override string AuthorName => "Design Computation Lab - UCL";
        public override string AuthorContact => "v.soler@ucl.ac.uk";
    }
}