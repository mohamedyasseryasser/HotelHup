using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.General
{
    public class ResponseStatus<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; } 
        public List<string> Errors { get; set; } = new();
        public int StatusCode { get; set; }

        public ResponseStatus(T data, string message = "", int statusCode = 200)
        {
            Success = true;
            Message = message;
            Data = data;
            StatusCode = statusCode;
        }

        public ResponseStatus(string message, IEnumerable<string>? errors = null, int statusCode = 400)
        {
            Success = false;
            Message = message;
            Errors = errors?.ToList() ?? new List<string>();
            StatusCode = statusCode;
        }
    }
}
