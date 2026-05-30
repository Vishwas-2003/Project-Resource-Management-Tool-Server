using AutoMapper;
using Prm.Api.Models.Employees;
using Prm.Common.Models.Employees;

namespace Prm.Api.Profiles;

public class EmployeeApiProfile : Profile
{
    public EmployeeApiProfile()
    {
        CreateMap<EmployeeSummary, EmployeeListItemResponse>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.FullName));

        CreateMap<EmployeeListResult, GetEmployeesResponse>();
    }
}
