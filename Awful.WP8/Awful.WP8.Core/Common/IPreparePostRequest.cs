using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Awful
{
    internal interface IPreparePostRequest
    {
        IRestRequest PreparePostRequest(IRestRequest request);
    }
}
