using System.Collections.Generic;

namespace Util
{
    public class LabelDictionary<T,U> : Dictionary<T,U>
    {
        public int label = -1; // -1로 초기화하여 0과 구분
    }
}
