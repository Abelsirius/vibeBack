using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VibeMe.Microservice.Entities;
using VibeMe.Microservice.Repository;
using VibeMeMicroservice.Infraestructure;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatRepository chatRepository, ILogger<ChatHub> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = int.Parse(Context.UserIdentifier);
        var chats = await _chatRepository.GetUserChats(userId);

        foreach (var chat in chats)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chat.Id}");
        }

        await base.OnConnectedAsync();
    }
    public async Task<List<VibeMe.Microservice.Repository.ChatRoomDto>> GetUserChats()
    {
        var userId = int.Parse(Context.UserIdentifier);
        return (List<ChatRoomDto>)await _chatRepository.GetUserChats(userId);
    }
    public async Task<ChatRoomDto> GetOrCreatePrivateChat(int otherUserId)
    {
        var currentUserId = int.Parse(Context.UserIdentifier);
        var chat = await _chatRepository.GetOrCreatePrivateChat(currentUserId, otherUserId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chat.Id}");
        return chat;
    }
    public async Task<MessageDto> SendMessage(int chatRoomId, string message)
    {
        // 1. Validación básica de parámetros
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("El mensaje no puede estar vacío");

        if (chatRoomId <= 0)
            throw new ArgumentException("ID de sala de chat inválido");

        // 2. Obtener y validar el ID del usuario
        if (string.IsNullOrEmpty(Context.UserIdentifier))
            throw new UnauthorizedAccessException("Usuario no autenticado");

        if (!int.TryParse(Context.UserIdentifier, out int senderId))
            throw new HubException("Identificador de usuario inválido");

        try
        {
            // 3. Verificar que el usuario tiene acceso al chat
            if (!await _chatRepository.UserHasAccessToChat(chatRoomId, senderId))
                throw new UnauthorizedAccessException("No tienes acceso a este chat");

            // 4. Enviar el mensaje a través del repositorio
            var messageDto = await _chatRepository.SendMessage(chatRoomId, senderId, message.Trim());

            if (messageDto == null)
                throw new InvalidOperationException("Error al crear el mensaje");

            // 5. Enviar el mensaje a todos los miembros del grupo
            await Clients.Group($"chat_{chatRoomId}")
                .SendAsync("ReceiveMessage", messageDto);

            // 6. Retornar el DTO del mensaje
            return messageDto;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Error de base de datos al enviar mensaje al chat {ChatRoomId}", chatRoomId);
            throw new HubException("Error al guardar el mensaje");
        }
        catch (UnauthorizedAccessException)
        {
            throw; // Reenviar excepciones de autorización directamente
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar mensaje al chat {ChatRoomId}", chatRoomId);
            throw new HubException("Error al procesar el mensaje");
        }
    }

    public async Task MarkMessagesAsRead(int chatRoomId)
    {
        var userId = int.Parse(Context.UserIdentifier);
        await _chatRepository.MarkMessagesAsRead(chatRoomId, userId);
    }

    public async Task<IEnumerable<MessageDto>> GetHistory(int chatRoomId, int page = 1)
    {
        var userId = int.Parse(Context.UserIdentifier);
        return await _chatRepository.GetChatHistory(chatRoomId, userId, page);
    }
}