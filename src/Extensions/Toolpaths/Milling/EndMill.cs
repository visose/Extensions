using Rhino.Geometry;
using Robots;

namespace Extensions.Toolpaths.Milling;

public class EndMill
{
    public enum Geometry { Flat, Ball }

    public double Diameter { get; set; }
    public double Length { get; set; }
    public double CutLength { get; set; }
    public Geometry Nose { get; set; }

    public Tool MakeTool(Tool spindle)
    {
        var name = $"{spindle.Name}_L{Length:0}mm_D{Diameter:0}mm";

        var tcp = spindle.Tcp;
        tcp.Translate(-tcp.Normal * Length);

        var endMillBrep = new Cylinder(new Circle(tcp, Diameter * 0.5), Length);
        var endMill = Mesh.CreateFromCylinder(endMillBrep, 1, 9);
        var mesh = spindle.Mesh.DuplicateMesh();
        mesh.Append(endMill);

        var tool = new Tool(tcp, name, spindle.Weight, spindle.Centroid, mesh);
        return tool;
    }
}
