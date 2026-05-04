import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import windowsHardeningMarkdown from '@repoDocs/windows-hardening.md?raw'

export function HardeningPage() {
  return (
    <div className="doc-page">
      <h1 className="page-title">Windows hardening</h1>
      <p className="page-lead">
        Preview of <code className="inline-code">docs/windows-hardening.md</code> — making Phase 1 harder to
        bypass. Edits to the file in the repo appear here after refresh or rebuild.
      </p>
      <article className="markdown-doc">
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          components={{
            a: ({ node, children, ...props }) => {
              void node
              return (
                <a {...props} target="_blank" rel="noopener noreferrer">
                  {children}
                </a>
              )
            },
          }}
        >
          {windowsHardeningMarkdown}
        </ReactMarkdown>
      </article>
    </div>
  )
}
