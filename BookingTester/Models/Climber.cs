public class Climber
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public Climber(string name, string email, string password)
    {
        Name = name;
        Email = email;
        Password = password;
    }
}