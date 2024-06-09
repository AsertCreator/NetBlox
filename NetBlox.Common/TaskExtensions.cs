namespace NetBlox
{
	public static class TaskExtensions
	{
		public static Task<T> AsCancellable<T>(this Task<T> task, CancellationToken token)
		{
			if (!token.CanBeCanceled)
			{
				return task;
			}

			var tcs = new TaskCompletionSource<T>();

			token.Register(() => tcs.TrySetCanceled(token),
				useSynchronizationContext: false);

			task.ContinueWith(t =>
			{
				if (task.IsCanceled)
				{
					tcs.TrySetCanceled();
				}
				else if (task.IsFaulted)
				{
					tcs.TrySetException(t.Exception);
				}
				else
				{
					tcs.TrySetResult(t.Result);
				}
			},
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);

			return tcs.Task;
		}
	}
}
