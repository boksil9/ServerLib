using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLib
{
    public class CircularQueue<T>
    {
        int _maxSize = 0;
        int _head = 0;
        int _tail = 0;

        List<T> _queue = new List<T>();

        public CircularQueue(int maxSize)
        {
            _queue.Capacity = maxSize;
            _maxSize = maxSize;
        }

        public bool Push(T data)
        {
            int now = (_head + 1) % _maxSize;
            if(now == _tail)
                return false;

            _queue[_head] = data;
            _head = now;

            return true;
        }

        public T Pop()
        {
            if (_head == _tail)
                return default(T);

            T ret = _queue[_tail];
            _tail = (_tail + 1) % _maxSize;

            return ret;
        }

        public void Clear() { _head = _tail = 0;}
        public bool Empty(){ return _head == _tail ? true : false; }
    }
}
