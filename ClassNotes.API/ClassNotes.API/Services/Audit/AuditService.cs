namespace ClassNotes.API.Services.Audit
{
	public class AuditService : IAuditService
	{
		// --------------------- CP --------------------- //

		private readonly IHttpContextAccessor _httpContextAccessor;

		public AuditService(
			IHttpContextAccessor httpContextAccessor
			)
		{
			_httpContextAccessor = httpContextAccessor;
		}

		public string GetUserId()
		{
			var idClaim = _httpContextAccessor.HttpContext
				.User.Claims.Where(x => x.Type == "UserId").FirstOrDefault();

			return idClaim.Value;
		}

		// --------------------- CP --------------------- //
	}
}
