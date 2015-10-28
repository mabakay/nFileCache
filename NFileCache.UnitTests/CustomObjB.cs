using System;

namespace nFC.UnitTests
{
    [Serializable]
    public class CustomObjB
    {
        public CustomObjA Obj { get; set; }
        public int Num { get; set; }
    }
}
