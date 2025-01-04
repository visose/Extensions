using Robots;
using static System.Math;

namespace Extensions.Toolpaths.Extrusion;

public static class ExternalExtrusion
{
    static bool IsExtrusion(Target target)
    {
        if (target == null) return false;
        if (target.External.Length == 0) return false;
        if (target.External[0] == 0) return false;
        return true;
    }

    public static List<int> FirstLayerIndices(List<Target> targets)
    {
        var indices = new List<int>();
        int index = 0;
        Target prev = null;

        foreach (var target in targets)
        {
            if (!IsExtrusion(prev) && IsExtrusion(target))
                indices.Add(index);

            index++;
            prev = target;
        }

        return indices;
    }

    public static IToolpath AddExtruderCommands(IToolpath toolpath, double externalFactor, string indMechanism = null)
    {
        if (toolpath == null)
            return toolpath;

        var resetCommand = ResetCommand(toolpath.Targets.First());

        var outTargets = SetExternalWithVariable(toolpath.Targets);
        return toolpath.ShallowClone(outTargets);

        Command ResetCommand(Target refTarget)
        {
            string declaration = $@"VAR num motorValue:= 0;
PERS num extrusionFactor:= {externalFactor:0.000};
VAR robtarget current;
";
            string resetCode = $@"current:= CRobT(\Tool:= {refTarget.Tool.Name} \WObj:= {refTarget.Frame.Name});
EOffsSet current.extax;
motorValue:= 0;";

            string initCode;

            if (indMechanism != null)
            {
                string indCode = $@"IndReset {indMechanism},1 \RefNum:=0 \Short;";
                initCode = $"{indCode}\r\n{resetCode}";
            }
            else
            {
                initCode = resetCode;
            }

            var command = new Robots.Commands.Custom("ResetExtruder", Manufacturers.ABB, initCode, declaration)
            {
                RunBefore = true
            };

            return command;
        }

        List<Target> SetExternalWithVariable(IEnumerable<Target> inTargets)
        {
            var outTargets = new List<Target>();

            double totalDistance = 0;
            int count = 0;
            int i = 0;
            Target prev;

            foreach (var target in inTargets)
            {
                var current = target.ShallowClone();
                double externalDistance = 0;

                if (target.External.Length > 0)
                    externalDistance = target.External[0];

                totalDistance += externalDistance;
                current.External = [totalDistance];
                current.ExternalCustom = ["motorValue"];

                if (i == 0)
                    current.AppendCommand(resetCommand);

                if (externalDistance != 0)
                {
                    //if (!IsExtrusion(prev))
                    string sign = externalDistance < 0 ? "+" : "-";
                    string code = $"motorValue:=motorValue{sign}{Abs(externalDistance):0.000}*extrusionFactor;";
                    var externalCommand = new Robots.Commands.Custom($"SetExternal{count++}", Manufacturers.ABB, code)
                    {
                        RunBefore = true
                    };

                    current.AppendCommand(externalCommand);
                }

                outTargets.Add(current);
                prev = target;
                i++;
            }

            return outTargets;
        }
    }
}
