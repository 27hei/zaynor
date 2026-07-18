interface WordmarkProps {
  className?: string
}

/** "ZAYNOR" with the brand's signature dot centered in the O. */
export function Wordmark({ className }: WordmarkProps) {
  return (
    <span className={className}>
      ZAYN
      <span className="wordmark-o">O</span>
      R
    </span>
  )
}
