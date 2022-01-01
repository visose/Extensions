using System.Drawing;

namespace Extensions.Grasshopper;

static class Util
{
    public static Bitmap GetIcon(string name)
    {
        var icon = $"Extensions.Grasshopper.Assets.Embed.{name}.png";
        var assembly = typeof(ExtensionsInfo).Assembly;
        using var stream = assembly.GetManifestResourceStream(icon);
        return new Bitmap(stream);
    }
}