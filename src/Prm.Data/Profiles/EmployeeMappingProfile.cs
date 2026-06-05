using AutoMapper;
using Prm.Common.Models.Employees;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<Employee, EmployeeSummary>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.User.FullName));

        CreateMap<AddEmployeeRequest, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.EmployeeSkills, o => o.Ignore())
            .ForMember(d => d.Allocations, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore());

        CreateMap<UpdateEmployeeRequest, Employee>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.UserId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.User, o => o.Ignore())
            .ForMember(d => d.EmployeeSkills, o => o.Ignore())
            .ForMember(d => d.Allocations, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore());
    }
}
