namespace ParkingFinder.API.Models;

public class UserTokenModel
{
    public UserTokenModel(string guid, string name, string email, string token)
    {
        Guid = guid; 
        Name = name;
        Email = email;
        Token = token;
    }

    public string Guid { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
}