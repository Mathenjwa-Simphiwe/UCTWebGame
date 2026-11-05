import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { clearAuthToken, getAuthToken, setAuthToken } from '../middleware/authMiddleware.js'

const AuthContext = createContext(null)
const USER_KEY = 'uct_game_user'

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => getAuthToken())
  const [user, setUser] = useState(() => {
    try { return JSON.parse(sessionStorage.getItem(USER_KEY) || 'null') } catch { return null }
  })
  const signIn = useCallback((auth) => {
    setAuthToken(auth.accessToken)
    sessionStorage.setItem(USER_KEY, JSON.stringify(auth.user))
    setToken(auth.accessToken)
    setUser(auth.user)
  }, [])
  const signOut = useCallback(() => {
    clearAuthToken()
    sessionStorage.removeItem(USER_KEY)
    setToken(null)
    setUser(null)
  }, [])
  const value = useMemo(() => ({ token, user, signIn, signOut }), [token, user, signIn, signOut])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
