using Microsoft.AspNetCore.Http;
using Repository.Repo.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helper
{
    public class AccountStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public AccountStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthRepository repo)
        {
            // Chỉ check khi đã authenticate
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim != null &&
                    int.TryParse(accountIdClaim.Value, out var accountId))
                {
                    var account = await repo.GetAccountByIdAsync(accountId);

                    // 🚫 Account bị khóa
                    if (account != null && account.IsActive == false)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Account has been locked");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
