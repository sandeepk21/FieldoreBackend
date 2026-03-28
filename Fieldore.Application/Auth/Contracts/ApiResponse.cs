using System;

namespace Fieldore.Application.Auth.Contracts;

public class ApiResponse<T>
{
     public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Create(T? data, bool success, string? message, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = success,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }
}