import test from 'node:test'
import assert from 'node:assert/strict'
import { highlight, highlightLines, languageFor, splitLines } from '../../docs/.vitepress/theme/codeColors.ts'

const kindsOf = (lines: ReturnType<typeof highlightLines>) => lines.flat().filter((token) => token.kind).map((token) => `${token.kind}:${token.text}`)

test('C# gets the kit token jobs: keyword, type, call, string, number, comment and meta', () => {
  const source = [
    'using System;',
    '',
    '[TestClass]',
    'public sealed class AvlTree<T> : IOrderedSet<T>',
    '{',
    '    // Balance after an insert.',
    '    private static int Height(Node? node) => node?.Height ?? 0;',
    '    public string Name => "avl";',
    '    public const double Ratio = 1.5;',
    '}'
  ].join('\n')
  const lines = highlightLines(source, 'csharp')
  assert.equal(lines.length, 10)
  const kinds = kindsOf(lines)
  assert.ok(kinds.includes('keyword:using') && kinds.includes('keyword:public') && kinds.includes('keyword:class'))
  assert.ok(kinds.includes('type:AvlTree') && kinds.includes('type:IOrderedSet') && kinds.includes('type:Node'))
  assert.ok(kinds.includes('call:Height'), 'a method name before its parenthesis is a call')
  assert.ok(kinds.includes('string:"avl"'))
  assert.ok(kinds.includes('number:1.5') && kinds.includes('number:0'))
  assert.ok(kinds.includes('comment:// Balance after an insert.'))
  assert.ok(kinds.includes('meta:[TestClass]'))
  assert.ok(lines[1].length === 0, 'an empty line holds no tokens')
  // Every token of a line sits in that line, so the text joins back to the source.
  assert.equal(lines.map((line) => line.map((token) => token.text).join('')).join('\n'), source)
})

test('a block comment keeps its colour on every line it spans', () => {
  const lines = highlightLines('/* first\n   second */\nint x = 1;', 'csharp')
  assert.equal(lines.length, 3)
  assert.deepEqual(lines[0], [{ text: '/* first', kind: 'comment' }])
  assert.deepEqual(lines[1], [{ text: '   second */', kind: 'comment' }])
  assert.equal(lines[2].find((token) => token.kind === 'keyword')?.text, 'int')
})

test('line ends and a trailing newline match the repository line count', () => {
  assert.equal(highlightLines('a\r\nb\r\n', 'text').length, 2)
  assert.equal(highlightLines('a\nb', 'text').length, 2)
  assert.deepEqual(splitLines([{ text: 'one\n' }, { text: 'two' }]), [[{ text: 'one' }], [{ text: 'two' }]])
  assert.deepEqual(highlightLines('', 'csharp'), [[]])
})

test('an unknown language is plain text with no kinds', () => {
  assert.deepEqual(highlight('select * from t', 'sql'), [{ text: 'select * from t' }])
  assert.deepEqual(highlight('', 'sql'), [])
})

test('project files, data files and shell lines get their own jobs', () => {
  const xml = highlight('<Project Sdk="Microsoft.NET.Sdk">\n  <!-- note -->\n  <TargetFramework>net11.0</TargetFramework>\n</Project>', 'xml')
  const xmlKinds = xml.filter((token) => token.kind).map((token) => `${token.kind}:${token.text}`)
  assert.ok(xmlKinds.includes('type:Project') && xmlKinds.includes('meta:Sdk') && xmlKinds.includes('string:"Microsoft.NET.Sdk"') && xmlKinds.includes('comment:<!-- note -->'))
  const json = highlight('{ "sdk": { "version": "11.0.100", "rollForward": true } }', 'json')
  const jsonKinds = json.filter((token) => token.kind).map((token) => `${token.kind}:${token.text}`)
  assert.ok(jsonKinds.includes('type:"sdk"') && jsonKinds.includes('string:"11.0.100"') && jsonKinds.includes('keyword:true'))
  const shell = highlight('dotnet test --filter "Name~Sort" # once', 'shell')
  const shellKinds = shell.filter((token) => token.kind).map((token) => `${token.kind}:${token.text}`)
  assert.ok(shellKinds.includes('type:dotnet') && shellKinds.includes('keyword:--filter') && shellKinds.includes('string:"Name~Sort"') && shellKinds.includes('comment:# once'))
})

test('the language follows the extension', () => {
  assert.equal(languageFor('DataStructureAndAlgorithm/Tree/AvlTree.cs'), 'csharp')
  assert.equal(languageFor('Directory.Build.props'), 'xml')
  assert.equal(languageFor('DataStructureAndAlgorithm.slnx'), 'xml')
  assert.equal(languageFor('global.json'), 'json')
  assert.equal(languageFor('README.md'), 'markdown')
  assert.equal(languageFor('tools/check.ps1'), 'powershell')
  assert.equal(languageFor('LICENSE'), 'text')
  assert.equal(languageFor('notes.rst'), 'rst')
})
