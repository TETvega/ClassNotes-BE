namespace ClassNotes.API.Services.Audit
{
	public class AuditService : IAuditService
	{
		//private readonly IHttpContextAccessor _httpContextAccessor;

		//public AuditService(
		//	IHttpContextAccessor httpContextAccessor
		//	)
		//{
		//	this._httpContextAccessor = httpContextAccessor;
		//}

		//public string GetUserId()
		//{
		//	var idClaim = _httpContextAccessor.HttpContext
		//		.User.Claims.Where(x => x.Type == "UserId").FirstOrDefault();

		//	return idClaim.Value;
		//}

		// AM: Descomentar para cargar el Seed de Datos por primera vez
		public string GetUserId()
		{
			return "41e958ea-a9e3-4deb-bccb-e17a987164c7";
		}
	}
}
