import type { Plugin, ResolvedConfig } from 'vite'
import path from 'path'
import fs from 'fs'

type MoveOption = {
  source: string
  dest: string
}

type UserOptions = MoveOption[]

export default function move(userOptions: UserOptions): Plugin {
  let config: ResolvedConfig

  return {
    name: 'move',
    configResolved(resolvedConfig) {
      config = resolvedConfig
    },
    writeBundle() {
      const root = config.root || process.cwd()
      const resolve = (p: string) => path.resolve(root, p)

      userOptions.forEach((moveOptions) => {
        if (!fs.existsSync(path.dirname(moveOptions.dest))) {
          fs.mkdirSync(path.dirname(moveOptions.dest), { recursive: true })
        }

        fs.rename(resolve(moveOptions.source), resolve(moveOptions.dest), () => {})
      })
    }
  }
}
