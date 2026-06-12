namespace Prm.Common.Constants;

public static class AppConstants
{
    public static class Configuration
    {
        public const string DefaultConnection = "DefaultConnection";
        public const string JwtSection = "Jwt";
        public const string BootstrapAdminSection = "BootstrapAdmin";
        public const string BrevoSection = "Brevo";
        public const string AiSection = "Ai";
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
        public const string BrevoConfigurationInvalid =
            "Brevo SMTP configuration requires SmtpLogin, SmtpKey, and SenderEmail when email notifications are enabled.";
    }

    public static class Email
    {
        public const string BrandName = "PRM";
        public const string FooterSignature = "PRM Admin";
        public const string FooterAutomatedNotice =
            "This is an automated notification from the Project & Resource Management system.";
        public const string AiDisclaimer =
            "AI-generated sections should be verified before making allocation decisions.";

        public const string SendFailed = "Failed to send email notification.";
        public const string Greeting = "Hi {0},";
        public const string WeekRange = "{0:yyyy-MM-dd} to {1:yyyy-MM-dd}";

        public const string RiskAlertSubject = "PRM Alert: Project \"{0}\" is At Risk";
        public const string RiskAlertTitle = "Project At Risk Alert";
        public const string RiskAlertIntro =
            "The following project under your management has been flagged as <strong>At Risk</strong>.";
        public const string SectionProjectDetails = "Project Details";
        public const string SectionHealthStatus = "Health Status";
        public const string SectionKeyMilestones = "Key Milestones";
        public const string SectionRiskFlags = "Risk Flags";
        public const string SectionAiRiskSummary = "AI Risk Summary";
        public const string SectionSuggestedHelp = "Suggested Help";
        public const string LabelName = "Name";
        public const string LabelManager = "Manager";
        public const string LabelStatus = "Status";
        public const string LabelPeriod = "Period";
        public const string NoOpenMilestones = "No open milestones found.";
        public const string NoRiskFlags = "No risk flags recorded.";
        public const string RecommendedBenchAllocations = "Recommended bench allocations:";
        public const string NoBenchResourcesSuggested = "No bench resources were suggested for this project.";
        public const string AiRiskSummaryUnavailable =
            "AI risk summary is temporarily unavailable. Please review the risk flags below.";
        public const string AiTeamSuggestionsUnavailable =
            "AI team suggestions are temporarily unavailable. Review bench resources in the system.";
        public const string HealthStatusAtRisk = "At Risk";
        public const string HealthStatusNeedsAttention = "Needs Attention";
        public const string HealthStatusOnTrack = "On Track";
        public const string MilestoneOverdueSuffix = " (overdue)";

        public const string TimesheetReminderMondaySubject =
            "PRM Reminder: Submit your timesheet for week starting {0:yyyy-MM-dd}";
        public const string TimesheetReminderTuesdaySubject =
            "PRM Urgent: Timesheet still missing for week starting {0:yyyy-MM-dd}";
        public const string TimesheetBlockedResourceSubject =
            "PRM Notice: Timesheet access blocked for week starting {0:yyyy-MM-dd}";
        public const string TimesheetBlockedManagerSubject =
            "PRM Notice: {0}'s timesheet access blocked for week starting {1:yyyy-MM-dd}";
        public const string TimesheetMondayTitle = "Timesheet Reminder";
        public const string TimesheetTuesdayTitle = "Timesheet Reminder — Action Required";
        public const string TimesheetBlockedResourceTitle = "Timesheet Access Blocked";
        public const string TimesheetBlockedManagerTitle = "Employee Timesheet Access Blocked";
        public const string TimesheetMondayBody =
            "Our records show that your timesheet for the week <strong>{0}</strong> has not been submitted yet.";
        public const string TimesheetMondayCallToAction =
            "Please log in to PRM and submit your timesheet as soon as possible.";
        public const string TimesheetTuesdayBody =
            "Your timesheet for the week <strong>{0}</strong> is still missing.";
        public const string TimesheetTuesdayWarning =
            "<strong>Important:</strong> If it is not submitted by end of day Tuesday, your timesheet access for this week will be blocked on Wednesday.";
        public const string TimesheetTuesdayCallToAction =
            "Please submit your timesheet in PRM immediately to avoid access restrictions.";
        public const string TimesheetBlockedResourceBody =
            "Your timesheet for the week <strong>{0}</strong> was not submitted by the deadline.";
        public const string TimesheetBlockedResourceNotice =
            "<strong>Your timesheet access for this week is now blocked.</strong>";
        public const string TimesheetBlockedResourceReason =
            "Reason: Missing timesheet submission after Monday and Tuesday reminders.";
        public const string TimesheetBlockedResourceAction =
            "Contact your manager to restore access. Once access is allowed, you may submit the timesheet for that week.";
        public const string TimesheetBlockedManagerBody =
            "<strong>{0}</strong> did not submit a timesheet for the week <strong>{1}</strong>.";
        public const string TimesheetBlockedManagerNotice =
            "<strong>The employee's timesheet access for that week is now blocked.</strong>";
        public const string TimesheetBlockedManagerReason =
            "Reason: Missing timesheet submission after automated Monday and Tuesday reminders.";
        public const string TimesheetBlockedManagerAction =
            "You can restore access in PRM when the employee is ready to submit the overdue timesheet.";

