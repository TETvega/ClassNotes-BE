using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClassNotes.API.Services
{
    public class AttendanceService : IAttendancesService
    {
        private readonly ClassNotesContext _context;

        public AttendanceService(ClassNotesContext context)
        {
            _context = context;
        }

        public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateDto attendanceCreateDto)
        {
            // Validar que el curso existe
            var course = await _context.Courses
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.Id == attendanceCreateDto.CourseId);

            if (course == null)
            {
                throw new ArgumentException("El curso no existe.");
            }

            // Validar que el estudiante existe
            var student = await _context.Students.FindAsync(attendanceCreateDto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("El estudiante no existe.");
            }

            // Validar que el profesor existe
            var teacher = await _context.Users.FindAsync(attendanceCreateDto.TeacherId);
            if (teacher == null)
            {
                throw new ArgumentException("El profesor no existe.");
            }

            // Validar que el profesor pertenece al centro del curso
            var center = await _context.Centers
                .FirstOrDefaultAsync(c => c.Id == course.CenterId && c.TeacherId == attendanceCreateDto.TeacherId);

            if (center == null)
            {
                throw new ArgumentException("El profesor no está asignado a este centro.");
            }

            // Crear la asistencia
            var attendance = new AttendanceEntity
            {
                Attended = attendanceCreateDto.Attended,
                Status = attendanceCreateDto.Status,
                RegistrationDate = attendanceCreateDto.RegistrationDate,
                CourseId = attendanceCreateDto.CourseId,
                StudentId = attendanceCreateDto.StudentId,
                CreatedByUser = teacher,  // Asignamos el objeto UserEntity completo
                UpdatedByUser = teacher   // Asignamos el objeto UserEntity completo
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                Status = attendance.Status,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }

        public async Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, bool attended)
        {
            var attendance = await _context.Attendances
                .Include(a => a.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                throw new ArgumentException("La asistencia no existe.");
            }

            attendance.Attended = attended;

            // No podemos actualizar UpdatedDate directamente ya que no existe en la entidad
            // Pero podemos actualizar UpdatedByUser si es necesario
            // attendance.UpdatedByUser = ...;

            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                Status = attendance.Status,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }

        public async Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, AttendanceEditDto attendanceEditDto)
        {
            // Obtener la asistencia existente
            var attendance = await _context.Attendances
                .Include(a => a.UpdatedByUser)
                .FirstOrDefaultAsync(a => a.Id == attendanceId);

            if (attendance == null)
            {
                throw new ArgumentException("La asistencia no existe.");
            }

            // Actualizar solo el status (sin modificar Attended si no es necesario)
            attendance.Status = attendanceEditDto.Status;

            // Guardar cambios
            await _context.SaveChangesAsync();

            // Devolver el DTO actualizado
            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended, // Mantiene el valor existente
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
                // No incluye Status si no está en el DTO
            };
        }

        public async Task<List<AttendanceDto>> ListAttendancesAsync()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .Include(a => a.UpdatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Status = a.Status,
                Attended = a.Attended,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }

        public async Task<List<AttendanceDto>> ListAttendancesByCourseAsync(Guid courseId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                Status = a.Status,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }

        public async Task<List<AttendanceDto>> ListAttendancesByStudentAsync(Guid studentId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.StudentId == studentId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .Include(a => a.CreatedByUser)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                Status = a.Status,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = $"{a.Student?.FirstName} {a.Student?.LastName}"
            }).ToList();
        }
    }
}