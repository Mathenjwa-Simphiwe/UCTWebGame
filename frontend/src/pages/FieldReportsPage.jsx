import PageHeader from '../components/PageHeader.jsx'
import AuthGate from '../components/AuthGate.jsx'

function dateText(value) {
  if (!value) return 'Unknown date'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Unknown date' : date.toLocaleString()
}

export default function FieldReportsPage({ games, setActiveGameId }) {
  return <main className="main">
    <PageHeader eyebrow="CAMPAIGN ARCHIVE · SAVED OPERATIONS" title="Field reports.">A summary of each campaign saved to your account.</PageHeader>
    <AuthGate><section className="report-window"><header className="board-head"><div className="board-title">OPERATION RECORDS<small>Campaign rounds, territory, and deployment</small></div><span className="round-tag">{games.length} RECORDS</span></header>
      {games.length ? <div className="report-list">{games.map((game, index) => {
        const human = game.players?.find((player) => player.type === 'Human' || player.type === 0)
        const owned = (game.countries ?? []).filter((country) => country.ownerId === human?.id)
        return <article className="report-row" key={game.id}><div className="report-row-main"><b>Operation {String(game.id).slice(-6).toUpperCase() || index + 1}</b><small>{game.status} · Started {dateText(game.createdAt)}</small></div><div className="report-cell">Round {game.round}<small>{game.phase} phase</small></div><div className="report-cell">{owned.length} countries held<small>{game.countries?.length ?? 0} total countries</small></div><div className="report-cell">Last updated<small>{dateText(game.updatedAt)}</small></div><button className="primary" onClick={() => { setActiveGameId(game.id); window.location.hash = '#/battle-map' }}>View map</button></article>
      })}</div> : <div className="empty">No reports yet. Start a campaign to create your first record.<br/><a className="primary button-link" href="#/campaign">Go to campaign desk</a></div>}</section></AuthGate>
  </main>
}
