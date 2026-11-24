import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

/** Корень репозитория, вычисленный от расположения хелпера. */
export const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../../..')

/** Читает текстовый файл репозитория по пути относительно корня. */
export function read(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8')
}
