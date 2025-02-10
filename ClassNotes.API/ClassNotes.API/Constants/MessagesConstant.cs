namespace ClassNotes.API.Constants
{
	public static class MessagesConstant
	{
		//Busqueda de Registros
		public const string RECORDS_FOUND = "Registros encontrados correctamente.";
		public const string RECORD_FOUND = "Registro encontrado correctamente.";
		public const string RECORD_NOT_FOUND = "Registro no encontrado.";

		//Creacion de registros 
		public const string CREATE_SUCCESS = "Registro creado correctamente.";
		public const string CREATE_ERROR = "Se produjo un error al crear el registro.";

		//Actualizacion de Registros
		public const string UPDATE_SUCCESS = "Registro editado correctamente.";
		public const string UPDATE_ERROR = "Se produjo un error al editar el registro.";

		//Eliminacion de Registros 
		public const string DELETE_SUCCESS = "Registro eliminado correctamente.";
		public const string DELETE_ERROR = "Se produjo un error al eliminar el registro.";

		//Manejo de LOGIN
		public const string LOGIN_SUCCESS = "Sesión iniciada correctamente.";
		public const string LOGIN_ERROR = "Se produjo un error al iniciar sesión, el correo o la contraseña no existen.";

		//Manejo de Register
		public const string REGISTER_SUCCESS = "Registro de usuario creado correctamente.";
		public const string REGISTER_ERROR = "Se produjo un error al registrar el usuario.";
	}
}
