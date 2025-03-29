using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClassNotes.API.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        //DD: Desabilitacion de la Auditoria 
        private bool _auditDisabled = false;
        private const string SystemUserId = "41e958ea-a9e3-4deb-bccb-e17a987164c7";

        public AuditService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            // Si la auditoría está desactivada (para background services)
            if (_auditDisabled)
            {
                return SystemUserId;
            }

            // Lógica normal para requests HTTP
            if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = _httpContextAccessor.HttpContext
                   .User.Claims.FirstOrDefault(x => x.Type == "UserId");

                return idClaim?.Value ?? SystemUserId; // Fallback al usuario sistema
            }

            return SystemUserId; // Default para casos sin contexto HTTP
        }

        // Nuevos métodos para controlar auditoría

        //DD: Desactiva la Auditoria 
        public bool DisableAuditTemporarily()
        {
            var originalState = _auditDisabled;
            _auditDisabled = true;
            return originalState;
        }

        public void RestoreAuditState(bool originalState)
        {
            _auditDisabled = originalState;
        }
    }
}