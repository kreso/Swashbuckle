﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace Swashbuckle.Swagger
{
    public class OperationGenerator
    {
        private readonly IEnumerable<IOperationFilter> _operationFilters;
        private readonly DataTypeRegistry _dataTypeRegistry;

        public OperationGenerator(DataTypeRegistry dataTypeRegistry, IEnumerable<IOperationFilter> operationFilters)
        {
            _dataTypeRegistry = dataTypeRegistry;
            _operationFilters = operationFilters;
        }

        public Operation ApiDescriptionToOperation(ApiDescription apiDescription)
        {
            var apiPath = apiDescription.RelativePathSansQueryString();
            var parameters = apiDescription.ParameterDescriptions
                .Select(paramDesc => CreateParameter(paramDesc, apiPath))
                .ToList();

            var operation = new Operation
            {
                Method = apiDescription.HttpMethod.Method,
                Nickname = apiDescription.Nickname(),
                Summary = apiDescription.Documentation,
                Parameters = parameters,
                ResponseMessages = new List<ResponseMessage>()
            };

            var responseType = apiDescription.ActualResponseType();
            if (responseType == null)
            {
                operation.Type = "void";
            }
            else
            {
                var dataType = _dataTypeRegistry.GetOrRegister(responseType);
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

            foreach (var filter in _operationFilters)
            {
                filter.Apply(operation, _dataTypeRegistry, apiDescription);
            }


            OrderResponseMessages(operation);

            return operation;
        }

        private void OrderResponseMessages(Operation operation)
        {
            if (operation.ResponseMessages.Count < 1) return;

            ((List<ResponseMessage>)operation.ResponseMessages).Sort((f, s) =>
            {
                if (f.Code > s.Code) return 1;
                if (f.Code < s.Code) return -1;
                return 0;
            });
        }

        private Parameter CreateParameter(ApiParameterDescription apiParamDesc, string apiPath)
        {
            var paramType = "";
            switch (apiParamDesc.Source)
            {
                case ApiParameterSource.FromBody:
                    paramType = "body";
                    break;
                case ApiParameterSource.FromUri:
                    paramType = apiPath.Contains(String.Format("{{{0}}}", apiParamDesc.Name)) ? "path" : "query";
                    break;
            }

            var parameter = new Parameter
            {
                ParamType = paramType,
                Name = apiParamDesc.Name,
                Description = apiParamDesc.Documentation,
                Required = !apiParamDesc.ParameterDescriptor.IsOptional
            };

            var dataType = _dataTypeRegistry.GetOrRegister(apiParamDesc.ParameterDescriptor.ParameterType);
            if (dataType.Type == "object")
            {
                parameter.Type = dataType.Id;
            }
            else
            {
                parameter.Type = dataType.Type;
                parameter.Format = dataType.Format;
                parameter.Items = dataType.Items;
                parameter.Enum = dataType.Enum;
            }

            return parameter;
        }
    }
}
