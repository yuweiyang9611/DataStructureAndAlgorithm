/** JSON.parse discards duplicate object keys; reject them before importing v2 ID maps. */
export function parseBackupJson(text: string): unknown {
  const value: unknown = JSON.parse(text)
  let offset = 0
  const whitespace = () => { while (/\s/.test(text[offset] ?? '') && offset < text.length) offset++ }
  function string(): string {
    const start = offset++
    while (offset < text.length) {
      const character = text[offset++]
      if (character === '\\') offset++
      else if (character === '"') return JSON.parse(text.slice(start, offset))
    }
    throw new Error('备份 JSON 字符串无效。')
  }
  function visit() {
    whitespace()
    if (text[offset] === '{') {
      offset++; whitespace()
      const keys = new Set<string>()
      if (text[offset] === '}') { offset++; return }
      while (offset < text.length) {
        whitespace()
        const key = string()
        if (keys.has(key)) throw new Error('备份包含重复字段或单元 ID。')
        keys.add(key); whitespace(); offset++; visit(); whitespace()
        if (text[offset++] === '}') return
      }
    } else if (text[offset] === '[') {
      offset++; whitespace()
      if (text[offset] === ']') { offset++; return }
      while (offset < text.length) {
        visit(); whitespace()
        if (text[offset++] === ']') return
      }
    } else if (text[offset] === '"') string()
    else while (offset < text.length && !/[\s,}\]]/.test(text[offset])) offset++
  }
  visit()
  return value
}
