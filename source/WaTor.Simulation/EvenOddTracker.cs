using System;

namespace WaTor.Simulation
{
    public class EvenOddTracker
    {
        public EvenOddTracker(int totalEven, int totalOdd)
        {
            _totalEven = totalEven;
            _totalOdd = totalOdd;
            //this.evenSemaphore = new Semaphore(totalEven, totalEven);
            //this.oddSemaphore = new Semaphore(0, totalOdd);
        }

        readonly int _totalEven, _totalOdd;
        //Semaphore evenSemaphore, oddSemaphore;
        object _mutex = new { };
        volatile bool _doingEven = true;
        volatile int _activeCount = 0;

        public void Do(bool isEven, Action action)
        {
            if (isEven) DoEven(action);
            else DoOdd(action);
        }

        public void DoEven(Action action)
        {
            if (_totalOdd == 0)
            {
                action();
                return;
            }


            while (!_doingEven) { }
            while (true)
            {
                lock (_mutex)
                {
                    if (_doingEven)
                    {
                        _activeCount++;
                        break;
                    }
                }
            }

            action();

            lock (_mutex)
            {
                if (_activeCount >= _totalEven)
                {
                    _activeCount = 0;
                    _doingEven = false;
                }
            }

        }

        public void DoOdd(Action action)
        {
            if (_totalEven == 0)
            {
                action();
                return;
            }

            while (_doingEven) { }
            while (true)
            {
                lock (_mutex)
                {
                    if (!_doingEven)
                    {
                        _activeCount++;
                        break;
                    }
                }
            }

            action();

            lock (_mutex)
            {
                if (_activeCount >= _totalOdd)
                {
                    _activeCount = 0;
                    _doingEven = true;
                }
            }
        }
    }
}
