import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const outputUrl = new URL('../docs/.vitepress/dist/index.html', import.meta.url)
const outputPath = fileURLToPath(outputUrl)
const html = readFileSync(outputPath, 'utf8')

const layoutClasses = [
  'learning-path',
  'learning-topic-grid',
  'learning-projects',
  'learning-evidence',
  'learning-cta'
]

const missingLayouts = layoutClasses.filter(
  (className) => !html.includes(`class="${className}"`)
)

const escapedLayouts = layoutClasses.filter((className) =>
  html.includes(`&quot;${className}&quot;`)
)

if (missingLayouts.length > 0 || escapedLayouts.length > 0) {
  if (missingLayouts.length > 0) {
    console.error(`Missing rendered homepage layouts: ${missingLayouts.join(', ')}`)
  }

  if (escapedLayouts.length > 0) {
    console.error(`Homepage layouts rendered as text: ${escapedLayouts.join(', ')}`)
  }

  process.exit(1)
}

console.log('Homepage custom layouts rendered as HTML.')
