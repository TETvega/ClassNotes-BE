using ClassNotes.API.Database;
using ClassNotes.API.Database.Entities;
using ClassNotes.API.Dtos;
using ClassNotes.API.Dtos.Attendances;
using ClassNotes.API.Services.Attendances;
using Microsoft.EntityFrameworkCore;
using System;
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
            // Verificar si el curso y el estudiante existen
            var course = await _context.Courses.FindAsync(attendanceCreateDto.CourseId);
            if (course == null)
            {
                throw new ArgumentException("El curso no existe.");
            }

            var student = await _context.Students.FindAsync(attendanceCreateDto.StudentId);
            if (student == null)
            {
                throw new ArgumentException("El estudiante no existe.");
            }

            // Crear la asistencia
            var attendance = new AttendanceEntity
            {
                Attended = attendanceCreateDto.Attended, 
                RegistrationDate = DateTime.UtcNow,
                CourseId = attendanceCreateDto.CourseId,
                StudentId = attendanceCreateDto.StudentId,
                CreatedByUser = await _context.Users.FindAsync(course.TeacherId),
                UpdatedByUser = await _context.Users.FindAsync(course.TeacherId) 
            };

            // Guardar la asistencia en la base de datos
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Retornar el DTO de la asistencia creada
            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }
    }
}