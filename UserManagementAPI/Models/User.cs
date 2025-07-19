namespace UserManagementApi.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public int Userage { get; set; }
}
