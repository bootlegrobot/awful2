using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful.Common
{
    public interface IUpdateable<T>
    {
        void Update(T updated);
    }
}
