namespace Prm.Common.Constants;

public static class AppConstants
{
    public static class Configuration
    {
        public const string DefaultConnection = "DefaultConnection";
        public const string JwtSection = "Jwt";
        public const string BootstrapAdminSection = "BootstrapAdmin";
    }

    public static class ErrorCodes
    {
        public const string SessionExpired = "SESSION_EXPIRED";
        public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string BadRequest = "BAD_REQUEST";
        public const string NotFound = "NOT_FOUND";
        public const string Conflict = "CONFLICT";
        public const string InternalError = "INTERNAL_ERROR";
    }

    public static class Messages
    {
        public const string SessionExpired = "Your session has expired. Please login again.";
        public const string InternalError = "An unexpected error occurred. Please try again later.";
        public const string DatabaseError = "A database error occurred while processing your request.";
        public const string ConcurrencyConflict =
            "The record was modified by another process. Please refresh and try again.";
        public const string JwtSecretMissing = "Jwt:Secret is missing from configuration.";
        public const string JwtConfigurationInvalid = "Jwt configuration is missing required values.";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "Invalid username or password.";
        public const string EmployeeProfileNotFound = "Employee profile not found. Contact Admin.";
        public const string RefreshTokenInvalidOrExpired = "Refresh token is invalid or expired.";
        public const string UserNotAuthenticated = "You must be logged in to change your password.";
        public const string PasswordChangeNotRequired = "A password change is not required for this account.";
        public const string PasswordChangeRequired = "You must change your password before continuing.";
        public const string PasswordsDoNotMatch = "New password and confirmation do not match.";
        public const string PasswordDoesNotMeetRequirements =
            "Password must be at least 8 characters and include one uppercase letter, one lowercase letter, one number, and one special character.";
        public const string NewPasswordMustDiffer = "New password must be different from your current password.";
    }

    public static class Employees
    {
        public const string NotFound = "Employee not found.";
        public const string UserNotFound = "User not found.";
        public const string UserInactive = "User account is inactive.";
        public const string InvalidRoleForEmployee =
            "Only users with the Employee role can have an employee profile.";
        public const string ProfileAlreadyExists = "This user already has an employee profile.";
        public const string AlreadyDeactivated = "Employee is already deactivated.";
        public const string DeactivatedSuccessfully = "Employee deactivated successfully.";
        public const string InvalidRoleForManagerAssignment =
            "Only users with the Employee role can be assigned to a manager.";
        public const string InvalidManagerUser =
            "Manager user must have the Manager role and be active.";
        public const string ManagerAssigned = "Manager assigned successfully.";
        public const string DepartmentAndDesignationRequired =
            "Department and designation are required when assigning a manager.";
        public const string DefaultDepartment = "Unassigned";
        public const string DefaultEmployeeDesignation = "Employee";
        public const string DefaultManagerDesignation = "Manager";
    }

    public static class Skills
    {
        public const string SkillNotFound = "Skill not found.";
        public const string EmployeeSkillNotFound = "Employee skill assignment not found.";
        public const string SkillAlreadyAssigned = "This skill is already assigned to the employee.";
        public const string InvalidProficiency = "Proficiency must be Beginner, Intermediate, or Advanced.";
        public const string InvalidCategory = "Category must be Backend, Frontend, DevOps, QA, or Other.";
        public const string SkillAdded = "Skill added successfully.";
        public const string SkillUpdated = "Skill proficiency updated successfully.";
        public const string SkillRemoved = "Skill removed successfully.";
    }

    public static class Users
    {
        public const string NotFound = "User not found.";
        public const string LookupRequired = "Either user ID or username must be provided.";
        public const string UsernameExists = "Username is already in use.";
        public const string EmailExists = "Email is already in use.";
        public const string InvalidRole = "Role must be Admin (1), Manager (2), or Employee (3).";
        public const string AlreadyActive = "User account is already active.";
        public const string AlreadyInactive = "User account is already inactive.";
        public const string CannotDeactivateLastAdmin = "Cannot deactivate last Active admin.";
    }

