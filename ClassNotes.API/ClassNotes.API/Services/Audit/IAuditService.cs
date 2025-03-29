namespace ClassNotes.API.Services.Audit
{
	public interface IAuditService
	{
        bool DisableAuditTemporarily();
        string GetUserId();
        void RestoreAuditState(bool originalState);
    }
}
