using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONTECH.Service.DataAccess
{
    public static class ErrorCodes
    {
        public const byte ValidationError = 1;
        public const byte ConcurrencyViolationError = 2;
        public const int UniqueKeyViolationError = 2627;
        public const int SqlUserRaisedError = 50000;
    }
}
