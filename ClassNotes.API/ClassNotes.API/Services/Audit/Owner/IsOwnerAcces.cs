namespace ClassNotes.API.Services.Audit.Owner
{
    public interface IsOwnerAcces
    {
        bool IsTheOwtherOfTheCourse(Guid courseId);
    }
}
