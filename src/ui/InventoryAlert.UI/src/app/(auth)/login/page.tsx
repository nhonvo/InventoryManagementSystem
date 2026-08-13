'use client'

export const dynamic = 'force-dynamic'

import { Suspense, useEffect, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { fetchApi } from '@/lib/api'

function LoginForm() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const router = useRouter()
  const searchParams = useSearchParams()
  const registered = searchParams.get('registered')
  const paramUsername = searchParams.get('username')

  useEffect(() => {
    if (paramUsername) {
      setUsername(paramUsername)
    } else {
      const savedUser = localStorage.getItem('remembered_username')
      if (savedUser) {
        setUsername(savedUser)
        setRememberMe(true)
      }
    }
  }, [paramUsername])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const data = await fetchApi('/api/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password, rememberMe }),
      })
      
      const token = data.accessToken
      if (token) {
        localStorage.setItem('auth_token', token)

        if (rememberMe) {
          localStorage.setItem('remembered_username', username)
        } else {
          localStorage.removeItem('remembered_username')
        }

        window.location.href = '/' // Force hard redirect to sync all components
      }
    } catch (err: any) {
      setError(err.message || 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[75vh]">
      <div className="w-full max-w-md p-10 space-y-8 bg-white/60 dark:bg-black/60 backdrop-blur-3xl border border-white/40 dark:border-white/10 rounded-[2.5rem] shadow-2xl dark:shadow-black/50 relative overflow-hidden group">
        <div className="absolute -top-32 -left-32 w-64 h-64 bg-blue-500/10 blur-[80px] rounded-full group-hover:bg-blue-500/20 transition-all duration-1000"></div>
        <div className="text-center relative z-10">
          <h2 className="text-4xl font-semibold text-zinc-900 dark:text-white tracking-tight">Welcome Back</h2>
          <p className="mt-2 text-zinc-500 dark:text-zinc-400 font-medium">Log in to your InventoryAlert account</p>
        </div>

        {registered && (
          <div className="p-4 text-sm bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 rounded-2xl flex items-center gap-3">
            <span className="text-xl">✅</span>
            <div>
              <p className="font-bold">Registration Successful!</p>
              <p className="text-xs text-emerald-400/80">Please log in with your credentials.</p>
            </div>
          </div>
        )}
        
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400 mb-2 ml-1">Username</label>
              <input
                type="text"
                required
                className="w-full px-5 py-4 bg-zinc-100/50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-white/5 rounded-2xl text-zinc-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-zinc-800 transition-all font-bold placeholder:text-zinc-400 dark:placeholder:text-zinc-600 backdrop-blur-sm"
                placeholder="johndoe"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400 mb-2 ml-1">Password</label>
              <input
                type="password"
                required
                className="w-full px-5 py-4 bg-zinc-100/50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-white/5 rounded-2xl text-zinc-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-zinc-800 transition-all font-bold placeholder:text-zinc-400 dark:placeholder:text-zinc-600 backdrop-blur-sm"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>

            <div className="flex items-center justify-between pt-1 px-1">
              <label className="flex items-center gap-3 cursor-pointer select-none group">
                <input
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                  className="w-5 h-5 rounded-lg border border-zinc-300 dark:border-zinc-700 bg-zinc-100 dark:bg-zinc-800 text-blue-600 focus:ring-blue-500 focus:ring-offset-0 transition-all cursor-pointer"
                />
                <span className="text-xs font-medium text-zinc-600 dark:text-zinc-400 group-hover:text-zinc-900 dark:group-hover:text-white transition-colors">
                  Remember me for 30 days
                </span>
              </label>
            </div>
          </div>

          {error && (
            <div className="p-4 text-sm bg-rose-500/10 border border-rose-500/20 text-rose-400 rounded-2xl">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-5 bg-gradient-to-r from-blue-600 to-blue-500 hover:from-blue-500 hover:to-blue-400 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold rounded-2xl shadow-xl shadow-blue-500/20 transition-all transform hover:scale-[1.02] active:scale-[0.98] uppercase tracking-wider text-xs relative z-10"
          >
            {loading ? 'AUTHENTICATING...' : 'SIGN IN SECURELY'}
          </button>

          <div className="text-center text-sm pt-2">
            <span className="text-zinc-500">Don't have an account? </span>
            <Link href="/register" className="text-blue-500 hover:text-blue-400 font-bold transition-colors">
              Create New Account
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}

export default function LoginPage() {
  return (
    <Suspense fallback={<div className="text-white text-center py-20">Loading login session...</div>}>
      <LoginForm />
    </Suspense>
  )
}
