import { test } from 'node:test'
import assert from 'node:assert/strict'
import { parseBackupJson } from '../../docs/.vitepress/data/backupJson.ts'
test('reject duplicate v2 IDs, including escaped names, before JSON parsing discards them', () => {
  assert.throws(() => parseBackupJson('{"units":{"a":1,"a":2}}'), /重复/)
  assert.throws(() => parseBackupJson('{"units":{"a":1,"\\u0061":2}}'), /重复/)
  assert.deepEqual(parseBackupJson('{"records":[{"id":"a","record":null},{"id":"b","record":{"nested":[true,false,2,"a\\\"b"]}}]}'),
    { records: [{ id: 'a', record: null }, { id: 'b', record: { nested: [true,false,2,'a"b'] } }] })
  assert.throws(() => parseBackupJson('{'), SyntaxError)
})
