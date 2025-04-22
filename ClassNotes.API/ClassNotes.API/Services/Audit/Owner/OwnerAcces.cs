
using ClassNotes.API.Database;
using Microsoft.EntityFrameworkCore;

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

            var course = _context.Courses
                  .Include(c => c.Center)
                  .FirstOrDefault(c => c.Id == courseId);

            if( course == null )
            {
                Console.WriteLine("Curso no encontrado.");
                return false;
            }

            if (course.Center == null)
            {
                Console.WriteLine("El curso no tiene centro asignado.");
                return false;
            }

            var isOwner = course.Center.TeacherId == userId;
            if (!isOwner)
            {
                Console.WriteLine("El usuario no es dueño del curso.");
            }


            return isOwner;
        }
    }
}
