namespace RevenueMonsterLibrary.Model;

public class ClientCredentials
{
    public string accessToken { get; set; }
    public Error error { get; set; }
    public int expiresIn { get; set; }
    public string refreshToken { get; set; }
    public string tokenType { get; set; }
}