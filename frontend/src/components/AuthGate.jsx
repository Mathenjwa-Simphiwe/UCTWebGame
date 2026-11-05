import { useAuth } from '../context/AuthContext.jsx'

export default function AuthGate({ children, message = 'Sign in to view your saved campaigns.' }) {
  const { token } = useAuth()
  if (token) return children
  return <div className="empty"><p>{message}</p><a className="primary button-link" href="#/login">Sign in to RISK</a></div>
}
