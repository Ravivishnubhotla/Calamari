﻿using System;
using System.Collections.Generic;
using System.Threading;
using Calamari.Integration.Processes;
using Calamari.Integration.Processes.Semaphores;
using NUnit.Framework;

namespace Calamari.Tests.Fixtures.Processes
{
    [TestFixture]
    public class SemaphoreFixture
    {
        [Test]
        [Category("SystemSemaphore")]
        public void SystemSemaphoreShouldIsolate()
        {
            ShouldIsolate(new SystemSemaphore());
        }

        [Test]
        [Category("FileBasedSemaphore")]
        public void FileBasedSempahoreShouldIsolate()
        {
            ShouldIsolate(new FileBasedSempahore());
        }

        public void ShouldIsolate(ISemaphore semaphore)
        {
            var result = 0;
            var threads = new List<Thread>();

            for (var i = 0; i < 4; i++)
            {                
                threads.Add(new Thread(new ThreadStart(delegate
                {
                    using (semaphore.Acquire("CalamariTest", "Another process is performing arithmetic, please wait"))
                    {
                        result = 1;
                        Thread.Sleep(200);
                        result = result + 1;
                        Thread.Sleep(200);
                        result = result + 1;
                    }
                })));
            }

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            Assert.That(result, Is.EqualTo(3));
        }


        [Test]
        [Category("SystemSemaphore")]
        public void SystemSemaphoreWorksProperly()
        {
            SemaphoreWorksProperly(new SystemSemaphore());
        }

        [Test]
        [Category("FileBasedSemaphore")]
        public void FileBasedSempahoreWorksProperly()
        {
            SemaphoreWorksProperly(new FileBasedSempahore());
        }

        public void SemaphoreWorksProperly(ISemaphore semaphore)
        {
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            var threadTwoShouldGetSemaphore = true;

            var threadOne = new Thread(() =>
            {
                using (semaphore.Acquire("Octopus.Calamari.TestSemaphore", "Another process has the semaphore..."))
                {
                    threadTwoShouldGetSemaphore = false;
                    autoEvent.Set();
                    Console.WriteLine("Thread 1 has the semaphore");
                    Thread.Sleep(200);
                    Console.WriteLine("Thread 1 has finished with the semaphore");
                    threadTwoShouldGetSemaphore = true;
                }
                Console.WriteLine("Thread 1 has released the semaphore");
            });

            var threadTwo = new Thread(() =>
            {
                autoEvent.WaitOne();
                using (semaphore.Acquire("Octopus.Calamari.TestSemaphore", "Another process has the semaphore..."))
                {
                    Assert.That(threadTwoShouldGetSemaphore, Is.True);
                    Console.WriteLine("Thread 2 has the semaphore");
                }
                Console.WriteLine("Thread 2 has release the semaphore");
            });

            threadOne.Start();
            threadTwo.Start();
            threadOne.Join();
            threadTwo.Join();
        }
    }
}
