
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    interface ILastUpdated
    {
        DateTime? LastUpdated { get; set; }
    }
}
