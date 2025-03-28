 
namespace ClassNotes.API.Constants
{
    public static class MessagesConstant
    {
        //  Busqueda de Registros (R-6XXX) 
        public const string RECORDS_FOUND = "R-1011: Registros encontrados correctamente.";
        public const string RECORD_FOUND = " R-1012: Registro encontrado correctamente.";
        public const string RECORD_NOT_FOUND = "R-1013: Registro no encontrado.";

        //  Creación de Registros (R-7XXX)
        public const string CREATE_SUCCESS = "R-1034: Registro creado correctamente.";
        public const string CREATE_ERROR = "R-1035: Se produjo un error al crear el registro.";

        //  Actualización de Registros (R-8XXX) 
        public const string UPDATE_SUCCESS = "R-1056: Registro editado correctamente.";
        public const string UPDATE_ERROR = "R-1057: Se produjo un error al editar el registro.";

        //  Eliminación de Registros (R-9XXX) 
        public const string DELETE_SUCCESS = "R-1088: Registro eliminado correctamente";
        public const string DELETE_ERROR = "R-1089: Se produjo un error al eliminar el registro.";

        //  Manejo de LOGIN (A-4XXX) 
        public const string LOGIN_SUCCESS = "A-2001: Sesión iniciada correctamente.";
        public const string LOGIN_ERROR = "A-2012: Credenciales inválidas.";
        public const string TOKEN_EXPIRED = "A-2014: La sesión ha expirado, por favor inicie sesión nuevamente.";
        public const string USER_REGISTERED_SUCCESS = "A-2023: Registro de usuario realizado satisfactoriamente.";
        public const string USER_REGISTRATION_FAILED = "A-2024: Error al registrar el usuario.";
        public const string REFRESH_TOKEN_EXPIRED = "A-2038: La sesión ha expirado.";
        public const string INVALID_REFRESH_TOKEN = "A-2039: La sesión no es válida.";
        public const string INCORRECT_PASSWORD = "A-2045: Contraseña incorrecta.";
        public const string INCORRECT_EMAIL = "A-2046: Correo electronico incorrecta.";
        public const string USER_RECORD_NOT_FOUND = "A-2047: El usuario no existe.";

        //  Manejo de Registro de Usuarios (RU-5XXX)
        public const string REGISTER_SUCCESS = "RU-3001: Registro de usuario creado correctamente.";
        public const string REGISTER_ERROR = "RU-3002: Se produjo un error al registrar el usuario.";

        // Menejo de Usuario (U-2XXX)
        public const string USER_NOT_FOUND = "U-4001: No se encontró el usuario.";
        public const string USER_CREATED_SUCCESS = "U-4012U: Usuario creado con éxito.";
        public const string USER_UPDATE_SUCCESS = "U-4023: Usuario actualizado con éxito.";
        public const string USER_DELETE_SUCCESS = "U-4034: Usuario eliminado con éxito.";
        public const string USER_OPERATION_FAILED = "U-4045: No se pudo completar la operación con el usuario.";
        public const string USER_Email_FAILED = "U-4045: No se pudo cambiar el correo del usuario.";
        public const string USER_PASSWORD_CHANGE_FAILED = "U-4046: No se pudo cambiar la contraseña.";
        public const string PASSWORD_UPDATED_SUCCESSFULLY = "U-4047: La contraseña fue actualizada satisfactoriamente.";
        public const string USER_EMAIL_ALREADY_REGISTERED = "U-4058: El correo electrónico ingresado ya está registrado.";
        public const string EMAIL_UPDATED_SUCCESSFULLY = "U-4059: El correo electrónico fue actualizado satisfactoriamente.";


        // Mensajes Generales (G-3XXX)
        public const string OPERATION_SUCCESS = "G-5011: Operación realizada con éxito.";
        public const string OPERATION_FAILED = "G-5012: No se pudo completar la operación.";
        public const string OPERATION_RECORD_NOT_FOUND = "G-5013: El recurso solicitado no está disponible.";

