using System.Reflection;
using System.Drawing;
using Grasshopper;

namespace Extensions.Grasshopper;

public class ExtensionsInfo : GH_AssemblyInfo
{
    internal static bool IsRobotsInstalled { get; private set; }

    public ExtensionsInfo()
    {
        try
        {
            foreach (var folder in Folders.AssemblyFolders)
            {
                if (!Directory.Exists(folder.Folder))
                    continue;

                var robots = Directory.EnumerateFiles(folder.Folder, "Robots.gha", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (robots is null)
                    continue;

                Assembly.LoadFrom(robots);
                IsRobotsInstalled = true;
                return;
            }
        }
        catch
        {
            IsRobotsInstalled = false;
        }
    }

    public override string Name => GetInfo<AssemblyProductAttribute>().Product;
    public override string AssemblyVersion => GetInfo<AssemblyInformationalVersionAttribute>().InformationalVersion;
    public override Bitmap Icon => Util.GetIcon("Cube");
    public override string Description => GetInfo<AssemblyDescriptionAttribute>().Description;
    public override GH_LibraryLicense License => GH_LibraryLicense.opensource;
    public override string AuthorName => GetCompany()[0];
    public override string AuthorContact => GetCompany()[1];
    public override Guid Id => new("29035877-56b2-45cd-b65d-bf19a046d30b");

    T GetInfo<T>() where T : Attribute
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<T>()
            ?? throw new InvalidOperationException($"Missing required assembly attribute {typeof(T).Name}.");
    }

    string[] GetCompany()
    {
        var company = GetInfo<AssemblyCompanyAttribute>().Company;
        return company.Split([" - "], StringSplitOptions.None);
    }
}
