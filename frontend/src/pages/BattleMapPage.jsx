import { useState } from 'react'
import PageHeader from '../components/PageHeader.jsx'
import GameMap from '../components/GameMap.jsx'
import AuthGate from '../components/AuthGate.jsx'

export default function BattleMapPage({ activeGame, games, setActiveGameId }) {
  const [selected, setSelected] = useState('')
  const country = activeGame?.countries?.find((item) => item.code === selected) ?? activeGame?.countries?.[0]
  const owner = activeGame?.players?.find((player) => player.id === country?.ownerId)
  return <main className="main">
    <PageHeader eyebrow="OPERATIONAL VIEW · COUNTRY INTELLIGENCE" title="Battle map.">Inspect the countries, owners, and forces in each saved campaign.</PageHeader>
    <AuthGate><div className="route-layout">
      <section className="board"><header className="board-head"><div className="board-title">WORLD THEATRE<small>{activeGame ? 'Round ' + activeGame.round + ' · ' + activeGame.countries.length + ' countries' : 'No campaign selected'}</small></div><select className="game-select" value={activeGame?.id ?? ''} onChange={(event) => setActiveGameId(event.target.value)}>{games.map((game, index) => <option key={game.id} value={game.id}>Operation {String(game.id).slice(-6).toUpperCase() || index + 1}</option>)}</select></header><GameMap game={activeGame} selectedCode={country?.code} onSelect={setSelected} /></section>
      <aside className="side"><section className="side-card"><div className="side-head"><h2>Country intelligence</h2><span>{country?.code ?? 'SELECT'}</span></div><div className="country-intel">{country ? <><span className="intel-code">{country.code}</span><h3>{country.name}</h3><p>{owner?.name ?? 'Unknown commander'} · {owner?.type === 'Human' || owner?.type === 0 ? 'Your country' : 'Rival territory'}</p><div className="intel-stat"><small>SOLDIERS STATIONED</small><b>{country.soldiers ?? 0}</b></div></> : <p>Choose a campaign to see its map.</p>}</div></section><section className="side-card turn-card"><span className="turn-label">CURRENT POSITION</span><h2>{activeGame ? activeGame.phase + ' phase' : 'No operation selected'}</h2><p>{activeGame ? 'Round ' + activeGame.round + '. ' + activeGame.pendingReinforcements + ' reinforcements pending.' : 'Start a campaign to deploy your forces.'}</p><p className="notice">Attack, transfer, and reinforcement commands will appear here when the API supports turn actions.</p></section></aside>
    </div></AuthGate>
  </main>
}