        // Manejo Centros (CEN-XXX)
        public const string NAME_REQUIRED = "CEN-6011: El nombre es requerido.";
        public const string DUPLICATE_NAME = "CEN-6012: Ya existe un centro con este nombre, ingrese uno nuevo.";
        public const string INVALID_IMAGE_FORMAT = "CEN-6013: El archivo no es una imagen válida. Formatos permitidos: .png, .jpg, .jpeg, .gif, .bmp, .tiff, .webp";
        public const string DELETE_CONFIRMATION_REQUIRED = "CEN-6014: No se confirmó la eliminación del centro.";
        public const string UNAUTHORIZED_DELETE = "CEN-6015: No está autorizado para borrar este registro.";
        public const string CENTER_HAS_COURSES = "CEN-6016: No se puede eliminar un centro si aún contiene clases asignadas.";
        public const string ERROR_DELETE_IMAGE = "CEN-6017: Error al tratar de eliminar la imagen, ponerse en contacto con soporte";
        public const string ERROR_CLOUDINARY_DELETE = "CEN-6028: Error al eliminar la imagen en Cloudinary";
        public const string ERROR_NOT_AUTHORIZED = "CEN-6129: No está autorizado para editar este registro.";
        public const string UNAUTHORIZED_ARCHIVE_CENTER = "CEN-6130: No está autorizado para archivar este centro.";
        public const string CENTER_ALREADY_ARCHIVED = "CEN-6131: Ya archivó este centro.";
        public const string CENTER_ARCHIVED_SUCCESS = "CEN-6132: Se archivó correctamente.";
        public const string CENTER_RECOVERY_UNAUTHORIZED = "CEN-6133: No está autorizado para recuperar este centro.";
        public const string CENTER_NOT_ARCHIVED = "CEN-6134: Este centro no está archivado.";
        public const string CENTER_RECOVERED_SUCCESS = "CEN-6135: Se recuperó correctamente.";

        // Manejo Estudiantes (STU-XXX)
        public const string STU_RECORD_NOT_FOUND = "STU-7001: El registro no fue encontrado.";
        public const string STU_RECORD_FOUND = "STU-7001: El registro fue encontrado.";
		public const string STU_RECORDS_FOUND = "STU-7028: Estudiantes encontrados correctamente.";
		public const string STU_RECORDS_NOT_FOUND = "STU-7029: No se encontrarón estudiantes.";
		public const string STU_CREATE_SUCCESS = "STU-7002: Estudiante creado exitosamente.";
        public const string STU_UPDATE_SUCCESS = "STU-7013: Estudiante actualizado exitosamente.";
        public const string STU_DELETE_SUCCESS = "STU-7014: Estudiante eliminado exitosamente.";
        public const string EMAIL_ALREADY_REGISTERED = "STU-7025: El correo electrónico ya está registrado con otro estudiante.";
        public const string STUDENT_EXISTS = "STU-7026: El estudiante ya existe y se ha referenciado correctamente.";
        public const string EMAIL_DIFFERENT_NAMES = "STU-7027: El correo electrónico ya está registrado con nombres diferentes.";

        // Manejo Actividades (ACT-XXX)
        public const string ACT_RECORD_NOT_FOUND = "ACT-8071: La actividad no fue encontrada.";
        public const string ACT_CREATE_SUCCESS = "ACT-8072: Actividad creada exitosamente.";
        public const string ACT_UPDATE_SUCCESS = "ACT-8073: Actividad actualizada exitosamente.";
        public const string ACT_DELETE_SUCCESS = "ACT-8074: Actividad eliminada exitosamente.";
        public const string ACT_QUALIFICATION_DATE_INVALID = "ACT-8075: La fecha de calificación no puede ser menor a la fecha actual.";
        public const string ACT_RECORDS_FOUND = "ACT-8085: Actividad encontrada correctamente.";

        // Manejo Cursos (CRS-XXX)
        public const string CRS_RECORD_NOT_FOUND = "CRS-9051: El curso no fue encontrado.";
        public const string CRS_CREATE_SUCCESS = "CRS-9052: Curso creado exitosamente.";
        public const string CRS_UPDATE_SUCCESS = "CRS-9053: Curso actualizado exitosamente.";
        public const string CRS_DELETE_SUCCESS = "CRS-9054: Curso eliminado exitosamente.";
        public const string CRS_ALREADY_EXISTS = "CRS-9065: El curso ya existe con los mismos datos.";
        public const string CRS_INVALID_TIME_RANGE = "CRS-9066: La hora de finalización no puede ser menor a la hora de inicio.";
        public const string CRS_NOT_AUTHORIZED = "CRS-9067: No tiene permiso para realizar esta acción.";
        public const string CRS_INVALID_COURSE_DATA = "CRS-9078: Los datos del curso proporcionados no son válidos.";
        public const string CRS_INSUFFICIENT_COURSE_DATA = "CRS-9079: No se proporcionaron todos los datos necesarios para crear el curso.";
        public const string CRS_PAGINATION_ERROR = "CRS-9010: Error al calcular la paginación.";
		    public const string CRS_RECORD_FOUND = "CRS-9011: El curso fue encontrado exitosamente.";
        public const string CRS_INVALID_SETTING = "CRS-9020: La configuración seleccionada no existe o no pertenece al usuario." ;

