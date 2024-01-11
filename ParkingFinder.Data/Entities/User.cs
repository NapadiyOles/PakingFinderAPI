namespace ParkingFinder.Data.Entities;

public class User
{
    public User(string name, string email, string role, string password)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        Role = role;
        Password = password;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string Password { get; set; }
}