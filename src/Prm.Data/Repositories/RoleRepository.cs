using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class RoleRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Role, int>(_dbContext), IRoleRepository;
