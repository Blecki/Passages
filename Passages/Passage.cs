using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Passages
{
    public class Passage
    {
        public Passage parent;
        public List<Passage> children;
        public String summary;
        public String name;
        public bool Leaf;

        public Passage()
        {
            children = new List<Passage>();
            Leaf = true;
        }
    }
}
