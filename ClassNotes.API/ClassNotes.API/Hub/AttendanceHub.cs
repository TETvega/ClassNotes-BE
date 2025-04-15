using ClassNotes.API.Constants;
using ClassNotes.API.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace ClassNotes.API.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]

    /// <summary>
    /// Hub de SignalR para manejar la comunicación en tiempo real relacionada con la asistencia a cursos.
    /// Implementa autenticación JWT y autorización por roles, con validación de propiedad de cursos.
    /// </summary>
    /// <remarks>
    /// <para><b>Seguridad implementada:</b></para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Autenticación mediante Bearer Token (JWT)</description>
    ///     </item>
    ///     <item>
    ///         <description>Autorización por rol de usuario (<see cref="RolesConstant.USER"/>)</description>
    ///     </item>
    ///     <item>
    ///         <description>Validación adicional de propiedad del curso mediante <see cref="IAuditService"/></description>
    ///     </item>
    /// </list>
    /// <para><b>Referencias clave:</b></para>
    /// <list type="bullet">
    ///     <item>
    ///         <description><see href="https://learn.microsoft.com/es-es/aspnet/core/signalr/authn-and-authz?view=aspnetcore-9.0">Autenticación y autorización en SignalR</see></description>
    ///     </item>
    ///     <item>
    ///         <description><see href="https://learn.microsoft.com/es-es/aspnet/core/signalr/groups?view=aspnetcore-9.0">Manejo de grupos en SignalR</see></description>
    ///     </item>
    ///     <item>
    ///         <description><see href="https://learn.microsoft.com/es-es/aspnet/core/tutorials/signalr?view=aspnetcore-9.0&tabs=visual-studio"/>Base de signal</description>
    ///     </item>
    /// </list>
    /// </remarks>
    public class AttendanceHub : Hub


    {
        private readonly IAuditService _auditService;

        public AttendanceHub(
            IAuditService auditService
            
            )
        {
            _auditService = auditService;
        }
        // No es necesario agregar métodos adicionales aquí,
        // ya que el controlador enviará mensajes directamente a los clientes.
        [Authorize(Roles = $"{RolesConstant.USER}")]

        /// <summary>
        /// Permite a un usuario unirse al grupo de notificaciones de un curso específico.
        /// </summary>
        /// <param name="courseId">Identificador del curso al que unirse</param>
        /// <returns>Tarea asincrónica</returns>
        /// <exception cref="HubException">
        /// Se lanza si el usuario no está autenticado o no es propietario del curso.
        /// </exception>
        /// <remarks>
        /// <para>Implementa las siguientes validaciones de seguridad:</para>
        /// <list type="number">
        ///     <item>Valida el token JWT mediante el atributo <see cref="AuthorizeAttribute"/></item>
        ///     <item>Verifica el rol de usuario (<see cref="RolesConstant.USER"/>)</item>
        ///     <item>Valida que el usuario sea propietario del curso mediante <see cref="IAuditService.isTheOwtherOfTheCourse"/></item>
        /// </list>
        /// <para>Configuración requerida en Startup:</para>
        /// <code>
        /// services.AddSignalR(options => 
        /// {
        ///     options.EnableDetailedErrors = true;
        /// });
        /// </code>
        /// </remarks>
        public async Task JoinCourseGroup(Guid courseId)
        {

            var isOwner = _auditService.isTheOwtherOfTheCourse(courseId);

            if(!isOwner)
                throw new HubException("Usuario no autenticado o No esta Authorizado a este curso");

            await Groups.AddToGroupAsync(Context.ConnectionId, courseId.ToString());
        }
        [Authorize(Roles = $"{RolesConstant.USER}")]
        public async Task LeaveCourseGroup(Guid courseId)
        {

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, courseId.ToString());
        }
    }
}