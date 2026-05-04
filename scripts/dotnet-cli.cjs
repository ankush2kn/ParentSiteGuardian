/**
 * Runs `dotnet` from the repo root after verifying the SDK is available.
 * Avoids a silent "'dotnet' is not recognized" on Windows when the SDK isn't installed or on PATH.
 */
const { spawnSync } = require('child_process')
const path = require('path')

const root = path.resolve(__dirname, '..')
const dotnetArgs = process.argv.slice(2)

const versionCheck = spawnSync('dotnet', ['--version'], {
  shell: true,
  encoding: 'utf8',
  cwd: root,
})

if (versionCheck.error || versionCheck.status !== 0) {
  console.error('')
  console.error('[Parent Site Guardian] .NET SDK not found.')
  console.error('')
  console.error('  The local API targets .NET 10. Install the SDK (not only the runtime):')
  console.error('  https://dotnet.microsoft.com/download/dotnet/10.0')
  console.error('')
  console.error('  After installing, close this terminal, open a new one, and run:')
  console.error('  npm run dev:service')
  console.error('')
  process.exit(1)
}

const run = spawnSync('dotnet', dotnetArgs, {
  stdio: 'inherit',
  shell: true,
  cwd: root,
})

process.exit(run.status === null ? 1 : run.status)
