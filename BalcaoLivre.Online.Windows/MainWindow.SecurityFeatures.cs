using System.Security.Cryptography;
using System.Text;

namespace BalcaoLivre.Online.Windows;

public partial class MainWindow
{
    private const int PasswordHashIterations = 120_000;
    private const int PasswordSaltBytes = 16;
    private const int PasswordHashBytes = 32;

    private static void SetUserPassword(UserAccount user, string password)
    {
        var clean = (password ?? "").Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(user.EmployeeNumber))
        {
            var number = NormalizeStaffNumber(clean);
            if (!string.IsNullOrWhiteSpace(number))
            {
                user.EmployeeNumber = number;
            }
        }

        user.PinHash = CreatePasswordHash(clean);
        user.Pin = "";
    }

    private static bool VerifyUserPassword(UserAccount user, string password)
    {
        var clean = (password ?? "").Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(user.PinHash) && VerifyPasswordHash(user.PinHash, clean))
        {
            return true;
        }

        var legacyMatch = string.Equals(user.Pin, clean, StringComparison.Ordinal)
            || (string.IsNullOrWhiteSpace(user.Pin)
                && string.Equals(StaffNumber(user), NormalizeStaffNumber(clean), StringComparison.Ordinal));
        if (legacyMatch)
        {
            SetUserPassword(user, clean);
        }

        return legacyMatch;
    }

    private static string CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(PasswordSaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordHashIterations,
            HashAlgorithmName.SHA256,
            PasswordHashBytes);

        return string.Join("$",
            PasswordHashPrefix,
            PasswordHashIterations.ToString(Brazil),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    private static bool VerifyPasswordHash(string encoded, string password)
    {
        try
        {
            var parts = (encoded ?? "").Split('$');
            if (parts.Length != 4 || parts[0] != PasswordHashPrefix)
            {
                return false;
            }

            var iterations = int.Parse(parts[1], Brazil);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }

    private static void NormalizeRolePermissions(UserAccount user)
    {
        if (user.IsMaster || string.Equals(user.Role, "MASTER", StringComparison.OrdinalIgnoreCase))
        {
            user.IsMaster = true;
            user.CanTransfer = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanManageProducts = true;
            user.CanReports = true;
            user.CanCash = true;
            user.CanDelivery = true;
            user.CanInventory = true;
            user.CanKitchen = true;
            user.CanIFood = true;
            user.CanSettings = true;
            user.CanBackup = true;
            user.CanFiscal = true;
            user.CanDeliveryZones = true;
            user.CanCentralSync = true;
            return;
        }

        if (string.Equals(user.Role, "GERENTE", StringComparison.OrdinalIgnoreCase))
        {
            user.CanTransfer = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanManageProducts = true;
            user.CanReports = true;
            user.CanCash = true;
            user.CanDelivery = true;
            user.CanInventory = true;
            user.CanKitchen = true;
            user.CanIFood = true;
            user.CanSettings = true;
            user.CanBackup = true;
            user.CanDeliveryZones = true;
            user.CanCentralSync = true;
        }
        else if (string.Equals(user.Role, "CAIXA", StringComparison.OrdinalIgnoreCase))
        {
            user.CanCash = true;
            user.CanCancel = true;
            user.CanDiscount = true;
            user.CanDelivery = true;
        }
        else if (string.Equals(user.Role, "GARCOM", StringComparison.OrdinalIgnoreCase))
        {
            user.CanTransfer = true;
            user.CanDelivery = true;
        }
        else if (string.Equals(user.Role, "COZINHA", StringComparison.OrdinalIgnoreCase))
        {
            user.CanKitchen = true;
        }
    }
}
