using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMI.AMIData.Database
{
    class DataEntry<K>
    {
        public K _id;

        public DataEntry(K id)
        {
            _id = id;
        }
    }
}
