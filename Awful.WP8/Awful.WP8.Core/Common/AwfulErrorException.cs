using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Awful
{
    public enum ExceptionCode
    {
        LoginFailed,
        NoCookies,
    }

    public class AwfulErrorException : Exception
    {
        private readonly ExceptionCode _code;

        public AwfulErrorException(ExceptionCode code) : base(GetMessage(code)) { _code = code; }

        public static string GetMessage(ExceptionCode code)
        {
            return string.Empty;
        }
    }


}
