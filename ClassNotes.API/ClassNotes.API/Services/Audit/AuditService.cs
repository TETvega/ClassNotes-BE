using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClassNotes.API.Services.Audit
{
    public class AuditService : IAuditService
    {
        // --------------------- CP --------------------- //

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            IHttpContextAccessor httpContextAccessor
            )
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            var idClaim = _httpContextAccessor.HttpContext
               .User.Claims.Where(x => x.Type == "UserId").FirstOrDefault();

            // return "41e958ea-a9e3-4deb-bccb-e17a987164c7";
            return idClaim.Value; // Deberia ser asi ya que si no no se esta haciendo uso de la función
        }

        // --------------------- CP --------------------- //
    }
}
