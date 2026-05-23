using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Transactions.Infrastructure.HttpHandlers
{
    public class TokenForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}