namespace ParkingFinder.API.Models;

public class UserOutputModel
{
    public UserOutputModel(string guid, string name, string email)
    {
        Name = name;
        Email = email;
        Guid = guid;
    }

    public string Guid { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}