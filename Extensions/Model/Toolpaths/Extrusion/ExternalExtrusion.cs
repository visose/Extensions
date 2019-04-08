using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robots;
using Robots.Commands;
using static System.Math;

namespace Extensions.Model.Toolpaths.Extrusion
{
    public static class ExternalExtrusion
    {
        public static List<Target> AddExtruderCommands(List<Target> targets, double externalFactor)
        {
            if (targets == null || targets.Count == 0)
                return targets;

            var clonedTargets = targets.Select(t => t.ShallowClone()).ToList();
            InitTarget(clonedTargets[0], externalFactor);
            SetExternalWithVariable(clonedTargets);

            return clonedTargets;
        }

        static void InitTarget(Target home, double externalFactor)
        {
            string declaration = $@"VAR num motorValue:= 0;
PERS num extrusionFactor:= {externalFactor: 0.000};
VAR robtarget current;
";
            string initCode = $@"current:= CRobT(\Tool:= {home.Tool.Name} \WObj:= {home.Frame.Name});
EOffsSet current.extax;";

            var initCommand = new Robots.Commands.Custom("Init", abbDeclaration: declaration, abbCode: initCode);
            initCommand.RunBefore = true;

            var group = new Group();
            if (home.Command != null && home.Command != Command.Default)
                group.Add(home.Command);

            group.Add(initCommand);
            home.Command = group;
        }

        static void SetExternalWithVariable(List<Target> targets)
        {
            double totalDistance = 0;
            int count = 0;

            foreach (var target in targets)
            {
                if (target.External.Length == 0)
                {
                    target.External = new[] { 0.0 };
                }

                var externalDistance = target.External[0];
                totalDistance += externalDistance;
                target.External[0] = totalDistance;
                target.ExternalCustom = new[] { "motorValue" };

                var group = new Group();
                if (target.Command != null && target.Command != Command.Default)
                    group.Add(target.Command);


                if (externalDistance != 0)
                {
                    string sign = externalDistance < 0 ? "+" : "-";
                    string code = $"motorValue:=motorValue{sign}{Abs(externalDistance):0.000}*extrusionFactor;";
                    var externalCommand = new Robots.Commands.Custom($"SetExternal{count++}", abbCode: code);
                    externalCommand.RunBefore = true;
                    group.Add(externalCommand);
                    target.Command = group;
                }
            }
        }
    }
}
