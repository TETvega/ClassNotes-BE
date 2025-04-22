namespace ClassNotes.API.Constants
{
    public static class Attendance_Helpers
    {
        // para saber quien registro el cambio 
        public const string TEACHER = nameof(TEACHER);
        public const string STUDENT = nameof(STUDENT);
        public const string SYSTEM = nameof(SYSTEM);

        // para saber el metodo que se utilizo para registrar la asistencia
        public const string TYPE_OTP = nameof(TYPE_OTP);
        public const string TYPE_QR = nameof(TYPE_QR);
        public const string TYPE_MANUALLY = nameof(TYPE_MANUALLY);
        public const string TYPE_BOUGTH = nameof(TYPE_BOUGTH);

        //Para enviar mensaje de Signal

        public const string UPDATE_ATTENDANCE_STATUS = nameof(UPDATE_ATTENDANCE_STATUS);


    }
}
