using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Identity.DTOs.Requests;
    public class SignupRequestDto
    {

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Password is required")]
        public string Password {  get; set; }


        [Required(ErrorMessage = "First Name is required")]
        [Length(3,10)]

        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]

        public string LastName { get; set; }

        [Required(ErrorMessage = "Birth Date is required")]

        public DateOnly BirthDate { get; set; }

        [Required(ErrorMessage = "Gender is required")]

        public EnGender Gender { get; set; }

}
