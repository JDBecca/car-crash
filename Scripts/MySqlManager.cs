using System;
using MySqlConnector;
using Godot;
using System.Threading.Tasks;

public static class MySqlManager
{
    private const string _ConnectionInfo = "Server=localhost;Port=3606;Database=game_db;UserID=root;Password=root;";

    public static async Task<bool> UserValidationAsync(string username, string passwordHash, DateTime currentdate)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string update = "UPDATE users SET last_login = @currentdate where username = @username AND password_hash = @password;";

            await using var updateCommand = new MySqlCommand(update, connection);
            updateCommand.Parameters.AddWithValue("@username", username);
            updateCommand.Parameters.AddWithValue("@password", passwordHash);
            updateCommand.Parameters.AddWithValue("@currentdate", currentdate);

            int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
            bool result = rowsAffected > 0;

            return result;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> UserRegisterAsync(string username, string passwordHash, DateTime currentdate)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string insert = "INSERT INTO users (username, password_hash, creation_date, last_login) VALUES (@username, @password, @currentdate, @currentdate);";

            await using var insertCommand = new MySqlCommand(insert, connection);
            insertCommand.Parameters.AddWithValue("@username", username);
            insertCommand.Parameters.AddWithValue("@password", passwordHash);
            insertCommand.Parameters.AddWithValue("@currentdate", currentdate);

            int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
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

            await using var queryCommand = new MySqlCommand(query, connection);
            queryCommand.Parameters.AddWithValue("@username", username);

            object? result = await queryCommand.ExecuteScalarAsync();
            long count = Convert.ToInt64(result);

            return count > 0;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }

    public static async Task<int?> GetUserId(string username, string passwordHash)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT id FROM users WHERE username = @username AND password_hash = @password;";

            await using var queryCommand = new MySqlCommand(query, connection);
            queryCommand.Parameters.AddWithValue("@username", username);
            queryCommand.Parameters.AddWithValue("@password", passwordHash);

            object? result = await queryCommand.ExecuteScalarAsync();
            if (result is null or DBNull)
            {
                return null;
            }
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> SaveScore(int user_id, DateTime currentDate)
    {
        try
        {
            await using var connection = new MySqlDataSource(_ConnectionInfo).CreateConnection();
            await connection.OpenAsync();

            const string query = "SELECT COUNT(1) user_id FROM scoreboard WHERE user_id = @id;";

            await using var queryCommand = new MySqlCommand(query, connection);
            queryCommand.Parameters.AddWithValue("@id", user_id);

            object? queryResult = await queryCommand.ExecuteScalarAsync();
            long queryCount = Convert.ToInt64(queryResult);
            bool userHasSaves = queryCount > 0;
            if (userHasSaves)
            {
                const string getScores = "SELECT high_score FROM scoreboard WHERE user_id = @id;";
                await using var getScorescommand = new MySqlCommand(getScores, connection);
                getScorescommand.Parameters.AddWithValue("@id", user_id);

                object? savedScores = await getScorescommand.ExecuteScalarAsync();
                if (savedScores is null or DBNull) { return false; }
                int high_score = Convert.ToInt32(savedScores);

                if (ScoreManager.Instance.Score > high_score)
                {
                    const string update = "UPDATE scoreboard SET high_score = @score, high_score_date = @date where user_id = @id;";
                    await using var updateCommand = new MySqlCommand(update, connection);
                    updateCommand.Parameters.AddWithValue("@score", ScoreManager.Instance.Score);
                    updateCommand.Parameters.AddWithValue("@date", currentDate);
                    updateCommand.Parameters.AddWithValue("@id", user_id);

                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    bool result = rowsAffected > 0;
                    return result;
                }
                else
                {
                    const string update = "UPDATE scoreboard SET last_score = @score, last_score_date = @date where user_id = @id;";
                    await using var updateCommand = new MySqlCommand(update, connection);
                    updateCommand.Parameters.AddWithValue("@score", ScoreManager.Instance.Score);
                    updateCommand.Parameters.AddWithValue("@date", currentDate);
                    updateCommand.Parameters.AddWithValue("@id", user_id);

                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    bool result = rowsAffected > 0;
                    return result;
                }
            }
            else
            {
                const string insert = "INSERT INTO scoreboard (user_id, last_score, last_score_date, high_score, high_score_date) VALUES (@id, @score, @date, @score, @date);";
                await using var insertCommand = new MySqlCommand(insert, connection);
                insertCommand.Parameters.AddWithValue("@id", user_id);
                insertCommand.Parameters.AddWithValue("@score", ScoreManager.Instance.Score);
                insertCommand.Parameters.AddWithValue("@date", currentDate);
                int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                bool result = rowsAffected > 0;
                return result;
            }

        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error DB: {ex.Message}");
            return false;
        }
    }
}