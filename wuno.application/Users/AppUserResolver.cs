using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Users
{
    public sealed class AppUserResolver(IHttpContextAccessor acc) : IAppUserResolver
    {
        public bool TryGet(out Guid userId)
        {
            var ctx = acc.HttpContext!;
            var sub = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out userId)) return true;

            var gid = ctx.User?.FindFirstValue("guest-id");
            return Guid.TryParse(gid, out userId);
        }
    }
}
