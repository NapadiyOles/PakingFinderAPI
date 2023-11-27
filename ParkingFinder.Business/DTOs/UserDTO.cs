namespace ParkingFinder.Business.DTOs;

public record UserDTO
{
    public UserDTO(string name, string password, string email)
    {
        Name = name;
        Email = email;
        Password = password;
    }

    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}