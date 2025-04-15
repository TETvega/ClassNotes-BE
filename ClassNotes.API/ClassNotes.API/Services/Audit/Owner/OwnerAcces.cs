
using ClassNotes.API.Database;

namespace ClassNotes.API.Services.Audit.Owner
{
    public class OwnerAcces : IsOwnerAcces
    {
        private readonly IAuditService _audit;
        private readonly ClassNotesContext _context;

        public OwnerAcces(
            IAuditService audit,
            ClassNotesContext context
            )
        {
            _audit = audit;
            _context = context;
        }
        public bool IsTheOwtherOfTheCourse(Guid courseId)
        {
            var userId =  _audit.GetUserId();
            var isOwner = _context.Courses.FirstOrDefault(c => c.Id == courseId).Center.TeacherId == userId;
            return isOwner;
        }
    }
}
