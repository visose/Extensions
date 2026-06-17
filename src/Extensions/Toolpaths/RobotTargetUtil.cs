using Robots;
using Robots.Commands;
using Rhino.Geometry;

namespace Extensions.Toolpaths;

static class RobotTargetUtil
{
    public static Command? CombineCommands(params Command?[] commands)
    {
        var items = commands.Where(static command => command is not null).Cast<Command>().ToArray();

        return items.Length switch
        {
            0 => null,
            1 => items[0],
            _ => new Group(items)
        };
    }

    extension(Target target)
    {
        public Target WithCommand(Command? command)
        {
            return target switch
            {
                CartesianTarget cartesian => new CartesianTarget(
                    cartesian.Plane,
                    cartesian.Configuration,
                    cartesian.Motion,
                    cartesian.Tool,
                    cartesian.Speed,
                    cartesian.Zone,
                    command,
                    cartesian.Frame,
                    cartesian.External,
                    cartesian.ExternalCustom),
                JointTarget joint => new JointTarget(
                    joint.Joints,
                    joint.Tool,
                    joint.Speed,
                    joint.Zone,
                    command,
                    joint.Frame,
                    joint.External,
                    joint.ExternalCustom),
                _ => throw new ArgumentException($"Target type '{target.GetType().Name}' is not supported.", nameof(target))
            };
        }

        public Target AppendCommand(Command command)
        {
            var current = target.Command is null || target.Command == Command.Default
                ? command
                : new Group([target.Command, command]);

            return target.WithCommand(current);
        }

        public Target WithExternal(double[] external, string[]? externalCustom)
        {
            return target switch
            {
                CartesianTarget cartesian => new CartesianTarget(
                    cartesian.Plane,
                    cartesian.Configuration,
                    cartesian.Motion,
                    cartesian.Tool,
                    cartesian.Speed,
                    cartesian.Zone,
                    cartesian.Command,
                    cartesian.Frame,
                    external,
                    externalCustom),
                JointTarget joint => new JointTarget(
                    joint.Joints,
                    joint.Tool,
                    joint.Speed,
                    joint.Zone,
                    joint.Command,
                    joint.Frame,
                    external,
                    externalCustom),
                _ => throw new ArgumentException($"Target type '{target.GetType().Name}' is not supported.", nameof(target))
            };
        }
    }

    extension(CartesianTarget target)
    {
        public CartesianTarget WithPlane(Plane plane)
        {
            return new(
                plane,
                target.Configuration,
                target.Motion,
                target.Tool,
                target.Speed,
                target.Zone,
                target.Command,
                target.Frame,
                target.External,
                target.ExternalCustom);
        }

        public CartesianTarget WithPlaneSpeedCommand(Plane plane, Speed speed, Command? command)
        {
            return new(
                plane,
                target.Configuration,
                target.Motion,
                target.Tool,
                speed,
                target.Zone,
                command,
                target.Frame,
                target.External,
                target.ExternalCustom);
        }
    }
}
