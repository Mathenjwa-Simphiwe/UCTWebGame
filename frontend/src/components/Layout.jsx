import { useAuth } from '../context/AuthContext.jsx'

const links = [
  ['/home', 'Home'], ['/campaign', 'Campaign'], ['/battle-map', 'Battle map'],
  ['/field-reports', 'Field reports'], ['/rules', 'Rules'],
]

export default function Layout({ route, children }) {
  const { user, signOut } = useAuth()
  const authPage = route === '/login' || route === '/register'
  return <div className="site-shell">
    <header className="topbar">
      <a className="brand" href="#/home" aria-label="RISK home"><span className="brand-emblem">R</span><span>RISK<small>GLOBAL DOMINATION</small></span></a>
      {!authPage && <nav className="topnav" aria-label="Main navigation">{links.map(([path, label]) => <a key={path} href={'#' + path} aria-current={route === path ? 'page' : undefined}>{label}</a>)}</nav>}
      <div className="account">{user ? <><span>{user.username}</span><span className="avatar">{user.username.slice(0, 2).toUpperCase()}</span><button className="secondary compact" onClick={signOut}>Sign out</button></> : <a className="secondary compact" href="#/login">Sign in</a>}</div>
    </header>
    {children}
    <footer className="site-footer">RISK · STRATEGY, ALLIANCES, AND TERRITORY</footer>
  </div>
}
