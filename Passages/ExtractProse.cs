using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class ExtractProse : Extraction
    {
        private Passage root;

        public ExtractProse(Passage root)
        {
            this.root = root;
        }

        public override IEnumerator<string>  ImplementGetEnumerator()
        {
 	        if (String.IsNullOrEmpty(root.name) || root.name[0] != '*')
            {
                if (root.Leaf)
                {
                    yield return root.name;
                }
                else
                    foreach (var child in root.children)
                        foreach (var item in new ExtractProse(child))
                            yield return item;
            }
        }
    }
}
