using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using util;
using VibeMe.Microservice.Repository;
using VibeME.DTOS;

namespace VibeME.Controllers
{
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository userRepository , IConfiguration config, ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _config = config;
            _logger = logger;

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dTO)
        {
            var user = await _userRepository.RegisterAsync(dTO.FirstName , dTO.LastName, dTO.Username, dTO.Email, dTO.Password);

            return Ok(new {user});


        }

        [HttpGet("users/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null) return NotFound();
            return Ok(new { user.Id, user.Username,user.FirstName, user.Email});
        }
        [HttpGet("user/{username}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);

                return user == null
                    ? NotFound($"Usuario con username {username} no encontrado")
                    : Ok(user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por username");
                return StatusCode(500, "Error interno del servidor");
            }
        }
        [HttpGet("Get/All/Users")]
        [Authorize]

        public async Task<IActionResult> GetAllUser()
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int loggedInUserId))
            {
                return Unauthorized("Usuario no autenticado correctamente");
            }

            var users = await _userRepository.GetAllAsync(loggedInUserId);
            return Ok(users);
        }
        [HttpGet("Get/All/UserStatusFollow")]
        [Authorize]
         
        public async Task<IActionResult> getAllUserWhiteStatusFollow()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int loggedInUserId))
            {
                return Unauthorized("Usuario no autenticado correctamente");
            }

            var users = await _userRepository.GetAllUsersWithFollowStatusAsync(loggedInUserId);
            return Ok(users);

        }


        [HttpGet("login")]
        public async Task<IActionResult> Login([FromQuery] string username, [FromQuery] string password)
        {
            var user = await _userRepository.LoginAsync(username, password);
            if (user == null)
                return Unauthorized("Usuario o contraseña incorrectos");

            var token = JwtHelper.GenerateToken(
                user,
                _config["Jwt:Key"],
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"]
            );

            return Ok(new { token, user.Id, user.Username, user.FirstName, user.LastName, user.Email });
        } 
        
    }

}
