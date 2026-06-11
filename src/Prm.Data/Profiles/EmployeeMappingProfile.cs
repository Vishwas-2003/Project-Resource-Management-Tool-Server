using AutoMapper;
using Prm.Common.Constants;
using Prm.Common.Models.Employees;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<User, EmployeeSummary>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FullName))
            .ForMember(d => d.Department, o => o.MapFrom(s => s.Department))
            .ForMember(d => d.Status, o => o.MapFrom(s =>
                s.ResourceStatusHistories
                    .Where(history => history.EffectiveToUtc == null)
                    .OrderByDescending(history => history.EffectiveFromUtc)
                    .Select(history => history.ResourceStatusType.Name)
                    .FirstOrDefault()));

        CreateMap<UpdateEmployeeRequest, User>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.RoleId, o => o.Ignore())
            .ForMember(d => d.FullName, o => o.Ignore())
            .ForMember(d => d.Username, o => o.Ignore())
            .ForMember(d => d.Email, o => o.Ignore())
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore())
            .ForMember(d => d.PasswordExpiryTime, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore())
            .ForMember(d => d.RefreshToken, o => o.Ignore())
            .ForMember(d => d.UserSkills, o => o.Ignore())
            .ForMember(d => d.ManagerHistories, o => o.Ignore())
            .ForMember(d => d.SubordinateManagerHistories, o => o.Ignore())
            .ForMember(d => d.ResourceStatusHistories, o => o.Ignore())
            .ForMember(d => d.ManagedProjects, o => o.Ignore())
            .ForMember(d => d.Allocations, o => o.Ignore())
            .ForMember(d => d.Timesheets, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore());
    }
}
