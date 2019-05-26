﻿using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour {
  static ThreadedDataRequester instance;
  Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

  private void Awake() {
    instance = FindObjectOfType<ThreadedDataRequester>();
  }

  public static void requestData(Func<object> generateData, Action<object> callback) {
    ThreadStart threadStart = delegate {
      instance.dataThread(generateData, callback);
    };

    new Thread(threadStart).Start();
  }

  void dataThread(Func<object> generateData, Action<object> callback) {
    object data = generateData();

    lock (dataQueue) {
      dataQueue.Enqueue(new ThreadInfo(callback, data));
    }
  }

  void Update() {
    if (dataQueue.Count > 0) {
      for (int i = 0; i < dataQueue.Count; i++) {
        ThreadInfo threadInfo = dataQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }
  }

  struct ThreadInfo {
    public readonly Action<object> callback;
    public readonly object parameter;

    public ThreadInfo(Action<object> callback, object parameter) {
      this.callback = callback;
      this.parameter = parameter;
    }
  }
}