    public static class Projects
    {
        public const string NotFound = "Project not found.";
        public const string NameExists = "Project name is already in use.";
        public const string InvalidStatus = "Status must be Planned (1), Active (2), or On Hold (3).";
        public const string InvalidDateRange = "End date must be on or after start date.";
        public const string ManagerNotFound = "Manager not found or is not an active manager.";
    }

    public static class Milestones
    {
        public const string NotFound = "Milestone not found.";
        public const string InvalidStatus = "Status must be Not Started (1), In Progress (2), or Done (3).";
        public const string TitleExists = "A milestone with this title already exists for the project.";
        public const string InvalidDueDate = "Due date must be within the project start and end dates.";
        public const string StoryPointsExceedProjectTotal =
            "Milestone story points cannot exceed the project's total story points.";
    }

    public static class Http
    {
        public const string JsonContentType = "application/json";
        public const string BearerScheme = "Bearer";
    }

    public static class SystemConfiguration
    {
        public const int DefaultSchedulerIntervalMinutes = 4;
        public const int DefaultMaxWeeklyHours = 40;

        public const string NotFound = "System configuration not found.";
        public const string InvalidValue = "Invalid value.";
        public const string ValueUnchanged = "The value is the same as the current value.";
    }

    public static class Manager
    {
        public const string ProfileNotFound = "Manager user not found or is not active.";
        public const string ProjectNotOwned = "You can only manage allocations on projects you own.";
        public const string EmployeeNotFound = "Employee not found.";
        public const string EmployeeNotEligible = "Only active employees can be allocated to projects.";
    }

    public static class Allocations
    {
        public const string NotFound = "Allocation not found.";
        public const string AlreadyEnded = "Allocation has already ended.";
        public const string InvalidDateRange = "From date must be before to date.";
        public const string InvalidUtilization =
            "Utilization must be between 1 and 100 percent.";
        public const string InvalidFilter = "Invalid input. Enter an employee name or a project name.";
        public const string ExceedsMaxUtilization =
            "Total utilisation across overlapping allocations cannot exceed 100%.";
        public const string OverlappingAllocationOnProject =
            "Employee already has an overlapping allocation on this project for the selected period.";
        public const string ProjectNotAllocatable =
            "Project must be in ACTIVE or PLANNED status to allocate resources.";
        public const string AllocationDatesOutsideProject =
            "Allocation dates must be within the project start and end dates.";
        public const string CreatedSuccessfully = "Allocation created successfully.";
        public const string EndedSuccessfully = "Allocation ended successfully.";
    }

    public static class Timesheets
    {
        public const string EmployeeNotFound = "Employee profile not found.";
        public const string AlreadySubmitted = "A timesheet for this week has already been submitted.";
        public const string FutureWeekNotAllowed = "You cannot submit a timesheet for a future week.";
        public const string InvalidWeekStart = "Week start must be a Monday.";
        public const string ProjectNotAllocated = "You can only log hours for projects you are allocated to during that week.";
        public const string HoursExceedAllocation = "Hours for a project cannot exceed the expected hours for your allocation.";
        public const string TotalHoursExceedMax = "Total hours cannot exceed the configured maximum weekly hours.";
        public const string NoEntries = "At least one timesheet entry is required.";
        public const string DuplicateProjectInEntries = "Each project can only appear once in a timesheet submission.";
        public const string ActivityTagsRequired = "At least one activity tag is required for each entry with hours.";
        public const string InvalidActivityTag = "One or more activity tags are invalid.";
        public const string NotFound = "Timesheet not found for the selected week.";
        public const string EmployeeNotOnTeam = "This employee is not on your team.";
        public const string SubmittedSuccessfully = "Timesheet submitted successfully.";
    }
}
