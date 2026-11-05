import { useState } from 'react'
import { login, register } from '../api/gameApi.js'
import { useAuth } from '../context/AuthContext.jsx'
import PageHeader from '../components/PageHeader.jsx'

export default function AuthPage({ mode }) {
  const registering = mode === 'register'
  const { signIn } = useAuth()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  async function submit(event) {
    event.preventDefault(); setBusy(true); setError('')
    const form = new FormData(event.currentTarget)
    const credentials = { email: email.trim(), password }
    try {
      const result = registering ? await register({ ...credentials, username: String(form.get('username')).trim() }) : await login(credentials)
      signIn(result)
      window.location.hash = '#/campaign'
    } catch (requestError) { setError(requestError.message) }
    finally { setBusy(false) }
  }
  return <main className="main auth-main"><PageHeader eyebrow="COMMANDER ACCESS" title={registering ? 'Join RISK.' : 'Welcome back.'}>{registering ? 'Create your commander account to save campaign progress.' : 'Sign in to resume your campaigns and review your reports.'}</PageHeader>
    <section className="auth-card"><header className="board-head"><div className="board-title">{registering ? 'CREATE ACCOUNT' : 'COMMANDER SIGN IN'}<small>Your campaign data is tied to your account</small></div></header><form className="form auth-form" onSubmit={submit}>{registering && <label>Commander name<input name="username" autoComplete="username" minLength="3" maxLength="50" required /></label>}<label>Email address<input name="email" type="email" autoComplete="email" maxLength="254" value={email} onChange={(event) => setEmail(event.target.value)} required /></label><label>Password<input name="password" type="password" autoComplete={registering ? 'new-password' : 'current-password'} minLength={registering ? 8 : 1} maxLength="128" value={password} onChange={(event) => setPassword(event.target.value)} required /></label><button type="submit" className="primary" disabled={busy}>{busy ? 'Please wait…' : registering ? 'Create account' : 'Sign in'}</button>{error && <p className="error-banner" role="alert">{error}</p>}</form>{!registering && import.meta.env.DEV && <section className="demo-access"><div className="demo-access-head"><span>LOCAL DEMO ACCOUNTS</span><span>DEVELOPMENT ONLY</span></div><div className="demo-access-list"><button type="button" className="demo-account" onClick={() => { setEmail('commander@frontline.demo'); setPassword('FrontlineDemo1!') }}>Commander<small>commander@frontline.demo<br />Fill demo credentials</small></button><button type="button" className="demo-account" onClick={() => { setEmail('scout@frontline.demo'); setPassword('FrontlineDemo2!') }}>Scout<small>scout@frontline.demo<br />Fill demo credentials</small></button></div><div className="demo-note">Local test accounts · click to fill the sign-in form</div></section>}<div className="auth-switch">{registering ? 'Already have an account?' : 'New commander?'} <a href={registering ? '#/login' : '#/register'}>{registering ? 'Sign in' : 'Create an account'}</a></div></section>
  </main>
}
