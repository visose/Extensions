using System.Globalization;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Extensions.Grasshopper;

static class DataAccess
{
    extension(IGH_DataAccess DA)
    {
        public T Get<T>(int index)
        {
            T? value = default;
            return Required(DA.GetData(index, ref value), value);
        }

        public T Get<T>(string name)
        {
            T? value = default;
            return Required(DA.GetData(name, ref value), value);
        }

        public T Get<T>(int index, T fallback)
        {
            T value = fallback;
            return DA.GetData(index, ref value) && value is not null ? value : fallback;
        }

        public T? Maybe<T>(int index) where T : class
        {
            T? value = null;
            return DA.GetData(index, ref value) ? value : null;
        }

        public T? MaybeValue<T>(int index) where T : struct
        {
            T value = default;
            return DA.GetData(index, ref value) ? value : null;
        }

        public T[] List<T>(int index)
        {
            List<T> values = [];
            return DA.GetDataList(index, values)
                ? values.CheckedArray(index.ToString(CultureInfo.InvariantCulture))
                : throw new MissingInputException();
        }

        public T[] MaybeList<T>(int index)
        {
            List<T> values = [];
            return DA.GetDataList(index, values)
                ? values.CheckedArray(index.ToString(CultureInfo.InvariantCulture))
                : [];
        }

        public GH_Structure<T> Tree<T>(int index)
            where T : IGH_Goo
        {
            return DA.GetDataTree(index, out GH_Structure<T> tree) ? tree : throw new MissingInputException();
        }

        public List<List<Plane>> PlaneTree(int index)
        {
            var tree = DA.Tree<GH_Plane>(index);
            return [.. tree.Branches.Select(branch => branch.Select(static value => value.Value).ToList())];
        }

        public List<List<double>> NumberTree(int index)
        {
            var tree = DA.Tree<GH_Number>(index);
            return [.. tree.Branches.Select(branch => branch.Select(static value => value.Value).ToList())];
        }

        public void SetCurveTree(int index, IReadOnlyList<IReadOnlyList<Polyline>> polylines)
        {
            GH_Structure<GH_Curve> tree = [];

            for (int i = 0; i < polylines.Count; i++)
            {
                var path = DA.ParameterTargetPath(index).AppendElement(i);

                for (int j = 0; j < polylines[i].Count; j++)
                    tree.Append(new GH_Curve(polylines[i][j].ToNurbsCurve()), path);
            }

            DA.SetDataTree(index, tree);
        }
    }

    static T Required<T>(bool success, T? value) => success && value is not null ? value : throw new MissingInputException();

    extension<T>(List<T> values)
    {
        T[] CheckedArray(string input)
        {
            if (values.Any(value => value is null))
                throw new InvalidOperationException($"Input '{input}' contains null values.");

            return [.. values];
        }
    }
}
