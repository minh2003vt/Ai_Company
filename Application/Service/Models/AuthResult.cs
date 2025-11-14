namespace Application.Service.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
        public int FailedAttempts { get; set; }
        public int RemainingAttempts { get; set; }
        public System.Guid? UserId { get; set; }
        public object Data { get; set; }
    }
}


