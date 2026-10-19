'use client'

import { useState, useEffect } from 'react'
import { usePathname } from 'next/navigation'
import Navbar from './Navbar'
import { GlobalSearch } from './GlobalSearch'

export default function NavbarWrapper() {
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const pathname = usePathname()

  // Global keyboard shortcut: Ctrl/Cmd+K
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        setIsSearchOpen(true)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  // Do not render navbar or search on authentication pages
  if (pathname === '/login' || pathname === '/register') {
    return null
  }

  return (
    <>
      <Navbar onSearchOpen={() => setIsSearchOpen(true)} />
      <GlobalSearch isOpen={isSearchOpen} onClose={() => setIsSearchOpen(false)} />
    </>
  )
}
