using System.Collections.Generic;

namespace Antlr4.Build.Tasks
{
    public class ListSection<T>
    {
        private readonly List<T> _list;
        private readonly int _base;
        private readonly int _len;
        public List<T> List => _list;
        public int Base => _base;
        public int Len => _len;

        public ListSection(List<T> list, int b, int l)
        {
            _list = list;
            _base = b;
            _len = l;
        }

        public T this[int i]
        {
            get => _list[_base + i];
            set => _list[_base + i] = value;
        }

        private static void Resize(ref ListSection<T> arr, int new_length) { }
    }
}
