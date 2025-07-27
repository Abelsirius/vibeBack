namespace VibeME.DTOS
{
    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserCommentDto User { get; set; }
        public int MediaId { get; set; }
    }
}
