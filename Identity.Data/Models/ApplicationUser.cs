using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;


namespace Identity.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }


    [MaxLength(10)]
    public string FirstName { get; set; }

    [MaxLength(10)]
    public string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public EnGender Gender { get; set; }

}
