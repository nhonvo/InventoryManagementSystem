'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useTheme } from './ThemeProvider'
import NotificationBell from './NotificationBell'
import { useNotifications } from './NotificationProvider'

export default function UserNav() {
  const { token } = useNotifications()
  const [mounted, setMounted] = useState(false)
  const router = useRouter()
  const { theme, toggleTheme, mounted: themeMounted } = useTheme()

  useEffect(() => {
    setMounted(true)
  }, [])

  const handleLogout = async () => {
    try {
      if (token) {
        await fetch(`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080"}/api/v1/auth/logout`, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`
          },
          credentials: 'include'
        });
      }
    } catch (e) {
      console.error('Logout failed', e);
    } finally {
      localStorage.removeItem('auth_token');
      // The NotificationProvider will pick up the removal via interval/storage event
      router.push('/login');
    }
  }

  if (!mounted || !themeMounted) return <div className="w-20"></div>

  return (
    <div className="flex items-center gap-6">
      <button 
        onClick={toggleTheme}
        className="p-2 rounded-xl bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-white/5 text-zinc-600 dark:text-zinc-400 hover:text-blue-500 transition-all shadow-sm"
        title={`Switch to ${theme === 'dark' ? 'light' : 'dark'} mode`}
      >
        {theme === 'dark' ? '☀️' : '🌙'}
      </button>

      {!token ? (
        <div className="flex items-center gap-3">
          <Link 
            href="/login" 
            className="text-xs font-bold text-zinc-600 dark:text-zinc-300 hover:text-zinc-900 dark:hover:text-white px-3 py-2 transition-colors"
          >
            Sign In
          </Link>
          <Link 
            href="/register" 
            className="text-xs font-bold bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-500 hover:to-blue-400 text-white px-4 py-2 rounded-xl shadow-lg shadow-blue-500/20 transition-all transform hover:scale-[1.02] active:scale-[0.98]"
          >
            Register
          </Link>
        </div>
      ) : (
        <div className="flex items-center gap-4">
          <NotificationBell />
          <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center font-bold text-xs ring-2 ring-zinc-200 dark:ring-white/10 shadow-lg text-white">
            A
          </div>
          <button 
            onClick={handleLogout}
            className="text-xs font-bold text-zinc-500 hover:text-zinc-900 dark:hover:text-white transition-colors"
          >
            Sign Out
          </button>
        </div>
      )}
    </div>
  )
}
