using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoBitmaps
{
    public static class ExtensionClass
    {
        public static int Clamp(this int val, int min, int max)
        {
            return ((val < min) ? min : (val > max) ? max : val);
        }
    }
}
