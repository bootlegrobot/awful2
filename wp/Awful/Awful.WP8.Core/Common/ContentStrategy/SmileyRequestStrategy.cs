using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
    public abstract class SmileyRequestStrategy
    {
        protected abstract Uri GetSmileyPageUri();

        public abstract IEnumerable<TagMetadata> LoadSmilies();
    }
}
