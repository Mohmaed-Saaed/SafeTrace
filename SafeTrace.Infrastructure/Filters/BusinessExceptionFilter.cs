using ElmahCore;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.Infrastructure.Filters
{
    public class BusinessExceptionFilter : IErrorFilter
    {
        public void OnErrorModuleFiltering(object sender, ExceptionFilterEventArgs args)
        {
            var ex = args.Exception.GetBaseException();

            if (ex is BadRequestException ||
                ex is NotFoundException ||
                ex is UnauthorizedException ||
                ex is ForbiddenException ||
                ex is ConflictException)
            {
                args.Dismiss();
            }
        }
    }
}