using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace ProjectAegis.Addressables
{
	public static class AddressableLoader
	{
		public static T LoadSync<T>(string address) where T : UnityEngine.Object
		{
			if (string.IsNullOrEmpty(address)) return null;
			// 키 존재 여부 확인 후 로드해 InvalidKeyException 로그를 방지
			var locHandle = global::UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address, typeof(T));
			locHandle.WaitForCompletion();
			if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null || locHandle.Result.Count == 0)
			{
				global::UnityEngine.AddressableAssets.Addressables.Release(locHandle);
				return null;
			}
			global::UnityEngine.AddressableAssets.Addressables.Release(locHandle);

			AsyncOperationHandle<T> handle = global::UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
			handle.WaitForCompletion();
			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				Debug.LogWarning($"[AddressableLoader] Load failed: {address}");
				return null;
			}
			return handle.Result;
		}

		public static async Task<T> LoadAsync<T>(string address, CancellationToken token = default) where T : UnityEngine.Object
		{
			if (string.IsNullOrEmpty(address)) return null;
			// 키 존재 여부 확인
			var locHandle = global::UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address, typeof(T));
			while (!locHandle.IsDone)
			{
				if (token.IsCancellationRequested)
				{
					global::UnityEngine.AddressableAssets.Addressables.Release(locHandle);
					return null;
				}
				await Task.Yield();
			}
			if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null || locHandle.Result.Count == 0)
			{
				global::UnityEngine.AddressableAssets.Addressables.Release(locHandle);
				return null;
			}
			global::UnityEngine.AddressableAssets.Addressables.Release(locHandle);

			AsyncOperationHandle<T> handle = global::UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
			while (!handle.IsDone)
			{
				if (token.IsCancellationRequested)
				{
					global::UnityEngine.AddressableAssets.Addressables.Release(handle);
					return null;
				}
				await Task.Yield();
			}
			if (handle.Status != AsyncOperationStatus.Succeeded)
			{
				Debug.LogWarning($"[AddressableLoader] Load failed: {address}");
				return null;
			}
			return handle.Result;
		}
	}
}


