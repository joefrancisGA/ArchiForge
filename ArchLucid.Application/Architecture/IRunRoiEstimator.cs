using ArchLucid.Contracts.Architecture;

namespace ArchLucid.Application.Architecture;

/// <summary>Computes directional ROI-equivalent hour totals from <see cref="ArchitectureRunDetail"/>.</summary>
public interface IRunRoiEstimator
{
    RunRoiScorecardDto Estimate(ArchitectureRunDetail detail);
}
