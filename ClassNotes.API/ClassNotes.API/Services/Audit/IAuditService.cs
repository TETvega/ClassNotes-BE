namespace ClassNotes.API.Services.Audit
{
	public interface IAuditService
	{
        string GetUserId();

        bool isTheOwtherOfTheCourse(Guid courseId);
    }
}
