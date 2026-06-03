using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class SystemConfigurationRepository(AppDbContext _dbContext)
    : CrudBaseRepository<SystemConfiguration, int>(_dbContext), ISystemConfigurationRepository
{
}
