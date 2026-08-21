using System;
using System.Collections.Generic;

namespace SushiSurvival.Core
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public int InactiveCount => _inactive.Count;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
        }

        public T Get()
        {
            T item = _inactive.Count > 0 ? _inactive.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            _onRelease?.Invoke(item);
            _inactive.Push(item);
        }
    }
}
