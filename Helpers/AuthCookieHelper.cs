using glint_backend.DTOs.Auth;

namespace glint_backend.Helpers;

public static class AuthCookieHelper
{
    public static void AppendAuthCookies(
        HttpResponse response,
        AuthResponse auth,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var accessName = config["Auth:AccessCookie"] ?? "glint_access";
        var refreshName = config["Auth:RefreshCookie"] ?? "glint_refresh";
        var accessMinutes = config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 15;
        var refreshDays = config.GetValue<int?>("Auth:RefreshTokenDays") ?? 7;

        var secure = ShouldUseSecureCookie(env, config);
        var sameSite = ParseSameSite(config["Auth:CookieSameSite"]);

        var accessOpts = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(accessMinutes),
        };

        var refreshOpts = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = sameSite,
            Path = "/",
            MaxAge = TimeSpan.FromDays(refreshDays),
        };

        response.Cookies.Append(accessName, auth.AccessToken, accessOpts);
        response.Cookies.Append(refreshName, auth.RefreshToken, refreshOpts);
    }

    public static void DeleteAuthCookies(HttpResponse response, IConfiguration config, IWebHostEnvironment env)
    {
        var accessName = config["Auth:AccessCookie"] ?? "glint_access";
        var refreshName = config["Auth:RefreshCookie"] ?? "glint_refresh";
        var secure = ShouldUseSecureCookie(env, config);
        var sameSite = ParseSameSite(config["Auth:CookieSameSite"]);

        void delete(string name)
        {
            response.Cookies.Delete(name, new CookieOptions
            {
                Path = "/",
                Secure = secure,
                SameSite = sameSite,
                HttpOnly = true,
            });
        }

        delete(accessName);
        delete(refreshName);
    }

    private static bool ShouldUseSecureCookie(IWebHostEnvironment env, IConfiguration config)
    {
        if (config.GetValue("Auth:CookieSecure", false))
            return true;
        return env.IsProduction();
    }

    private static SameSiteMode ParseSameSite(string? value)
    {
        if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
            return SameSiteMode.None;
        if (string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase))
            return SameSiteMode.Strict;
        return SameSiteMode.Lax;
    }
}
