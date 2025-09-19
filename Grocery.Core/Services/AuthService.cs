using Grocery.Core.Helpers;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;

namespace Grocery.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IClientService _clientService;
        public AuthService(IClientService clientService)
        {
            _clientService = clientService;
        }
        public Client? Login(string email, string password)
        {
            // 1) Haal de klant op op basis van e-mail
            var client = _clientService.Get(email);
            if (client is null) return null;

            // 2) Vergelijk het ingevoerde wachtwoord met de opgeslagen hash
            //    Let op: client.Password bevat de opgeslagen "salt.hash" string
            bool ok = PasswordHelper.VerifyPassword(password, client.Password);

            // 3) OK? Geef client terug, anders null
            return ok ? client : null;
        }
    }
}
