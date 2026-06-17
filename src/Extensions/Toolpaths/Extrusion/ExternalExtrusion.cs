using Robots;
using static System.Math;
using CustomCommand = Robots.Commands.Custom;

namespace Extensions.Toolpaths.Extrusion;

public static class ExternalExtrusion
{
    static bool IsExtrusion(Target? target)
    {
        return target switch
        {
            { External: [not 0.0, ..] } => true,
            _ => false
        };
    }

    public static IReadOnlyList<int> FirstLayerIndices(IReadOnlyList<Target> targets)
    {
        List<int> indices = [];
        int index = 0;
        Target? prev = null;

        foreach (var target in targets)
        {
            if (!IsExtrusion(prev) && IsExtrusion(target))
                indices.Add(index);

            index++;
            prev = target;
        }

        return indices;
    }

    public static IToolpath AddExtruderCommands(IToolpath toolpath, double externalFactor, string? indMechanism = null)
    {
        ArgumentOutOfRangeException.ThrowIfZero(toolpath.Targets.Count, nameof(toolpath));

        var resetCommand = ResetCommand(toolpath.Targets.First());

        var outTargets = SetExternalWithVariable(toolpath.Targets);
        return new SimpleToolpath(outTargets);

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

            CustomCommand command = new("ResetExtruder", Manufacturers.ABB, initCode, declaration)
            {
                RunBefore = true
            };

            return command;
        }

        List<Target> SetExternalWithVariable(IReadOnlyList<Target> inTargets)
        {
            List<Target> outTargets = [];

            double totalDistance = 0;
            int count = 0;
            int i = 0;

            foreach (var target in inTargets)
            {
                double externalDistance = 0;

                if (target.External.Length > 0)
                    externalDistance = target.External[0];

                totalDistance += externalDistance;
                var current = target.WithExternal([totalDistance], ["motorValue"]);

                if (i == 0)
                    current = current.AppendCommand(resetCommand);

                if (externalDistance != 0)
                {
                    string sign = externalDistance < 0 ? "+" : "-";
                    string code = $"motorValue:=motorValue{sign}{Abs(externalDistance):0.000}*extrusionFactor;";
                    CustomCommand externalCommand = new($"SetExternal{count++}", Manufacturers.ABB, code)
                    {
                        RunBefore = true
                    };

                    current = current.AppendCommand(externalCommand);
                }

                outTargets.Add(current);
                i++;
            }

            return outTargets;
        }
    }
}
