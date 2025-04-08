using Microsoft.AspNetCore.SignalR;

namespace ClassNotes.API.Hubs
{
    public class AttendanceHub : Hub
    {
        // No es necesario agregar métodos adicionales aquí,
        // ya que el controlador enviará mensajes directamente a los clientes.
        public async Task JoinCourseGroup(string courseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, courseId);
        }

        public async Task LeaveCourseGroup(string courseId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, courseId);
        }
    }
}