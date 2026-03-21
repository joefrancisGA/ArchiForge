using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Comparison;

public interface IComparisonService
{
    ComparisonResult Compare(GoldenManifest baseManifest, GoldenManifest targetManifest);
}
