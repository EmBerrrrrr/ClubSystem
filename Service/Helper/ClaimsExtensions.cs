using System.Security.Claims;

namespace Service.Helper
{
    public static class ClaimsExtensions
    {
        public static int GetAccountId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!.Value);
        }
    }
}
