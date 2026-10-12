using System;
using MySqlConnector;
using Godot;
using System.Threading.Tasks;

public static class MySqlManager
{
    private const string _ConnectionInfo = "Server=localhost;Port=3606;Database=game_db;UserID=root;Password=root;";

    public static async Task<bool> UserValidationAsync(string username, string passwordHash)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT COUNT(1) FROM users WHERE username = @username AND password_hash = @password LIMIT 1; ";

            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", passwordHash);

            object? result = await command.ExecuteScalarAsync();
            long count = Convert.ToInt64(result);

            return count > 0;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> UserRegisterAsync(string username, string passwordHash)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string insert = "INSERT INTO users (username, password_hash) VALUE (@username, @password);";

            await using var command = new MySqlCommand(insert, connection);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", passwordHash);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            bool result = rowsAffected > 0;
            return result;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> UsernameExists(string username)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT COUNT(1) FROM users WHERE username = @username LIMIT 1; ";

            await using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@username", username);

            object? result = await command.ExecuteScalarAsync();
            long count = Convert.ToInt64(result);
            return count > 0;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }
}