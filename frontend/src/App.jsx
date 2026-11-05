import { useEffect, useState } from 'react'
import Layout from './components/Layout.jsx'
import { AuthProvider, useAuth } from './context/AuthContext.jsx'
import HomePage from './pages/HomePage.jsx'
import CampaignPage from './pages/CampaignPage.jsx'
import BattleMapPage from './pages/BattleMapPage.jsx'
import FieldReportsPage from './pages/FieldReportsPage.jsx'
import RulesPage from './pages/RulesPage.jsx'
import AuthPage from './pages/AuthPage.jsx'
import { getGames } from './api/gameApi.js'

function RoutedApp() {
  const { token, signOut } = useAuth()
  const [route, setRoute] = useState(window.location.hash.slice(1) || '/home')
  const [games, setGames] = useState([])
  const [activeGameId, setActiveGameId] = useState('')
  const [error, setError] = useState('')
  const activeGame = games.find((game) => game.id === activeGameId) ?? games[0] ?? null

  useEffect(() => {
    const updateRoute = () => setRoute(window.location.hash.slice(1) || '/home')
    window.addEventListener('hashchange', updateRoute)
    return () => window.removeEventListener('hashchange', updateRoute)
  }, [])

  useEffect(() => {
    if (!token) {
      setGames([])
      setActiveGameId('')
      return
    }
    let alive = true
    getGames().then((result) => {
      if (!alive) return
      const list = Array.isArray(result) ? result : []
      setGames(list)
      setActiveGameId((current) => list.some((game) => game.id === current) ? current : list[0]?.id ?? '')
      setError('')
    }).catch((requestError) => {
      if (!alive) return
      setError(requestError.message)
      if (/401|unauthor/i.test(requestError.message)) signOut()
    })
    return () => { alive = false }
  }, [token, signOut])

  const refreshGames = async () => {
    if (!token) return []
    const result = await getGames()
    const list = Array.isArray(result) ? result : []
    setGames(list)
    return list
  }
  const saveGame = (game) => {
    setGames((current) => [game, ...current.filter((item) => item.id !== game.id)])
    setActiveGameId(game.id)
  }

  let page
  if (route === '/login' || route === '/register') page = <AuthPage mode={route.slice(1)} />
  else {
    const props = { games, activeGame, setActiveGameId, refreshGames, saveGame, error }
    switch (route) {
      case '/campaign': page = <CampaignPage {...props} />; break
      case '/battle-map': page = <BattleMapPage {...props} />; break
      case '/field-reports': page = <FieldReportsPage {...props} />; break
      case '/rules': page = <RulesPage />; break
      default: page = <HomePage {...props} />
    }
  }
  return <Layout route={route}>{page}</Layout>
}

export default function App() {
  return <AuthProvider><RoutedApp /></AuthProvider>
}
