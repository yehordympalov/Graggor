using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IFromQueryParametersesBinder : IParametersBinder<IQueryCollection>;
