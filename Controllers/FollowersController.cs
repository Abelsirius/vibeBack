using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VibeMe.Microservice.Entities;
using VibeMe.Microservice.Repository;
using VibeME.DTOS;

namespace VibeME.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowersController : ControllerBase
    {
        private readonly IFollowerRepository _followerService;

        public FollowersController(IFollowerRepository followerService)
        {
            _followerService = followerService;
        }

        [HttpPost("follow/{userIdToFollow}")]
        [Authorize]
        public async Task<IActionResult> FollowUser(int userIdToFollow)
        {
            // Obtener el ID del usuario de forma segura
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int followerId))
            {
                return Unauthorized("Usuario no autenticado correctamente");
            }

            if (followerId == userIdToFollow)
            {
                return BadRequest("No puedes seguirte a ti mismo");
            }

            try
            {
                var result = await _followerService.FollowUserAsync(followerId, userIdToFollow);
                return result ? Ok() : BadRequest("No se pudo completar la acción");
            }
            catch (Exception ex)
            {
                // Loggear el error si es necesario
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("followers")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetFollowers()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var followers = await _followerService.GetFollowersAsync(userId);
            return Ok(followers);
        }
        [HttpGet("followers/{id}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetFollowersXId(int id)
        {
            var followers = await _followerService.GetFollowersAsync(id);
            return Ok(followers);
        }
        [HttpPost("unFollow/{id}")]
        [Authorize]
        public async Task<IActionResult> unFollow(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var unFollowUser = await _followerService.UnfollowUserAsync(userId,id);
            return Ok(unFollowUser);

        }
        
        [HttpGet("followings/{id}")]

        public async Task<IActionResult> GetFollowingCount(int id)
        {
            var followingsUser = await _followerService.GetFollowingCountAsync(id);

            return Ok(followingsUser);
        }

        [HttpGet("followingsUsers/{id}")]

        public async Task<IActionResult> GetFollowing(int id)
        {
            var loggedUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var followings = await _followerService.GetFollowingAsync(id, loggedUserId);

            return Ok(followings);
        }

        [HttpGet("mutualFollow/{id}")]

        public async Task<IActionResult> GetMurualFollow(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized();
                }

                if (!int.TryParse(userIdClaim, out var loggedUserId) || loggedUserId <= 0)
                {
                    return BadRequest("ID de usuario inválido");
                }

                if (id <= 0)
                {
                    return BadRequest("ID de usuario objetivo inválido");
                }

                var mutuals = await _followerService.GetMutualFollowersAsync(id, loggedUserId);
                return Ok(mutuals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno del servidor");
            }
        }


        // Implementar endpoints similares para unfollow, following, etc.
    }
}
