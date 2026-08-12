namespace AMS.Domain.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string President = "President";
    public const string Owner = "Owner";
    public const string Tenant = "Tenant";
    public const string Staff = "Staff";
    public const string User = "User";

    public const string OwnerOrSuperAdmin = Owner + "," + SuperAdmin;
    public const string PresidentOrSuperAdmin = President + "," + SuperAdmin;
    public const string OwnerOrPresidentOrSuperAdmin = Owner + "," + President + "," + SuperAdmin;
    public const string StaffOrOwnerOrPresidentOrSuperAdmin = Staff + "," + Owner + "," + President + "," + SuperAdmin;
}
