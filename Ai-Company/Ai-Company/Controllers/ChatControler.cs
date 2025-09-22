using Application.Service;
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly FirebaseService _firebaseService;

    public ChatController(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
    {
        await _firebaseService.SaveChatMessage(request.ChatId, request.UserId, request.Message);
        return Ok("Message sent to Firebase!");
    }

}

public class ChatMessageRequest
{
    public string ChatId { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
}
