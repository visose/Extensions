using System.Reflection;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper;

namespace Extensions.Grasshopper;

public class ExtensionsInfo : GH_AssemblyInfo
{
    internal static bool IsRobotsInstalled { get; private set; }

    public ExtensionsInfo()
    {
        var componentServer = Instances.ComponentServer;

        if (componentServer.Libraries.Any(l => l.Name == "Robots"))
        {
            IsRobotsInstalled = true;
            return;
        }

        componentServer.GHAFileLoaded += CheckLoadrobots;
    }

    void CheckLoadrobots(object sender, GH_GHALoadingEventArgs e)
    {
        if (e.Name != "Robots")
            return;

        IsRobotsInstalled = true;
        Instances.ComponentServer.GHAFileLoaded -= CheckLoadrobots;
    }

    public override string Name => GetInfo<AssemblyProductAttribute>().Product;
    public override string AssemblyVersion => GetInfo<AssemblyInformationalVersionAttribute>().InformationalVersion;
    public override Bitmap Icon => Properties.Resources.Cube;
    public override string Description => GetInfo<AssemblyDescriptionAttribute>().Description;
    public override GH_LibraryLicense License => GH_LibraryLicense.opensource;
    public override string AuthorName => GetCompany()[0];
    public override string AuthorContact => GetCompany()[1];
    public override Guid Id => new("29035877-56b2-45cd-b65d-bf19a046d30b");

    T GetInfo<T>() where T : Attribute
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<T>();
    }

    string[] GetCompany()
    {
        var company = GetInfo<AssemblyCompanyAttribute>().Company;
        return company.Split(new[] { " - " }, StringSplitOptions.None);
    }
}