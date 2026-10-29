namespace MRRS.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";

    public static readonly string[] All = [Admin, Employee];

    public static bool IsKnown(string? role) =>
        role is Admin or Employee;
}
