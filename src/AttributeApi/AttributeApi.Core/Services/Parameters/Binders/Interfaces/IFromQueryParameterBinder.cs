using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IFromQueryParameterBinder : IParameterBinder<IQueryCollection>;
