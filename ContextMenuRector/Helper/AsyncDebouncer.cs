using System;
using System.Threading;
using System.Threading.Tasks;

namespace ContextMenuRector.Helper
{
    internal class AsyncDebouncer
    {
        private CancellationTokenSource _cts;
        private readonly TimeSpan _delay;

        public AsyncDebouncer(TimeSpan delay)
        {
            _delay = delay;
        }

        public async Task DebounceAsync(Func<Task> action)
        {
            // 1. 取消上一次未完成的任务
            _cts?.Cancel();
            _cts?.Dispose();

            // 2. 创建新的 Token
            var cts = new CancellationTokenSource();
            _cts = cts;

            try
            {
                // 3. 等待延迟时间
                await Task.Delay(_delay, cts.Token);

                // 4. 如果没有被取消，执行动作
                await action();
            }
            catch (TaskCanceledException)
            {
                // 忽略因取消导致的异常，这是预期行为
            }
        }
    }
}
