using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string Title { get; set; } = string.Empty;          
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string ProfileImagePath { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;       

    public string NidNo { get; set; } = string.Empty;          
    public string VisaNo { get; set; } = string.Empty;         
    public string PassportNo { get; set; } = string.Empty;     
}

