using System.Collections;
using System.Reflection;
using AttributeApi.Services.Builders;
using AttributeApi.Services.Parameters.Binders.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AttributeApi.Services.Parameters.Binders.Core;

internal class DefaultFromQueryParameterBinder : IFromQueryParameterBinder
{
    private readonly Type _enumerableType = typeof(IEnumerable);
    private readonly Type _listType = typeof(List<>);

    public Task<IEnumerable<BindParameter>> BindParametersAsync(List<ParameterInfo> parameters, IQueryCollection queryCollection)
    {
        var fromQueryParameters = parameters.Where(parameter => parameter.GetCustomAttribute<FromQueryAttribute>() is not null).ToList();
        var resolvedParameters = new List<BindParameter>(fromQueryParameters.Count);

        foreach (var parameter in fromQueryParameters)
        {
            object? resolvedParameter;
            var name = parameter.Name!;

            // verifying if there is any values which we are expecting.
            if (queryCollection.TryGetValue(name, out var stringValues))
            {
                // if value exists, and it's empty, we are trying insert a default value.
                if (stringValues.Count == 0)
                {
                    resolvedParameters.Add(new(name, parameter.HasDefaultValue ? parameter.DefaultValue : null));

                    continue;
                }

                Type? element;
                var returnType = parameter.ParameterType;
                var isArray = returnType.IsArray;

                // verifying the behavior for the parameter after the resolving 
                // all of his objects.
                if (isArray)
                {
                    element = returnType.GetElementType();
                }
                else if (returnType.IsAssignableTo(_enumerableType))
                {
                    element = returnType.GetGenericArguments().First();
                }
                // if parameter is not an array or enumerable, we try to convert type of single element;
                // and then continue iteration.
                else
                {
                    resolvedParameter = Convert.ChangeType(stringValues[0], returnType);
                    resolvedParameters.Add(new(name, resolvedParameter));

                    continue;
                }

                var temp = new List<object>(stringValues.Count);

                // for performance purposes, we allocate memory for additional delegate
                // to resolve object type.
                Action<string> action = DefaultParametersHandler._typeResolvers.TryGetValue(element.Name.ToLowerInvariant(), out var func)
                    ? argument => temp.Add(func(argument))
                    : argument => temp.Add(Convert.ChangeType(argument, element));

                foreach (var value in stringValues)
                {
                    action(value);
                }

                // proceeding with the actual instance of parameter's type
                // depending on what it is array or enumerable we proceed differently.
                if (isArray)
                {
                    var elementType = returnType.GetElementType();
                    resolvedParameter = Array.CreateInstance(elementType, stringValues.Count);
                    var array = resolvedParameter as Array;

                    for (var i = 0; i < temp.Count; i++)
                    {
                        array.SetValue(temp[i], i);
                    }
                }
                else
                {
                    // as we cannot create interface instance, we check if the return type is Interface member
                    // if true - we create list with generic argument of element type; otherwise - create instance of actual type
                    resolvedParameter = returnType.IsInterface ? Activator.CreateInstance(_listType.MakeGenericType(element)) : Activator.CreateInstance(returnType)!;
                    var list = resolvedParameter as IList;

                    foreach (var obj in temp)
                    {
                        list.Add(obj);
                    }
                }
            }
            // in case of negative verification we are trying to insert a default value
            else
            {
                resolvedParameter = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }

            // finally adding the actual instance into the sorted array
            resolvedParameters.Add(new(name, resolvedParameter));
        }

        return Task.FromResult<IEnumerable<BindParameter>>(resolvedParameters);
    }
}