		// Manejo Notas de Curso (CNS-XXX)
		public const string CNS_RECORD_NOT_FOUND = "CNS-101: La nota de curso no fue encontrada.";
        public const string CNS_CREATE_SUCCESS = "CNS-112: Nota de curso creada exitosamente.";
        public const string CNS_UPDATE_SUCCESS = "CNS-113: Nota de curso actualizada exitosamente.";
        public const string CNS_DELETE_SUCCESS = "CNS-114: Nota de curso eliminada exitosamente.";
        public const string CNS_RECORDS_FOUND = "CNS-125: Se encontraron las notas de curso.";
        public const string CNS_EDIT_PERMISSION_DENIED = "CNS-126: No puede editar una nota que no le pertenece.";
        public const string CNS_DELETE_PERMISSION_DENIED = "CNS-127: No puede eliminar una nota que no le pertenece.";
        public const string CNS_END_TIME_BEFORE_START_TIME = "CNS-128 :La hora de finalización no puede ser menor a la hora de inicio.";
        public const string CNS_CLASS_ALREADY_EXISTS = "CNS-129: Ya existe la clase.";

        // Manejo Configuracion Curso (CP-XXX)
        public const string CP_CREATE_SUCCESS = "CP-231: Configuración de curso creada exitosamente.";
        public const string CP_UPDATE_SUCCESS = "CP-232: Configuración de curso actualizada exitosamente.";
        public const string CP_DELETE_SUCCESS = "CP-233: Configuración de curso eliminada exitosamente.";
        public const string CP_RECORD_NOT_FOUND = "CP-244: La configuración de curso no fue encontrada.";
        public const string CP_RECORD_FOUND = "CP-244: La configuración de curso fue encontrada.";
        public const string CP_INVALID_DATES = "CP-245: La fecha de finalización no puede ser menor a la de inicio.";
        public const string CP_INVALID_GRADES = "CP-246: Las puntuaciones mínima y máxima deben ser mayores a 0, y la máxima no puede ser menor a la mínima.";
        public const string CONFIGURATION_ALREADY_EXISTS = "CP-247: Ya existe la configuración";

        // Manewjo Emails (EM-XXX)
        public const string EMAIL_CREATE_SUCCESS = "EM-311: Correo electrónico creado exitosamente.";
        public const string EMAIL_UPDATE_SUCCESS = "EM-312: Correo electrónico actualizado exitosamente.";
        public const string EMAIL_DELETE_SUCCESS = "EM-313: Correo electrónico eliminado exitosamente.";
        public const string EMAIL_RECORD_NOT_FOUND = "EM-354: El correo electrónico no fue encontrado.";
        public const string EMAIL_INVALID_RECIPIENT = "EM-355: El destinatario no puede estar vacío o ser inválido.";
        public const string EMAIL_INVALID_SUBJECT = "EM-356: El asunto no puede estar vacío.";
        public const string EMAIL_INVALID_BODY = "EM-357: El cuerpo del correo no puede estar vacío.";
        public const string EMAIL_SENT_SUCCESSFULLY = "EM-358: El correo fue enviado correctamente";
        public const string EMAIL_COURSE_NOT_REGISTERED = "EM-359: El curso ingresado no está registrado.";
        public const string EMAIL_STUDENT_NOT_REGISTERED = "EM-360: El estudiante ingresado no está registrado.";
        public const string EMAIL_STUDENT_NOT_REGISTERED_IN_CLASS = "EM-361: El estudiante ingresado no está registrado en la clase ingresada.";

