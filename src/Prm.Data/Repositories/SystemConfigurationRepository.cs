using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class SystemConfigurationRepository(AppDbContext dbContext)
    : CrudBaseRepository<SystemConfiguration, int>(dbContext), ISystemConfigurationRepository
{
}
