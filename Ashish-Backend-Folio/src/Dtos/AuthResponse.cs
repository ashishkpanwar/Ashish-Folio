namespace Ashish_Backend_Folio.Dtos
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string UserName { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }

}
