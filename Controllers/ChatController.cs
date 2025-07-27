using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VibeMe.Microservice.Entities;
using VibeMe.Microservice.Repository;
using VibeMe.Microservice.Repository.DTOS;
using VibeME.DTOS;

namespace VibeMe.Microservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class ChatController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IChatRepository _chatRepository;

        public ChatController(IHubContext<ChatHub> hubContext, IChatRepository chatRepository)
        {
            _hubContext = hubContext;
            _chatRepository = chatRepository;
        }
        [HttpPost("join-chat/{chatRoomId}")]
        public async Task<IActionResult> JoinChat(int chatRoomId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Validar acceso al chat
            if (!await _chatRepository.UserHasAccessToChat(chatRoomId, userId))
            {
                return Forbid();
            }

            // Notificar al Hub (opcional)
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("JoinChat", userId, chatRoomId);

            return Ok();
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto messageDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Guardar en base de datos
            await _chatRepository.SendMessage(messageDto.ChatRoomId, userId, messageDto.Message);

            // Enviar via SignalR
            await _hubContext.Clients.Group(messageDto.ChatRoomId.ToString())
                .SendAsync("ReceiveMessage", new
                {
                    SenderId = userId,
                    messageDto.ChatRoomId,
                    messageDto.Message,
                    SentAt = DateTime.UtcNow
                });

            return Ok();
        }
    }
}
