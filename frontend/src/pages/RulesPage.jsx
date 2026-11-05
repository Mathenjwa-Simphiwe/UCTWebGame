import PageHeader from '../components/PageHeader.jsx'

const steps = [
  ['Reinforce', 'From round 2 onward, receive 2 soldiers for each country you own. Place every reinforcement before taking another action.'],
  ['Take action', 'Attack once, transfer soldiers between your own countries, or skip your turn.'],
  ['Resolve a battle', 'Both sides roll dice. Higher rolls inflict losses; ties cause no loss. Combat continues until a country is conquered or the attack ends.'],
  ['Conquer territory', 'A conquered country changes owner and receives a garrison based on its original starting strength.'],
  ['Win the campaign', 'Eliminate opponents by taking their last country. The game ends when one player owns every country; losing the human commander ends the game.'],
]

export default function RulesPage() {
  return <main className="main">
    <PageHeader eyebrow="FIELD MANUAL · RULES REFERENCE" title="Know the rules.">A quick guide to the turn phases and battle mechanics in RISK.</PageHeader>
    <div className="rules-layout"><section className="report-window"><header className="board-head"><div className="board-title">HOW A ROUND WORKS<small>Take your turn, then hand command to the next player</small></div></header><ol className="rules-steps">{steps.map(([title, text]) => <li key={title}><b>{title}</b><span>{text}</span></li>)}</ol></section>
      <aside className="side"><section className="side-card"><div className="side-head"><h2>Quick rules</h2><span>REFERENCE</span></div><div className="rules-facts"><p><b>Starting force</b><span>10 players · 1 country each</span></p><p><b>Reinforcement</b><span>2 soldiers per owned country, from round 2</span></p><p><b>Attack limit</b><span>1 attack per turn</span></p><p><b>Keep a garrison</b><span>At least 1 soldier must remain when transferring</span></p><p><b>Conquest bonus</b><span>At least 2 soldiers, or half the original garrison</span></p></div></section><section className="side-card turn-card"><span className="turn-label">GAME ENGINE</span><h2>Campaign rules are implemented.</h2><p>The current HTTP API saves and loads campaigns. Turn actions are not available through the API yet.</p></section></aside>
    </div>
  </main>
}
