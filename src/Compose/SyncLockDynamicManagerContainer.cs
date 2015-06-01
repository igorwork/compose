﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Compose
{
	internal sealed class SyncLockDynamicManagerContainer<TInterface, TOriginal> : IDynamicManagerContainer<TInterface, TOriginal>
		where TInterface : class where TOriginal : TInterface
	{
		private readonly List<DynamicManager<TInterface, TOriginal>> Managers
			= new List<DynamicManager<TInterface, TOriginal>>();
		private readonly object SyncLock = new object();

		private static TypeInfo Disposable = typeof(IDisposable).GetTypeInfo();

		public void Add(DynamicManager<TInterface, TOriginal> manager)
		{
			lock (SyncLock)
			{
				Managers.Add(manager);
			}
		}

		private IEnumerable<DynamicManager<TInterface, TOriginal>> GetActiveManagers()
		{
			var deadReferences = new List<DynamicManager<TInterface, TOriginal>>(Managers.Count);

			lock (SyncLock)
			{
				foreach (var manager in Managers)
					if (manager.IsActive)
						yield return manager;
					else
						deadReferences.Add(manager);

				foreach (var deadReference in deadReferences)
					Managers.Remove(deadReference);
			}
		}

		public void Change(Func<TInterface> service)
		{
			foreach (var instance in GetActiveManagers())
				Change(instance, service());
		}

		private void Change(DynamicManager<TInterface, TOriginal> manager, TInterface service)
		{
			if (manager.CurrentService != null && Disposable.IsAssignableFrom(manager.CurrentService.GetType().GetTypeInfo()))
				((IDisposable)manager.CurrentService).Dispose();
			manager.CurrentService = service;
		}

		public void Snapshot()
		{
			foreach (var manager in GetActiveManagers())
				Snapshot(manager);
		}

		private void Snapshot(DynamicManager<TInterface, TOriginal> manager)
		{
			if (manager.SnapshotService != null && Disposable.IsAssignableFrom(manager.SnapshotService.GetType().GetTypeInfo()))
				((IDisposable)manager.SnapshotService).Dispose();
			manager.SnapshotService = manager.CurrentService;
		}

		public void Restore()
		{
			foreach (var manager in GetActiveManagers())
				Restore(manager);
		}

		private void Restore(DynamicManager<TInterface, TOriginal> manager)
		{
			Change(manager, manager.SnapshotService);
		}
	}
}
