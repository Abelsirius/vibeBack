using Microsoft.AspNetCore.Mvc;
using VibeMe.Microservice.Entities;
using VibeMe.Microservice.Repository;
using VibeMe.Microservice.Repository.DTOS;
using VibeME.DTOS;

namespace VibeME.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentRepository _commentRepository;

        public CommentsController(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        // GET: api/comments/media/5
        [HttpGet("media/{mediaId}")]
        public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByMedia(int mediaId)
            {
                try
                {
                    var comments = await _commentRepository.GetByMediaIdAsync(mediaId);

                    if (comments == null || !comments.Any())
                    {
                        return Ok(Enumerable.Empty<CommentResponseDto>());
                    }

                    return Ok(comments);
                }
                catch (ApplicationException ex)
                {
                    return BadRequest(ex.Message);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Ocurrió un error interno");
                }
            }

        // GET: api/comments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _commentRepository.GetByIdAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            return Ok(comment);
        }

        // POST: api/comments
        [HttpPost]
        public async Task<ActionResult<Comment>> PostComment([FromBody] CreateCommentDto createCommentDto)
        {
            var comment = new Comment
            {
                Content = createCommentDto.Content,
                UserId = createCommentDto.UserId,
                MediaId = createCommentDto.MediaId,
                CreatedAt = DateTime.UtcNow
            };

            await _commentRepository.AddAsync(comment);

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        }

        // PUT: api/comments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, [FromBody] UpdateCommentDto updateCommentDto)
        {
            var comment = await _commentRepository.GetByIdAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            comment.Content = updateCommentDto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            await _commentRepository.UpdateAsync(comment);

            return NoContent();
        }

        // DELETE: api/comments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _commentRepository.GetByIdAsync(id);

            if (comment == null)
            {
                return NotFound();
            }

            await _commentRepository.DeleteAsync(comment);

            return NoContent();
        }

        // GET: api/comments/media/5/count
        [HttpGet("media/{mediaId}/count")]
        public async Task<ActionResult<int>> GetCommentCountForMedia(int mediaId)
        {
            var count = await _commentRepository.CountByMediaAsync(mediaId);
            return Ok(count);
        }
    }
}
