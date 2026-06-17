using System.Drawing;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Extensions.Grasshopper;

public abstract class Goo<T, TGoo> : GH_Goo<T>
    where TGoo : Goo<T, TGoo>, new()
{
    readonly string _typeName;

    protected Goo(string typeName, T? initialValue = default)
    {
        _typeName = typeName;

        if (initialValue is not null)
            SetValue(initialValue);
    }

    public override bool IsValid => Value is not null;
    public override string TypeName => _typeName;
    public override string TypeDescription => _typeName;
    protected string NullText => $"Null {TypeName}";

    public override IGH_Goo Duplicate()
    {
        TGoo goo = new();

        if (Value is not null)
            goo.SetValue(DuplicateValue(Value));

        return goo;
    }

    public override string ToString() => Value?.ToString() ?? NullText;
    public override object? ScriptVariable() => Value;

    public override bool CastFrom(object source)
    {
        if (source is not T value)
            return false;

        SetValue(value);
        return true;
    }

    public override bool CastTo<TCast>(ref TCast target)
    {
        if (Value is null || !typeof(TCast).IsAssignableFrom(typeof(T)))
            return false;

        target = (TCast)(object)Value;
        return true;
    }

    internal void SetValue(T value) => Value = Validate(value);
    protected virtual T Validate(T value) => value;
    protected virtual T DuplicateValue(T value) => value;
}

public abstract class Param<T, TGoo>(
    string nickname,
    string description,
    string id,
    string icon,
    GH_Exposure exposure = GH_Exposure.primary)
    : GH_Param<TGoo>($"{nickname} Parameter", nickname, description, "Extensions", "Parameters", GH_ParamAccess.item)
    where TGoo : Goo<T, TGoo>, new()
{
    public override GH_Exposure Exposure => exposure;
    public override Guid ComponentGuid => new(id);
    protected override Bitmap Icon => Util.GetIcon(icon);

    protected override TGoo PreferredCast(object data) =>
        data is T value ? New(value) : base.PreferredCast(data);

    internal static TGoo New(T value)
    {
        TGoo goo = new();
        goo.SetValue(value);
        return goo;
    }
}

public abstract class PreviewParam<T, TGoo>(
    string nickname,
    string description,
    string id,
    string icon,
    GH_Exposure exposure = GH_Exposure.primary)
    : Param<T, TGoo>(nickname, description, id, icon, exposure),
    IGH_PreviewObject
    where TGoo : Goo<T, TGoo>, IGH_PreviewData, new()
{
    public bool Hidden { get; set; }
    public bool IsPreviewCapable => true;
    public BoundingBox ClippingBox => Preview_ComputeClippingBox();
    public void DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    public void DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
}
