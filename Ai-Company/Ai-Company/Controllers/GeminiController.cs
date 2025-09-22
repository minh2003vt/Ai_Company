using Application.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using static Application.Service.GeminiService;
using System.Collections.Concurrent;
using Application.Service;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly IGeminiService _gemini;

        public GeminiController(IGeminiService geminiService)
        {
            _gemini = geminiService;
        }


        [HttpPost("message")]
        public async Task<IActionResult> SendSimpleMessage([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message text cannot be empty.");
            }

            try
            {
                // Create a list with just the current user's message
                var history = new List<Chat>
            {
                new Chat { Role = "user", Text = request.Message }
            };

                // Send this single-turn history to the Gemini API
                var modelResponseText = await _gemini.GenerateContentAsync(history);

                // Return the model's response directly
                return Ok(new { response = modelResponseText });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error communicating with Gemini API: {ex.Message}");
                return StatusCode(502, $"Failed to get response from AI model. Please try again. ({ex.Message})");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error parsing Gemini API response: {ex.Message}");
                return StatusCode(500, $"Failed to process AI model response. ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
    public class ChatRequest
    {
        public string Message { get; set; }
    }

}
