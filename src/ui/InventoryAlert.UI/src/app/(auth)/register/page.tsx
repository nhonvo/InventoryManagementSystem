'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { fetchApi } from '@/lib/api'

export default function RegisterPage() {
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    
    if (password !== confirmPassword) {
      setError('Passwords do not match. Please verify.')
      return
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters long.')
      return
    }

    setLoading(true)

    try {
      await fetchApi('/api/v1/auth/register', {
        method: 'POST',
        body: JSON.stringify({ username, email, password }),
      })

      router.push(`/login?registered=true&username=${encodeURIComponent(username)}`)
    } catch (err: any) {
      setError(err.message || 'Registration failed. Username may already be taken.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[80vh]">
      <div className="w-full max-w-md p-10 space-y-8 bg-white/60 dark:bg-black/60 backdrop-blur-3xl border border-white/40 dark:border-white/10 rounded-[2.5rem] shadow-2xl dark:shadow-black/50 relative overflow-hidden group">
        <div className="absolute -top-32 -right-32 w-64 h-64 bg-emerald-500/10 blur-[80px] rounded-full group-hover:bg-emerald-500/20 transition-all duration-1000"></div>
        
        <div className="text-center relative z-10">
          <h2 className="text-4xl font-semibold text-zinc-900 dark:text-white tracking-tight">Create Account</h2>
          <p className="mt-2 text-zinc-500 dark:text-zinc-400 font-medium">Join InventoryAlert for real-time tracking</p>
        </div>
        
        <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
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
              <label className="block text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400 mb-2 ml-1">Email Address</label>
              <input
                type="email"
                required
                className="w-full px-5 py-4 bg-zinc-100/50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-white/5 rounded-2xl text-zinc-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-zinc-800 transition-all font-bold placeholder:text-zinc-400 dark:placeholder:text-zinc-600 backdrop-blur-sm"
                placeholder="john@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
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

            <div>
              <label className="block text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400 mb-2 ml-1">Confirm Password</label>
              <input
                type="password"
                required
                className="w-full px-5 py-4 bg-zinc-100/50 dark:bg-zinc-800/50 border border-zinc-200 dark:border-white/5 rounded-2xl text-zinc-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-zinc-800 transition-all font-bold placeholder:text-zinc-400 dark:placeholder:text-zinc-600 backdrop-blur-sm"
                placeholder="••••••••"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />
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
            className="w-full py-5 bg-gradient-to-r from-emerald-600 to-teal-500 hover:from-emerald-500 hover:to-teal-400 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold rounded-2xl shadow-xl shadow-emerald-500/20 transition-all transform hover:scale-[1.02] active:scale-[0.98] uppercase tracking-wider text-xs relative z-10"
          >
            {loading ? 'REGISTERING ACCOUNT...' : 'REGISTER NEW ACCOUNT'}
          </button>

          <div className="text-center text-sm pt-2">
            <span className="text-zinc-500">Already have an account? </span>
            <Link href="/login" className="text-blue-500 hover:text-blue-400 font-bold transition-colors">
              Log In Here
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
