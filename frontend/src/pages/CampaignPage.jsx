import { useState } from 'react'
import PageHeader from '../components/PageHeader.jsx'
import GameMap from '../components/GameMap.jsx'
import AuthGate from '../components/AuthGate.jsx'
import { createGame, startingCountries } from '../api/gameApi.js'

export default function CampaignPage({ games, activeGame, setActiveGameId, refreshGames, saveGame, error }) {
  const [country, setCountry] = useState('RSA')
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState('')
  async function startCampaign(event) {
    event.preventDefault()
    setBusy(true); setNotice('')
    try { saveGame(await createGame(country)); setNotice('Your campaign has been deployed.') }
    catch (e) { setNotice(e.message) }
    finally { setBusy(false) }
  }
  async function refresh() {
    try { await refreshGames(); setNotice('Campaign list refreshed.') }
    catch (e) { setNotice(e.message) }
  }
  return <main className="main">
    <PageHeader eyebrow="CAMPAIGN DESK · SAVED OPERATIONS" title="Your campaign.">Deploy a new game or pick up where your last operation left off.</PageHeader>
    <AuthGate><div className="campaign-layout">
      <section className="board"><header className="board-head"><div className="board-title">THEATRE OF OPERATIONS<small>{activeGame ? 'Operation ' + String(activeGame.id).slice(-6).toUpperCase() : 'No campaign selected'}</small></div><div className="board-tools"><span className="phase-tag">{activeGame ? activeGame.phase : 'STANDBY'}</span></div></header><GameMap game={activeGame} />{activeGame && <div className="campaign-command"><span>Ready for orders?</span><a className="primary button-link" href="#/battle-map">Open battle map</a></div>}</section>
      <aside className="side">
        <section className="side-card"><div className="side-head"><h2>Choose a campaign</h2><span>{games.length} SAVED</span></div><div className="campaign-list">{games.length ? games.map((game, index) => <button key={game.id} className={'campaign-choice ' + (game.id === activeGame?.id ? 'is-active' : '')} onClick={() => setActiveGameId(game.id)}><strong>Operation {String(game.id).slice(-6).toUpperCase() || index + 1}</strong><small>Round {game.round} · {game.status}</small></button>) : <p className="side-note">No saved games yet. Deploy your first campaign below.</p>}</div></section>
        <section className="side-card turn-card"><span className="turn-label">NEW OPERATION</span><h2>Choose your opening position.</h2><form className="form" onSubmit={startCampaign}><label>Starting country<select value={country} onChange={(event) => setCountry(event.target.value)}>{startingCountries.map(([code, name]) => <option key={code} value={code}>{name} · {code}</option>)}</select></label><button className="primary" disabled={busy}>{busy ? 'Deploying…' : 'Start a campaign'}</button></form></section>
        <button className="secondary refresh-button" onClick={refresh}>Refresh saved campaigns</button>
      </aside>
    </div></AuthGate>
    {(notice || error) && <p className={notice.includes('deployed') || notice.includes('refreshed') ? 'notice-banner' : 'error-banner'}>{notice || error}</p>}
  </main>
}
