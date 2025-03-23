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
            //DD: Verificar si el curso y el estudiante existen
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

            //DD: Crear la asistencia
            var attendance = new AttendanceEntity
            {
                Attended = attendanceCreateDto.Attended,
                RegistrationDate = DateTime.UtcNow,
                CourseId = attendanceCreateDto.CourseId,
                StudentId = attendanceCreateDto.StudentId,
                CreatedByUser = await _context.Users.FindAsync(course.TeacherId),
                UpdatedByUser = await _context.Users.FindAsync(course.TeacherId)
            };

            //DD: Guardar la asistencia en la base de datos
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            //DD: Retornar el DTO de la asistencia creada
            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }
        public async Task<AttendanceDto> EditAttendanceAsync(Guid attendanceId, AttendanceEditDto attendanceEditDto)
        {
            var attendance = await _context.Attendances.FindAsync(attendanceId);
            if (attendance == null)
            {
                throw new ArgumentException("La asistencia no existe.");
            }

            //DD: Editar solo el campo permitido
            attendance.Attended = attendanceEditDto.Attended;

            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Attended = attendance.Attended,
                RegistrationDate = attendance.RegistrationDate,
                CourseId = attendance.CourseId,
                StudentId = attendance.StudentId
            };
        }
         public async Task<List<AttendanceDto>> ListAttendancesAsync()
        {
            var attendances = await _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Student) 
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name, // Opcional: incluir el nombre del curso
                StudentName = a.Student?.FirstName
            }).ToList();
        }

        //DD: Método para listar asistencias por curso
        public async Task<List<AttendanceDto>> ListAttendancesByCourseAsync(Guid courseId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = a.Student?.FirstName
            }).ToList();
        }

        //DD: Método para listar asistencias por estudiante
        public async Task<List<AttendanceDto>> ListAttendancesByStudentAsync(Guid studentId)
        {
            var attendances = await _context.Attendances
                .Where(a => a.StudentId == studentId)
                .Include(a => a.Course)
                .Include(a => a.Student)
                .ToListAsync();

            return attendances.Select(a => new AttendanceDto
            {
                Id = a.Id,
                Attended = a.Attended,
                RegistrationDate = a.RegistrationDate,
                CourseId = a.CourseId,
                StudentId = a.StudentId,
                CourseName = a.Course?.Name,
                StudentName = a.Student?.FirstName
            }).ToList();
        }
    }
}