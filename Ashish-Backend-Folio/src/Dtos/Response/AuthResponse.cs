namespace Ashish_Backend_Folio.Dtos.Response
{
    public class AuthResponse
    {
        public string token { get; set; }
        public string userName { get; set; }
        public IEnumerable<string> roles { get; set; }
        public string refreshToken { get; set; }
    }

}
