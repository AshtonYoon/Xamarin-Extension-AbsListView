using System.Collections;
using System.Collections.Generic;
using Aurender.Core.Data.DB.Windowing;

namespace Aurender.Core.Data.DB
{
    class DBItemEnumerator<T> : IEnumerator<T> where T : IDatabaseItem
    {
        private WindowedData<T> data;
        int position = -1;

        public T Current => data.ItemAt(position);

        object IEnumerator.Current => Current;

        internal DBItemEnumerator(WindowedData<T> items)
        {
            this.data = items;
        }

        public void Dispose()
        {
            data = null;
        }

        public bool MoveNext()
        {
            position++;
            return (position < data.totalItemCount);
        }

        public void Reset()
        {
            position = -1;
        }
    }
}