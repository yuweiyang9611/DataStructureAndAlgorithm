import test from 'node:test'
import assert from 'node:assert/strict'
import {
  blobUrl, describeEntry, directoryHref, fileName, inRange, listDirectory, parentOf, parseRange, siblingsOf, sourceHref
} from '../../docs/.vitepress/theme/sourceBundle.ts'

const files = [
  { path: 'DataStructureAndAlgorithm/Tree/AvlTree.cs', bytes: 2048, lines: 80 },
  { path: 'DataStructureAndAlgorithm/Tree/Trie.cs', bytes: 1024, lines: 40 },
  { path: 'DataStructureAndAlgorithm/Sorting/SortAlgorithms.cs', bytes: 512, lines: 20 },
  { path: 'DataStructureAndAlgorithm/DataStructureAndAlgorithm.csproj', bytes: 300, lines: 12 },
  { path: 'README.md', bytes: 100, lines: 5 }
]

test('in-site links keep the path whole and a line range in the hash', () => {
  assert.equal(sourceHref('DataStructureAndAlgorithm/Tree/AvlTree.cs'), '/源码.html?path=DataStructureAndAlgorithm%2FTree%2FAvlTree.cs')
  assert.equal(sourceHref('a b.cs', [5, 9]), '/源码.html?path=a%20b.cs#L5-L9')
  assert.equal(sourceHref('a.cs', [7, 7]), '/源码.html?path=a.cs#L7')
  assert.equal(directoryHref('DataStructureAndAlgorithm/Tree/'), '/源码.html?dir=DataStructureAndAlgorithm%2FTree')
  assert.equal(directoryHref(''), '/源码.html')
  assert.equal(blobUrl('https://github.com/o/r/blob/main/', 'x/y z.cs'), 'https://github.com/o/r/blob/main/x/y%20z.cs')
})

test('a line range reads #L5-L9, #L5-9 and #L5, in order, and nothing else', () => {
  assert.deepEqual(parseRange('#L5-L9'), [5, 9])
  assert.deepEqual(parseRange('L9-5'), [5, 9])
  assert.deepEqual(parseRange('#L12'), [12, 12])
  assert.equal(parseRange('#unit-core-sorting'), null)
  assert.equal(parseRange(''), null)
  assert.ok(inRange(7, [[5, 9]]) && !inRange(10, [[5, 9]]))
})

test('a directory lists its subdirectories first, with files and lines added up', () => {
  const root = listDirectory(files, '')
  assert.deepEqual(root.map((entry) => entry.name), ['DataStructureAndAlgorithm', 'README.md'])
  assert.deepEqual(root[0], { name: 'DataStructureAndAlgorithm', path: 'DataStructureAndAlgorithm', directory: true, files: 4, bytes: 3884, lines: 152 })
  const library = listDirectory(files, 'DataStructureAndAlgorithm/')
  assert.deepEqual(library.map((entry) => `${entry.name}${entry.directory ? '/' : ''}`), ['Sorting/', 'Tree/', 'DataStructureAndAlgorithm.csproj'])
  assert.equal(describeEntry(library[1]), '2 个文件 · 120 行')
  assert.equal(describeEntry(library[2]), '12 行 · 0.3 KB')
  assert.deepEqual(listDirectory(files, 'Nowhere'), [])
})

test('siblings are the other files beside a path', () => {
  assert.deepEqual(siblingsOf(files, 'DataStructureAndAlgorithm/Tree/AvlTree.cs').map((file) => file.path), ['DataStructureAndAlgorithm/Tree/Trie.cs'])
  assert.deepEqual(siblingsOf(files, 'README.md'), [])
  assert.equal(parentOf('README.md'), '')
  assert.equal(parentOf('a/b/c.cs'), 'a/b')
  assert.equal(fileName('a/b/c.cs'), 'c.cs')
})
