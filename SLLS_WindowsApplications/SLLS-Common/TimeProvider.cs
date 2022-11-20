using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLLS_Common {
    public interface ITimeProvider {
        public long Now();
    }
}
