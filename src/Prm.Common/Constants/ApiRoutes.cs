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
        public const string Update = "{employeeUserId:int}";
        public const string GetEmployees = "get-employees";
        public const string AddEmployee = "add";
        public const string AssignManager = "assign-manager";
        public const string Deactivate = "{employeeUserId:int}/deactivate";
        public const string GetDetail = "{employeeUserId:int}";
        public const string GetUtilization = "{employeeUserId:int}/utilization";
    }

    public static class Skills
    {
        public const string GetForEmployee = "employee/{employeeUserId:int}";
        public const string Add = "employee/{employeeUserId:int}";
        public const string Update = "employee/{employeeUserId:int}/{skillId:int}";
        public const string Remove = "employee/{employeeUserId:int}/{skillId:int}";
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
        public const string MyProjects = "my-projects";
        public const string GetDetail = "{projectId:int}";
    }

    public static class Allocations
    {
        public const string Create = "";
        public const string End = "{allocationId:int}/end";
        public const string GetByProject = "project/{projectId:int}";
        public const string GetActive = "active";
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

    public static class Manager
    {
        public const string ResourceDashboard = "resource-dashboard";
    }

    public static class Timesheets
    {
        public const string ActivityTags = "activity-tags";
        public const string Reminder = "reminder";
        public const string WeekAllocations = "week-allocations";
        public const string Submit = "submit";
        public const string MyTimesheets = "my-timesheets";
        public const string MyTimesheetDetail = "my-timesheets/{weekStart}";
        public const string MyAllocations = "my-allocations";
        public const string Team = "team";
        public const string TeamEmployeeDetail = "team/{employeeUserId:int}";
    }
}
