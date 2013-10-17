using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class Extraction : IEnumerable<String>
    {
        public virtual IEnumerator<String> ImplementGetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.ImplementGetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return this.ImplementGetEnumerator();
        }
    }
}
