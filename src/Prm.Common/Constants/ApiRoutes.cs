namespace Prm.Common.Constants;

public static class ApiRoutes
{
    public const string BaseApi = "api/[controller]";

    public static class Auth
    {
        public const string Login = "login";
        public const string Refresh = "refresh";
        public const string ChangePassword = "change-password";
    }

    public static class Employees
    {
        public const string Update = "{employeeId:int}";
        public const string GetEmployees = "get-employees";
        public const string AddEmployee = "add";
        public const string Deactivate = "{employeeId:int}/deactivate";
    }

    public static class Skills
    {
        public const string GetForEmployee = "employee/{employeeId:int}";
        public const string Add = "employee/{employeeId:int}";
        public const string Update = "employee/{employeeId:int}/{skillId:int}";
        public const string Remove = "employee/{employeeId:int}/{skillId:int}";
    }

    public static class Users
    {
        public const string GetUsers = "get-users";
        public const string Add = "add";
        public const string Reactivate = "{userId:int}/reactivate";
        public const string ResetPassword = "reset-password";
        public const string Deactivate = "deactivate";
    }

    public static class Projects
    {
        public const string GetProjects = "get-projects";
        public const string Add = "add";
        public const string Update = "{projectId:int}";
    }

    public static class Milestones
    {
        public const string GetByProject = "project/{projectId:int}";
        public const string Add = "project/{projectId:int}";
        public const string Update = "project/{projectId:int}/{milestoneId:int}";
    }

    public static class SystemConfiguration
    {
        public const string Update = "{configurationId:int}";
    }
}
