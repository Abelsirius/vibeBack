using System.ComponentModel.DataAnnotations;

namespace VibeME.DTOS
{
    public class RegisterDTO
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El nombre no puede estar vacío.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El apellido no puede estar vacío.")]
        public string LastName { get; set; } = string.Empty;

    }
}
