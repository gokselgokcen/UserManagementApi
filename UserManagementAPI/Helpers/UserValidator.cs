using UserManagementApi.Models;

namespace UserManagementApi.Helpers;

public static class UserValidator
{
    public static bool IsValidUser(User user, out string error)
    {
        error = "";

        if (user is null)
        {
            error = "User data is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            error = "Username is required.";
            return false;
        }

        if (user.Userage < 0 || user.Userage > 120)
        {
            error = "Userage must be between 0 and 120.";
            return false;
        }

        return true;
    }
}
