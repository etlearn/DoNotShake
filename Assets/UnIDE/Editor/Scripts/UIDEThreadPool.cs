using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.Threading;

namespace UIDE {
	public class ThreadPoolItem:System.Object {
		public string id = "";
		public WaitCallback callback;
		public System.Object paramObj;
		public bool hasParamObj = false;
		public float startTime = 0.0f;
		public float timeout = 0.0f;
		//public Thread thread;
		//private Timer timer;
		public ThreadPoolItem(string id, WaitCallback callback) {
			id = id.ToLower();
			this.id = id;
			this.callback = callback;
			
		}
		public ThreadPoolItem(string id, WaitCallback callback,System.Object paramObj) {
			id = id.ToLower();
			this.id = id;
			this.callback = callback;
			this.paramObj = paramObj;
			hasParamObj = true;
		}
		/*
		public void Start() {
			//if (thread != null && thread.IsAlive) {
			//	return;
			//}
			
			//if (!UIDEThreadPool.useRealThreads) {
				if (hasParamObj) {
					ThreadPool.QueueUserWorkItem((o) => {try {callback(o);}catch(System.Exception){UIDEThreadPool.UnregisterThread(id,false);}finally{UIDEThreadPool.UnregisterThread(id,false);}},paramObj);
				}
				else {
					ThreadPool.QueueUserWorkItem((o) => {try {callback(o);}catch(System.Exception){UIDEThreadPool.UnregisterThread(id,false);}finally{UIDEThreadPool.UnregisterThread(id,false);}});
				}
			//}
			//else {
				//
				//thread = new Thread(Run);
				//thread.IsBackground = true;
				//
				//startTime = UIDEThreadPool.currentTime;
				//UIDEThreadPool.runningItems.Add(this);
				//thread.Start();
				//
			//}
		}
		*/
		/*
		public void Run() {
			try {
				callback(paramObj);
			}
			catch(System.Exception){
				UIDEThreadPool.UnregisterThread(id,false);
				UIDEThreadPool.runningItems.Remove(this);
				//if (thread != null && thread.IsAlive) {
				//	thread.Abort();
				//}
			}
			finally{
				UIDEThreadPool.UnregisterThread(id,false);
				UIDEThreadPool.runningItems.Remove(this);
				//if (thread != null && thread.IsAlive) {
				//	thread.Abort();
				//}
			}
			
		}
		
		public void Abort() {
			UIDEThreadPool.UnregisterThread(id,true);
			UIDEThreadPool.runningItems.Remove(this);
			if (thread != null && thread.IsAlive) {
				thread.Abort();
			}
		}
		
		public void OnDestroy() {
			if (thread != null && thread.IsAlive) {
				thread.Abort();
			}
			//timer.Dispose();
		}
		*/
		//void OnTimeOut(object state) {
		//	Abort();
		//}
	}
	
	static public class UIDEThreadPool:System.Object {
		static public float timeout = 1.0f;
		static public float currentTime = 0.0f;
		//static public int maxThreads = 1;
		//static public bool useRealThreads = false;
		//static public List<ThreadPoolItem> queuedItems = new List<ThreadPoolItem>();
		//static public List<ThreadPoolItem> runningItems = new List<ThreadPoolItem>();
		static private Dictionary<string,ThreadPoolItem> registeredThreads = new Dictionary<string,ThreadPoolItem>();
		
		/*
		static public void SetMinThreads(int workerThreads, int completionPortThreads) {
			ThreadPool.SetMinThreads(workerThreads,completionPortThreads);
		}
		static public void SetMaxThreads(int workerThreads, int completionPortThreads) {
			ThreadPool.SetMaxThreads(workerThreads,completionPortThreads);
		}
		static public void SetMinMaxThreads(int workerThreads, int completionPortThreads) {
			SetMinThreads(workerThreads,completionPortThreads);
			SetMaxThreads(workerThreads,completionPortThreads);
		}
		*/
		
		static public void Update() {
			/*
			if (useRealThreads) {
				if (Application.platform == RuntimePlatform.OSXEditor) {
					maxThreads = 1;
				}
				else {
					int t0 = 0;
					int t1 = 0;
					ThreadPool.GetMaxThreads(out t0,out t1);
					maxThreads = t0;
				}
				//Debug.Log(maxThreads);
				
				ThreadPoolItem[] itemsToCheck = runningItems.ToArray();
				for (int i = 0; i < itemsToCheck.Length; i++) {
					float actualTimeout = itemsToCheck[i].timeout;
					if (actualTimeout <= 0.0f) {
						actualTimeout = timeout;
					}
					if (currentTime-itemsToCheck[i].startTime > actualTimeout || currentTime < itemsToCheck[i].startTime) {
						itemsToCheck[i].Abort();
					}
				}
				
				if (runningItems.Count < maxThreads &&  queuedItems.Count > 0) {
					queuedItems[0].Start();
					queuedItems.RemoveAt(0);
				}
			}
			//Debug.Log(runningItems.Count+" "+queuedItems.Count+" "+maxThreads);
			*/
		}
		
		static public void RegisterThread(string id, WaitCallback callback) {
			//return;
			id = id.ToLower();
			
			ThreadPool.QueueUserWorkItem((o) => {try {callback(o);}catch(System.Exception){UnregisterThread(id);}finally{UnregisterThread(id);}});
			//if (!IsRegistered(id)) {
				ThreadPoolItem newItem = new ThreadPoolItem(id,callback);
				//if (useRealThreads && runningItems.Count >= maxThreads) {
				//	queuedItems.Add(newItem);
				//}
				//else {
				//	newItem.Start();
				//}
				registeredThreads.Add(id,newItem);
			//}
		}
		static public void RegisterThread(string id, WaitCallback callback, System.Object obj) {
			//return;
			id = id.ToLower();
			ThreadPool.QueueUserWorkItem((o) => {try {callback(o);}catch(System.Exception){UnregisterThread(id);}finally{UnregisterThread(id);}},obj);
			//if (!IsRegistered(id)) {
				ThreadPoolItem newItem = new ThreadPoolItem(id,callback,obj);
				//if (useRealThreads && runningItems.Count >= maxThreads) {
				//	queuedItems.Add(newItem);
				//}
				//else {
				//	newItem.Start();
				//}
				registeredThreads.Add(id,newItem);
			//}
		}
		
		static public void UnregisterThread(string id) {
			id = id.ToLower();
			if (registeredThreads.ContainsKey(id)) {
				ThreadPoolItem item = null;
				registeredThreads.TryGetValue(id,out item);
				//if (item != null) {
				//	if (abort && item.thread != null && item.thread.IsAlive) {
				//		item.thread.Abort();
				//	}
				//}
				//item.OnDestroy();
				//runningItems.Remove(item);
				registeredThreads.Remove(id);
			}
			//if (registeredThreads.ContainsKey(id)) {
			//	UnregisterThread(id,abort);
			//}
			//while (IsRegistered(id)) {
			//	UnregisterThread(id,abort);
			//}
		}
		
		static public bool IsRegistered(string id) {
			id = id.ToLower();
			
			//if (!useRealThreads) {
				ThreadPoolItem item = null;
				registeredThreads.TryGetValue(id,out item);
				if (item == null) return false;
				
				float timeOutToCheck = timeout;
				if (item.timeout > 0.0f) {
					timeOutToCheck = item.timeout;
				}
				
				if (currentTime-item.startTime >= timeOutToCheck || currentTime < item.startTime) {
					UnregisterThread(id);
				}
			//}
			
			return registeredThreads.ContainsKey(id);
		}
	}
}
