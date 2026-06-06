using Prm.Common.Models.Manager;
using Prm.Data.Entities;

namespace Prm.Api.Services.Interfaces;

public interface IProjectHealthService
{
    Task<ProjectHealthEvaluation> Evaluate(Project project, CancellationToken cancellationToken = default);
}
