using System;

namespace Swashbuckle.Swagger
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SwaggerAttribute : Attribute
    {
        public Type ResponseClassType { get; set; }
    }
}
