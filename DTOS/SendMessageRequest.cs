using System.ComponentModel.DataAnnotations;

namespace VibeME.DTOS
{
    public class SendMessageRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ChatRoomId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Content { get; set; }
    }

}
