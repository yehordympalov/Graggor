using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IFromQueryParametersBinder : IParametersBinder<IQueryCollection>;
