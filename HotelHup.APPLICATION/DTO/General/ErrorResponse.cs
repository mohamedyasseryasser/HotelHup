using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.DTO.General
{
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public List<string> Errors { get; set; }= new List<string>();

        public ErrorResponse(string message, string errorCode = "INTERNAL_ERROR", List<string> errors = null)
        {
            Success = false;
            Message = message;
            ErrorCode = errorCode;
            Errors = errors ?? new List<string>();
        }
    }
}