        // Manejo OTP (OTP-XXX)
        public const string OTP_CREATE_SUCCESS = "OTP-403: Código OTP generado y enviado correctamente.";
        public const string OTP_CREATE_USER_NOT_FOUND = "OTP-401: El correo ingresado no está registrado.";
        public const string OTP_EXPIRED_OR_INVALID = "OTP-402: El código OTP ingresado no es válido o ha expirado.";
        public const string OTP_INVALID_CODE = "OTP-404: El código OTP ingresado no es válido.";
        public const string OTP_VALIDATION_SUCCESS = "OTP-405: Código OTP validado correctamente.";
        public const string OTP_CACHE_FOUND = "OTP-406: OTP encontrado en caché.";
        public const string OTP_CACHE_NOT_FOUND = "OTP-407: OTP no encontrado o expirado.";
        public const string OTP_INVALID_RECIPIENT = "OTP-408: El correo electrónico no puede estar vacío o ser inválido.";
        public const string OTP_SEND_FAILURE = "OTP-409: No se pudo enviar el código OTP.";
        public const string OTP_SECRET_GENERATION_FAILURE = "OTP-410: Error al generar la clave secreta para OTP.";
        public const string OTP_MEMORY_CACHE_FAILURE = "OTP-411: Error al almacenar el código OTP en la memoria caché.";
     
        // Manejo Cloudinary (CD-XXX)
        public const string IMAGE_UPLOAD_SUCCESS = "CD-511: Imagen subida correctamente.";
        public const string IMAGE_UPLOAD_FAILED = "CD-512: Error al subir la imagen.";
        public const string IMAGE_UPLOAD_INVALID_FORMAT = "CD-513: El archivo no es una imagen válida.";
        public const string IMAGE_DELETE_SUCCESS = "CD-524: Imagen eliminada correctamente.";
        public const string IMAGE_DELETE_FAILED = "CD-525: Error al eliminar la imagen en Cloudinary.";
        public const string IMAGE_UPLOAD_CLOUDINARY_ERROR = "CD-529: Error al subir la imagen a Cloudinary";
        public const string IMAGE_UPLOAD_STATUS_ERROR = "CD-510: Error en la respuesta de Cloudinary";
        public const string CD_INVALID_IMAGE_FORMAT = "CD-511: El archivo no es una imagen válida. Formatos permitidos: .png, .jpg, .jpeg, .gif, .bmp, .tiff, .webp";

		// Manejo de Etiquetas de Actividades (TA-XXX)
		public const string TA_RECORDS_FOUND = "TA-001: Etiquetas de actividad encontradas correctamente.";
		public const string TA_RECORDS_NOT_FOUND = "TA-002: No se encontraron etiquetas de actividad.";
		public const string TA_RECORD_FOUND = "TA-003: Etiqueta de actividad encontrada correctamente.";
		public const string TA_RECORD_NOT_FOUND = "TA-004: La etiqueta de actividad no fue encontrada.";
		public const string TA_CREATE_SUCCESS = "TA-005: Etiqueta de actividad creada exitosamente.";
		public const string TA_CREATE_ERROR = "TA-006: No se pudo crear la etiqueta de actividad.";
		public const string TA_UPDATE_SUCCESS = "TA-007: Etiqueta de actividad actualizada exitosamente.";
		public const string TA_UPDATE_ERROR = "TA-008: No se pudo actualizar la etiqueta de actividad.";
		public const string TA_DELETE_SUCCESS = "TA-009: Etiqueta de actividad eliminada exitosamente.";
		public const string TA_DELETE_ERROR = "TA-010: No se pudo eliminar la etiqueta de actividad.";
		public const string TA_IS_USED = "TA-011: La etiqueta no puede ser eliminada porque está asociada a una o más actividades.";

		// Manejo de asistencias (ATT-XXX)
		public const string ATT_RECORDS_FOUND = "ATT-001: Asistencias encontradas correctamente.";
		public const string ATT_RECORDS_NOT_FOUND = "ATT-002: No se encontrarón asistencias.";
		public const string ATT_RECORD_FOUND = "ATT-003: Asistencia encontrada correctamente.";
		public const string ATT_RECORD_NOT_FOUND = "ATT-004: La asistencia no fue encontrada.";
		public const string ATT_CREATE_SUCCESS = "ATT-005: Asistencia registrada exitosamente.";
		public const string ATT_CREATE_ERROR = "ATT-006: No se pudo registrar la asistencia.";
		public const string ATT_UPDATE_SUCCESS = "ATT-007: Asistencia actualizada exitosamente.";
		public const string ATT_UPDATE_ERROR = "ATT-008: No se pudo actualizar la asistencia.";
		public const string ATT_DELETE_SUCCESS = "ATT-009: Asistencia eliminada exitosamente.";
		public const string ATT_DELETE_ERROR = "ATT-010: No se pudo eliminar asistencia.";
        public const string ATT_STUDENT_NOT_ENROLLED = "ATT-011: El estudiante no está registrado en el curso.";

    }
}