        public const string TimesheetMondayTextBody =
            "Your timesheet for {0} is still missing. Please submit it in PRM as soon as possible.";
        public const string TimesheetTuesdayTextBody =
            "Your timesheet for {0} is still missing. If it is not submitted by end of day Tuesday, your timesheet access for this week will be blocked on Wednesday.";
        public const string TimesheetBlockedResourceTextBody =
            "Your timesheet access for {0} is blocked because the timesheet was not submitted. Contact your manager to restore access.";
        public const string TimesheetBlockedManagerTextBody =
            "{0} did not submit a timesheet for {1}. Their timesheet access for that week is blocked. Restore access in PRM when appropriate.";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "Invalid username or password.";
        public const string InactiveUser = "The user is inactive, please contact Admin.";
        public const string ResourceProfileNotFound = "Employee profile not found. Contact Admin.";
        public const string RefreshTokenInvalidOrExpired = "Refresh token is invalid or expired.";
        public const string UserNotAuthenticated = "You must be logged in to change your password.";
        public const string PasswordChangeNotRequired = "A password change is not required for this account.";
        public const string PasswordChangeRequired = "You must change your password before continuing.";
        public const string PasswordsDoNotMatch = "New password and confirmation do not match.";
        public const string PasswordDoesNotMeetRequirements =
            "Password must be at least 8 characters and include one uppercase letter, one lowercase letter, one number, and one special character.";
        public const string NewPasswordMustDiffer = "New password must be different from your current password.";
    }

    public static class Resources
    {
        public const string NotFound = "Resource not found.";
        public const string UserNotFound = "User not found.";
        public const string UserInactive = "User account is inactive.";
        public const string InvalidRoleForEmployee =
            "Only users with the Employee role can have an employee profile.";
        public const string ProfileAlreadyExists = "This user already has an employee profile.";
        public const string AlreadyDeactivated = "Resource is already deactivated.";
        public const string DeactivatedSuccessfully = "Resource deactivated successfully.";
        public const string InvalidRoleForManagerAssignment =
            "Only users with the Employee role can be assigned to a manager.";
        public const string InvalidManagerUser =
            "Manager user must have the Manager role and be active.";
        public const string ManagerAssigned = "Manager assigned successfully.";
        public const string DepartmentAndDesignationRequired =
            "Department and designation are required when assigning a manager.";
        public const string DefaultDepartment = "Unassigned";
        public const string DefaultResourceDesignation = "Employee";
        public const string DefaultManagerDesignation = "Manager";
    }

    public static class Skills
    {
        public const string SkillNotFound = "Skill not found.";
        public const string ResourceSkillNotFound = "Resource skill assignment not found.";
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
        public const string PastDateNotAllowed = "Project dates cannot be in the past.";
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
        public const string ResourceNotFound = "Resource not found.";
        public const string ResourceNotEligible = "Only active employees can be allocated to projects.";
        public const string ResourceNotUnderManager =
            "You can only allocate resources assigned to you.";
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
            "Resource already has an overlapping allocation on this project for the selected period.";
        public const string ProjectNotAllocatable =
            "Project must be in ACTIVE or PLANNED status to allocate resources.";
        public const string AllocationDatesOutsideProject =
            "Allocation dates must be within the project start and end dates.";
        public const string AllocationDatesBeforeResourceCreated =
            "Allocation dates cannot be before the employee account was created.";
        public const string PastDateNotAllowed = "Allocation dates cannot be in the past.";
        public const string CreatedSuccessfully = "Allocation created successfully.";
        public const string EndedSuccessfully = "Allocation ended successfully.";
    }

    public static class Timesheets
    {
        public const string ResourceNotFound = "Employee profile not found.";
        public const string AlreadySubmitted = "A timesheet for this week has already been submitted.";
        public const string FutureWeekNotAllowed = "You cannot submit a timesheet for a future week.";
        public const string WeekBeforeResourceCreated =
            "You cannot submit a timesheet for a week before your account was created.";
        public const string InvalidWeekStart = "Week start must be a Monday.";
        public const string ProjectNotAllocated = "You can only log hours for projects you are allocated to during that week.";
        public const string HoursExceedAllocation = "Hours for a project cannot exceed the expected hours for your allocation.";
        public const string TotalHoursExceedMax = "Total hours cannot exceed the configured maximum weekly hours.";
        public const string NoEntries = "At least one timesheet entry is required.";
        public const string DuplicateProjectInEntries = "Each project can only appear once in a timesheet submission.";
        public const string ActivityTagsRequired = "At least one activity tag is required for each entry with hours.";
        public const string InvalidActivityTag = "One or more activity tags are invalid.";
        public const string NotFound = "Timesheet not found for the selected week.";
        public const string ResourceNotOnTeam = "This resource is not on your team.";
        public const string SubmittedSuccessfully = "Timesheet submitted successfully.";
        public const string AccessBlocked =
            "Timesheet access for this week is blocked. Contact your manager to restore access.";
        public const string AccessAlreadyAllowed = "Timesheet access for this week is already allowed.";
        public const string AccessRestoreInvalidState =
            "Only blocked timesheets can be restored by a manager.";
        public const string AccessRestoredSuccessfully = "Timesheet access restored successfully.";
    }
}
