import PageHeader from '../components/PageHeader.jsx'
import { useAuth } from '../context/AuthContext.jsx'

const highlights = [
  ['01', 'Claim territory', 'Choose a starting country and build your presence across the world.'],
  ['02', 'Plan each turn', 'Reinforce your positions, attack once, or move troops between your countries.'],
  ['03', 'Take the world', 'Defeat rival commanders and become the last power standing.'],
]

export default function HomePage({ games, activeGame, error }) {
  const { user } = useAuth()
  const countries = activeGame?.countries ?? []
  const human = activeGame?.players?.find((player) => player.type === 'Human' || player.type === 0)
  const territories = countries.filter((country) => country.ownerId === human?.id).length
  return <main className="main">
    <PageHeader eyebrow="WORLD STRATEGY · COMMAND BRIEF" title="RISK" actions={<><a className="primary button-link" href={user ? '#/campaign' : '#/register'}>{user ? 'Continue campaign' : 'Join the campaign'}</a><a className="secondary button-link" href="#/rules">Read the rules</a></>}>
      A turn-based campaign of territory, timing, and calculated attacks. Build your forces, protect your borders, and outlast every rival commander.
    </PageHeader>
    <section className="home-grid">
      <article className="board overview-card"><header className="board-head"><div className="board-title">THE OBJECTIVE<small>One world. One winning commander.</small></div></header><div className="overview-copy"><p>RISK is a strategy game about controlling countries, growing your army, and deciding when to fight. Every turn asks you to weigh expansion against defence.</p><p>Start a campaign, receive a country, and face a field of rival commanders. Capture their final country to eliminate them. Control the whole map to win.</p><a className="text-link" href="#/campaign">Open campaign desk →</a></div></article>
      <aside className="metric-stack"><div className="metric"><span className="metric-icon">♜</span><small>YOUR CAMPAIGNS</small><strong>{user ? games.length : '—'}</strong><span>saved under your account</span></div><div className="metric"><span className="metric-icon">◎</span><small>COUNTRIES HELD</small><strong>{user && activeGame ? territories : '—'}</strong><span>{activeGame ? 'in the active campaign' : 'start a game to begin'}</span></div><div className="metric"><span className="metric-icon">⌖</span><small>ACTIVE ROUND</small><strong>{activeGame ? activeGame.round : '—'}</strong><span>{activeGame ? activeGame.phase + ' phase' : 'awaiting deployment'}</span></div></aside>
    </section>
    <section className="board how-to"><header className="board-head"><div className="board-title">HOW TO PLAY<small>Your route to global control</small></div><a className="board-link" href="#/rules">Full rules →</a></header><div className="steps-grid">{highlights.map(([number, title, description]) => <article key={number} className="step-card"><span>{number}</span><h2>{title}</h2><p>{description}</p></article>)}</div></section>
    {error && <p className="error-banner">{error}</p>}
    <section className="home-links"><a href="#/battle-map">Battle map <span>Inspect territories and forces →</span></a><a href="#/field-reports">Field reports <span>Review saved campaigns →</span></a><a href="#/rules">Rules reference <span>Learn phases and combat →</span></a></section>
  </main>
}
