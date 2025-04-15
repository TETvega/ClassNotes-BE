using ClassNotes.API.Database;
using iText.Commons.Actions.Contexts;

namespace ClassNotes.API.Services.Audit
{
    public class AuditService : IAuditService
    {
        // --------------------- CP --------------------- //

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClassNotesContext _context;

        public AuditService(
            IHttpContextAccessor httpContextAccessor,
            ClassNotesContext context

            )
        {
            this._httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public string GetUserId()
        {
            var idClaim = _httpContextAccessor.HttpContext
               .User.Claims.Where(x => x.Type == "UserId").FirstOrDefault();

            return idClaim.Value;
        }

        bool IAuditService.isTheOwtherOfTheCourse(Guid courseId)
        {
            var userId = GetUserId();
            var isOwner = _context.Courses.FirstOrDefault(c => c.Id == courseId).Center.TeacherId == userId;
            return isOwner;
        }

        // --------------------- CP --------------------- //
    }
}