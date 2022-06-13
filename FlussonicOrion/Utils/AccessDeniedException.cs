using System;

namespace FlussonicOrion.Utils
{
    public class AccessDeniedException: Exception
    {
        public AccessDeniedException(string reason)
        {
            Reason = reason;
        }
        public string Reason;
    }
}
