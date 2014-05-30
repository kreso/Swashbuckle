using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace Swashbuckle.Swagger
{
    internal class ApplySwaggerAttribute : IOperationFilter
    {
        private static readonly Dictionary<string, Type> AttributePropertyHandler = new Dictionary<string, Type>
        {
            { "ResponseClassType", typeof(ResponseClassTypeHandler) },           
        };

        public void Apply(Operation operation, DataTypeRegistry dataTypeRegistry, ApiDescription apiDescription)
        {
            var actionName = apiDescription.ActionDescriptor.ActionName;
            var controllerType = apiDescription.ActionDescriptor.ControllerDescriptor.ControllerType;
            var paramTypes = GetTypeOfActionParameters(apiDescription);
            var attribute =
                controllerType.GetMethod(actionName, paramTypes.ToArray()).GetCustomAttributes(typeof(SwaggerAttribute), false).FirstOrDefault();

            if (attribute == null) return;

            ExecuteAttributePropertyHandlers(operation, dataTypeRegistry, apiDescription, (SwaggerAttribute)attribute);
        }

        private IEnumerable<Type> GetTypeOfActionParameters(ApiDescription apiDescription)
        {
            var parameters = apiDescription.ActionDescriptor.GetParameters();
            var paramTypes = new List<Type>();
            if (parameters != null)
                parameters.ToList().ForEach(p => paramTypes.Add(p.ParameterType));

            return paramTypes;
        }

        private void ExecuteAttributePropertyHandlers(Operation operation, DataTypeRegistry dataTypeRegistry, ApiDescription apiDescription, SwaggerAttribute attribute)
        {
            AttributePropertyHandler.AsParallel().ForAll(p =>
            {
                var propertyInfo = typeof(SwaggerAttribute).GetProperty(p.Key);
                if (propertyInfo == null) return;

                var handler = (ISwaggerAttributeHandler)(Activator.CreateInstance(p.Value));
                handler.Handle(operation, dataTypeRegistry, apiDescription, attribute);
            });
        }
    }

    internal interface ISwaggerAttributeHandler
    {
        void Handle(Operation operation, DataTypeRegistry dataTypeRegistry, ApiDescription apiDescription, SwaggerAttribute attribute);
    }

    internal class ResponseClassTypeHandler : ISwaggerAttributeHandler
    {
        public void Handle(Operation operation, DataTypeRegistry dataTypeRegistry, ApiDescription apiDescription, SwaggerAttribute attribute)
        {
            var responseType = attribute.ResponseClassType;
            if (responseType == null || responseType == typeof(void))
            {
                operation.Type = "void";
                return;
            }

            var dataType = dataTypeRegistry.GetOrRegister(responseType);

            if (dataType.Type == "object")
            {
                operation.Type = dataType.Id;
            }
            else
            {
                operation.Type = dataType.Type;
                operation.Format = dataType.Format;
                operation.Items = dataType.Items;
                operation.Enum = dataType.Enum;
            }
        }
    }
}
