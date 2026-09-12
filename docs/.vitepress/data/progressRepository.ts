import { applyAction, emptyStore, mergeBackup, type ImportData, type ProgressAction, type ProgressStore } from './progress.ts'

export class StaleProgressError extends Error {
  constructor() { super('进度已在其他页面重置，请根据最新状态重新操作。') }
}
/** All read-modify-write operations share one readwrite transaction scope. */
export class ProgressRepository {
  private db: IDBDatabase
  constructor(db: IDBDatabase) { this.db = db }
  static open(factory: IDBFactory = indexedDB): Promise<ProgressRepository> {
    return new Promise((resolve, reject) => {
      let abandoned = false
      const request = factory.open('dsa-learning-progress', 1)
      request.onupgradeneeded = () => request.result.createObjectStore('progress')
      request.onerror = () => reject(request.error)
      request.onblocked = () => { abandoned = true; reject(new Error('请关闭旧版学习页面后重试。')) }
      request.onsuccess = () => {
        if (abandoned) { request.result.close(); return }
        request.result.onversionchange = () => request.result.close()
        resolve(new ProgressRepository(request.result))
      }
    })
  }
  private transaction(update?: (store: ProgressStore | undefined) => ProgressStore): Promise<ProgressStore> {
    return new Promise((resolve, reject) => {
      const transaction = this.db.transaction('progress', update ? 'readwrite' : 'readonly')
      const entries = transaction.objectStore('progress')
      const request = entries.get('current')
      let result: ProgressStore
      let failure: unknown
      request.onsuccess = () => {
        try {
          result = update ? update(request.result) : request.result ?? emptyStore()
          if (update) entries.put(result, 'current')
        } catch (error) { failure = error; transaction.abort() }
      }
      transaction.oncomplete = () => resolve(result)
      transaction.onabort = () => reject(failure ?? transaction.error ?? new Error('保存事务中断。'))
      transaction.onerror = () => { /* onabort owns the rejection. */ }
    })
  }
  initialize(legacy: ProgressStore) { return this.transaction(current => current ?? legacy) }
  read() { return this.transaction() }
  dispatch(action: ProgressAction, expected: { generation: number; deletedAt: number }, now: number) {
    return this.transaction(current => {
      const store = current ?? emptyStore()
      if (store.generation !== expected.generation || ('id' in action && (store.deleted[action.id] ?? 0) !== expected.deletedAt)) throw new StaleProgressError()
      return applyAction(store, action, now)
    })
  }
  import(data: ImportData, expectedRevision: number) {
    return this.transaction(current => {
      const store = current ?? emptyStore()
      if (store.revision !== expectedRevision) throw new StaleProgressError()
      return mergeBackup(store, data).store
    })
  }
  close() { this.db.close() }
}
