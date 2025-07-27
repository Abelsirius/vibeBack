using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VibeMe.Microservice.Repository;
using VibeMe.Microservice.Repository.DTOS;
using VibeMeMicroservice.Infraestructure;

namespace VibeME.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MediaController:ControllerBase
    {

        private readonly IMediaRepository _mediaService;

        public MediaController(IMediaRepository mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        [Authorize]
        [RequestSizeLimit(1073741824)]
        public async Task<IActionResult> UploadMedia([FromForm] MediaUploadDto mediaDto)
            {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _mediaService.UploadMediaAsync(userId, mediaDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

        }

        [HttpGet("my-media")]
        [Authorize]
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        public async Task<IActionResult> GetMyMedia()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var media = await _mediaService.GetUserMediaAsync(userId);
            return Ok(media);
        }
        
        [HttpGet("my-media/{iduser:int}")]
        [Authorize]
        [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
        public async Task<IActionResult> GetMediaXId(int iduser)
        {
            var media = await _mediaService.GetUserMediaAsync(iduser);
            return Ok(media);
        }

        [HttpGet("{mediaId}")]

        public async Task<IActionResult> GetMedia(int mediaId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var media = await _mediaService.GetMediaAsync(mediaId, userId);

            if (media == null)
                return NotFound();

            return Ok(media);
        }

        [HttpDelete("{mediaId}")]
        public async Task<IActionResult> DeleteMedia(int mediaId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _mediaService.DeleteMediaAsync(mediaId, userId);

            if (!result)
                return NotFound();

            return NoContent();
        }


    }
}
