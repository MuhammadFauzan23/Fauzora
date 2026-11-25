using Microsoft.AspNetCore.DataProtection;

namespace Fauzora.Services;

public sealed class SecureIdService(IDataProtectionProvider dataProtectionProvider)
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("Fauzora.PageId.v1");

    public string Protect(int id) => protector.Protect(id.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public bool TryUnprotect(string? token, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var value = protector.Unprotect(token);
            return int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out id)
                && id > 0;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }
}
