using Robots;

namespace Extensions.Toolpaths;

public abstract class TargetListToolpath : IToolpath
{
    readonly List<Target> _targets = [];

    public IReadOnlyList<Target> Targets => _targets;
    protected void AddTarget(Target target) => _targets.Add(target);
    protected int TargetCount => _targets.Count;
}
