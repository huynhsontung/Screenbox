using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Screenbox.Core.Factories
{
    public class GenericFactory<T>
    {
        private readonly Func<T> _generator;

        public GenericFactory(Func<T> generator)
        {
            _generator = generator;
        }


    }
}
