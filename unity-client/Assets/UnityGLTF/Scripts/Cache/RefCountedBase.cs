using UnityEngine;

namespace UnityGLTF.Cache
{
    public abstract class RefCountedBase
    {
        private bool _isDisposed = false;

        private int _refCount = 0;
        private readonly object _refCountLock = new object();

        public int RefCount { get { return _refCount; } }

        public void IncreaseRefCount()
        {
            if (_isDisposed)
            {
                Debug.LogError("Cannot inscrease the ref count on disposed cache data.");
                return;
            }

            lock (_refCountLock)
            {
                _refCount++;
            }

            OnIncreaseRefCount();
        }

        public void DecreaseRefCount()
        {
            if (_isDisposed)
            {
                Debug.LogError("Cannot decrease the ref count on disposed cache data.");
                return;
            }

            lock (_refCountLock)
            {
                if (_refCount <= 0)
                {
                    Debug.LogError("Cannot decrease the cache data ref count below zero. Name = " + this.ToString());
                    return;
                }

                _refCount--;
            }

            OnDecreaseRefCount();

            if (_refCount <= 0)
            {
                DestroyCachedData();
            }
        }

        private void DestroyCachedData()
        {
            if (!_isDisposed)
            {
                OnDestroyCachedData();
            }

            _isDisposed = true;
        }

        protected abstract void OnDestroyCachedData();

        protected virtual void OnIncreaseRefCount()
        {
        }
        protected virtual void OnDecreaseRefCount()
        {
        }
    }
}
