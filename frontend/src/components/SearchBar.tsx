import type { FormEvent } from 'react'

interface SearchBarProps {
  value: string
  onChange: (value: string) => void
  onSearch: (query: string) => void
  disabled?: boolean
}

export function SearchBar({ value, onChange, onSearch, disabled }: SearchBarProps) {
  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = value.trim()
    if (trimmed) {
      onSearch(trimmed)
    }
  }

  return (
    <form className="search-bar" onSubmit={handleSubmit} role="search">
      <input
        type="search"
        className="search-input"
        placeholder="Search for a product, e.g. Sony PlayStation 5"
        aria-label="Search for a product"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
      />
      <button type="submit" className="search-button" disabled={disabled}>
        Search
      </button>
    </form>
  )
}
