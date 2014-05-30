using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Http.Description;

namespace Swashbuckle.Swagger
{
    internal class ApplyErrorStatusCodes : IOperationFilter
    {
        private static IEnumerable<HttpStatusCode> _errorStatusCodes;

        public ApplyErrorStatusCodes(IEnumerable<HttpStatusCode> errorStatusCodes)
        {
            _errorStatusCodes = errorStatusCodes;
        }

        public void Apply(Operation operation, DataTypeRegistry dataTypeRegistry, ApiDescription apiDescription)
        {
            if (_errorStatusCodes == null) return;
            _errorStatusCodes.ToList().ForEach(e => operation.ResponseMessages.Add(new ResponseMessage
            {
                Code = (int)e,
                Message = Regex.Replace(Enum.GetName(typeof(HttpStatusCode), e),
                                        @"([a-z])([A-Z])", @"$1 $2", RegexOptions.None),
            }));
        }
    }
}
