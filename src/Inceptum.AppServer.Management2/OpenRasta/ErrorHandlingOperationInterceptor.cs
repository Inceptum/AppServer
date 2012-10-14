using System;
using System.Collections.Generic;
using Inceptum.AppServer.Management2.Resources;
using OpenRasta;
using OpenRasta.OperationModel;
using OpenRasta.OperationModel.Interceptors;
using OpenRasta.TypeSystem;
using OpenRasta.TypeSystem.ReflectionBased;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.OpenRasta
{
    public class ErrorHandlingOperationInterceptor : OperationInterceptor
    {
        private readonly ICommunicationContext m_Context;

        public ErrorHandlingOperationInterceptor(ICommunicationContext context)
        {
            m_Context = context;
        }

        public override Func<IEnumerable<OutputMember>> RewriteOperation(Func<IEnumerable<OutputMember>> operationBuilder)
        {
            return () =>
            {
                try
                {
                    return operationBuilder();
                }
                catch (Exception ex)
                {

                    if (ex.InnerException != null)
                        ex = ex.InnerException;
                    var error = new Error
                    {
                        Exception = ex,
                        Message = ex.Message,
                        Title = ex.Message
                    };
                    var errors = new List<Error> { error }; 


                    var response = new OperationResult.BadRequest{ ResponseResource = new ServerError{Error = ex.Message},Errors = errors};
                    m_Context.OperationResult = response;

                    var outputMember = new OutputMember
                                                    {
                                                        Value = response,
                                                        Member = new ReflectionBasedType(TypeSystems.Default, response.GetType())
                                                    };
                    return new[] { outputMember }; 
                }
            };
        }
    }
}