using UnityEngine;

/// <summary>
/// Datos mínimos del usuario tras el alta (POST /api/users/) para llamadas posteriores (p. ej. Bartle).
/// </summary>
public static class UserSession
{
    private const string UserIdKey = "ludo_user_id";
    private const string TokenKey = "ludo_user_token";
    private const string NameKey = "ludo_user_name";

    public static void Save(int userId, string token, string displayName)
    {
        PlayerPrefs.SetInt(UserIdKey, userId);
        if (!string.IsNullOrEmpty(token))
        {
            PlayerPrefs.SetString(TokenKey, token);
        }

        if (!string.IsNullOrEmpty(displayName))
        {
            PlayerPrefs.SetString(NameKey, displayName);
        }

        PlayerPrefs.Save();
    }

    public static bool TryGetUserId(out int userId)
    {
        if (!PlayerPrefs.HasKey(UserIdKey))
        {
            userId = 0;
            return false;
        }

        userId = PlayerPrefs.GetInt(UserIdKey, 0);
        return userId > 0;
    }

    public static string GetToken()
    {
        return PlayerPrefs.GetString(TokenKey, string.Empty);
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(NameKey);
        PlayerPrefs.Save();
    }
}
