﻿using Microsoft.AspNetCore.Http;

namespace AttributeApi.Services.Parameters.Binders.Interfaces;

public interface IFromHeaderParameterBinder : IParameterBinder<IHeaderDictionary>;