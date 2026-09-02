using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.General
{
    public class ResponseStatus<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Code { get; init; }
        public string? CorrelationId { get; init; }
        public T? Data { get; init; }
        public List<string> Errors { get; init; } = new();
        public int StatusCode { get; init; }

        public ResponseStatus(T data, string message = "",  int statusCode = 200, string? code = null, string? correlationId = null)
        {
            Success =true;
            Message = message;
            Data = data;
            Errors =  new List<string>();
            StatusCode = statusCode;
            Code = code;
            CorrelationId = correlationId;
        }

        public ResponseStatus(string message = "",  List<string>? errors = null, int statusCode = 400, string? code = null, string? correlationId = null)
        {
            Success = false;
            Message = message;
            Errors = errors ?? new List<string>();
            StatusCode = statusCode;
            Code = code;
            CorrelationId = correlationId;
        }
    }
}