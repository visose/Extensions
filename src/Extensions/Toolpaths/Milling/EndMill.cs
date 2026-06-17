using Rhino.Geometry;
using Robots;

namespace Extensions.Toolpaths.Milling;

public sealed class EndMill
{
    public enum Geometry { Flat, Ball }

    public double Diameter { get; init; }
    public double Length { get; init; }
    public double CutLength { get; init; }
    public Geometry Nose { get; init; }

    public Tool MakeTool(Tool spindle)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Diameter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Length);

        var name = $"{spindle.Name}_L{Length:0}mm_D{Diameter:0}mm";

        var tcp = spindle.Tcp;
        tcp.Translate(-tcp.Normal * Length);

        Cylinder endMillBrep = new(new(tcp, Diameter * 0.5), Length);
        var endMill = Mesh.CreateFromCylinder(endMillBrep, 1, 9);
        var mesh = spindle.Mesh.DuplicateMesh();
        mesh.Append(endMill);

        Tool tool = new(tcp, name, spindle.Weight, spindle.Centroid, mesh);
        return tool;
    }
}
