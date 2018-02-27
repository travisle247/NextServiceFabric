using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenCapture_API
{
    public static class MessageConstant
    {
        public const string MimePartErrorMessage = "Request content was not mime multipart";
        public const string UndefinedAttachment = "There is at lease one undefined attachment";
        public const string KeyCountLimit = "Key Count limit exceed";
        public const string InternalServerError = "There was an internal server error";
        public const string MoreThanOneMetaData = "There is more than one metadata";
        public const string NoMetaData = "There is no metadata";


    }
}
