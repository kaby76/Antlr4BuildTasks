using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Antlr4.Build.Tasks.Util
{
    public static class Locker
    {
        private static Mutex _mutex = new Mutex(false, "Antlr4BuildTasksLock");

        public static bool Grab()
        {
            try
            {
                _mutex.WaitOne();
                return true;
            }
            catch (AbandonedMutexException)
            {
                return false;
            }
        }

        public static void Release()
        {
            _mutex.ReleaseMutex();
        }
    }
}
