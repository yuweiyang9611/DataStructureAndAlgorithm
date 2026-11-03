import { emptyStore, migrateLegacy, type ProgressStore } from '../data/progress.ts'
import { ProgressRepository } from '../data/progressRepository.ts'

const databaseName = 'dsa-learning-progress'

/**
 * Reads the record the units page keeps, without ever writing it. A database opened here by name
 * alone would be created without its `progress` store and break the units page, so it is opened
 * only when it already exists, or through ProgressRepository, which creates it the same way.
 * The opening screen and the band on a guide page both read through this.
 */
export async function readRecord(): Promise<ProgressStore> {
  let existed = true
  try {
    if (typeof indexedDB.databases === 'function') {
      existed = (await indexedDB.databases()).some((database) => database.name === databaseName)
    }
    if (existed) {
      const repository = await ProgressRepository.open()
      try {
        const current = await repository.read()
        if (current.revision > 0 || current.generation > 0) return current
      } finally {
        repository.close()
      }
    }
  } catch {
    // Storage can be disabled; the legacy record below is still worth showing.
  }
  try {
    return migrateLegacy(localStorage, Date.now())
  } catch {
    return emptyStore()
  }
}
