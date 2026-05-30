using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class RoleRepository(AppDbContext dbContext)
    : CrudBaseRepository<Role, int>(dbContext), IRoleRepository;
