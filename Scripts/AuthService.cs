using System.Threading.Tasks;
using System;

public class AuthService
{
    public async Task<bool> AuthenticateAndSaveScore(string username, string passwordHash, int score, DateTime currentDate)
    {
        bool isAuthenticated = await MySqlManager.UserValidationAsync(username, passwordHash, currentDate);
        if (!isAuthenticated) { return false; }

        int? userId = await MySqlManager.GetUserId(username, passwordHash);
        if (userId == null) { return false; }

        return await MySqlManager.SaveScore((int)userId, score, currentDate);
    }

    public async Task<bool> Register(string username, string passwordHash, DateTime currentDate)
    {
        return await MySqlManager.UserRegisterAsync(username, passwordHash, currentDate);
    }

    public async Task<MySqlManager.UsernameLookup> LookupUsername(string username)
    {
        return await MySqlManager.LookupUsername(username);
    }
}