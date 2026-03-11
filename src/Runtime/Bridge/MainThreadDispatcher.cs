#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// MonoBehaviour that dispatches work from background threads onto the Unity main thread.
    /// Attach to the plugin's GameObject so that Update() is called every frame.
    /// Background IPC handlers use <see cref="RunOnMainThread{T}"/> to safely access Unity APIs.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        /// <summary>
        /// Drains the pending action queue each frame, executing all enqueued work on the main thread.
        /// </summary>
        private void Update()
        {
            int processed = 0;
            while (_queue.TryDequeue(out Action? action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Exception in queued action: {ex}");
                }

                // Safety valve: don't block the frame forever
                processed++;
                if (processed > 100)
                    break;
            }
        }

        /// <summary>
        /// Enqueue an action to run on the Unity main thread. The result is delivered
        /// through the provided <see cref="TaskCompletionSource{T}"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the work.</typeparam>
        /// <param name="work">The function to execute on the main thread.</param>
        /// <param name="tcs">The TaskCompletionSource to signal when work completes.</param>
        public static void Enqueue<T>(Func<T> work, TaskCompletionSource<T> tcs)
        {
            _queue.Enqueue(() =>
            {
                try
                {
                    T result = work();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
        }

        /// <summary>
        /// Schedule a function to run on the Unity main thread and return a Task
        /// that completes with the result. Safe to call from any thread.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="work">The function to execute on the main thread.</param>
        /// <returns>A task that completes when the work finishes on the main thread.</returns>
        public static Task<T> RunOnMainThread<T>(Func<T> work)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            Enqueue(work, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Schedule an action (no return value) to run on the Unity main thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread.</param>
        /// <returns>A task that completes when the action finishes.</returns>
        public static Task RunOnMainThread(Action action)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
