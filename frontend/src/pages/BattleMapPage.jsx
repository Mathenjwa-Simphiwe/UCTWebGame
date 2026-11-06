import { useEffect, useMemo, useState } from 'react'
import PageHeader from '../components/PageHeader.jsx'
import GameMap from '../components/GameMap.jsx'
import AuthGate from '../components/AuthGate.jsx'
import {
  advanceNpcTurns,
  placeReinforcements,
  resolveAttackRound,
  skipTurn,
  startAttack,
  stopAttack,
  transferTroops,
} from '../api/gameApi.js'

const phases = ['Reinforce', 'Action', 'Attacking']
const statuses = ['Setup', 'InProgress', 'Finished']
const enumName = (value, values) => typeof value === 'number' ? values[value] ?? 'Unknown' : String(value ?? 'Unknown')

export default function BattleMapPage({ activeGame, games, setActiveGameId, saveGame }) {
  const [selected, setSelected] = useState('')
  const [attackFrom, setAttackFrom] = useState('')
  const [attackTo, setAttackTo] = useState('')
  const [transferFrom, setTransferFrom] = useState('')
  const [transferTo, setTransferTo] = useState('')
  const [transferAmount, setTransferAmount] = useState(1)
  const [reinforcementCountry, setReinforcementCountry] = useState('')
  const [battleResult, setBattleResult] = useState(null)
  const [commentary, setCommentary] = useState('')
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const players = activeGame?.players ?? []
  const human = players.find((player) => player.type === 'Human' || player.type === 0)
  const currentPlayerId = activeGame?.turnOrder?.[activeGame.currentTurnIndex]
  const currentPlayer = players.find((player) => player.id === currentPlayerId)
  const humanCountries = useMemo(
    () => (activeGame?.countries ?? []).filter((country) => country.ownerId === human?.id),
    [activeGame, human?.id],
  )
  const enemyCountries = useMemo(
    () => (activeGame?.countries ?? []).filter((country) => country.ownerId !== human?.id),
    [activeGame, human?.id],
  )
  const selectedCountry = activeGame?.countries?.find((country) => country.code === selected) ?? activeGame?.countries?.[0]
  const selectedOwner = players.find((player) => player.id === selectedCountry?.ownerId)
  const phase = enumName(activeGame?.phase, phases)
  const status = enumName(activeGame?.status, statuses)
  const isHumanTurn = currentPlayer?.id === human?.id && status === 'InProgress'
  const canTransfer = humanCountries.length > 1
  const attackCountries = humanCountries.filter((country) => country.soldiers >= 2)
  const effectiveAttackFrom = attackCountries.some((country) => country.code === attackFrom)
    ? attackFrom
    : attackCountries[0]?.code ?? ''
  const effectiveAttackTo = enemyCountries.some((country) => country.code === attackTo)
    ? attackTo
    : enemyCountries[0]?.code ?? ''
  const effectiveTransferFrom = humanCountries.some((country) => country.code === transferFrom)
    ? transferFrom
    : humanCountries[0]?.code ?? ''
  const effectiveTransferTo = humanCountries.some((country) => country.code === transferTo && country.code !== effectiveTransferFrom)
    ? transferTo
    : humanCountries.find((country) => country.code !== effectiveTransferFrom)?.code ?? ''
  const transferSource = humanCountries.find((country) => country.code === effectiveTransferFrom)

  useEffect(() => {
    setSelected('')
    setAttackFrom('')
    setAttackTo('')
    setTransferFrom('')
    setTransferTo('')
    setReinforcementCountry('')
    setBattleResult(null)
    setCommentary('')
    setNotice('')
    setError('')
  }, [activeGame?.id])

  function selectCountry(code) {
    setSelected(code)
    const country = activeGame?.countries?.find((item) => item.code === code)
    if (country?.ownerId === human?.id) {
      setAttackFrom(code)
      setTransferFrom(code)
    } else {
      setAttackTo(code)
      setTransferTo(code)
    }
  }

  async function perform(action, successMessage) {
    if (!activeGame || busy) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = await action(activeGame.id)
      saveGame(response.game)
      setBattleResult(response.roundResult ?? null)
      setCommentary(response.commentary ?? '')
      setNotice(successMessage(response))
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(false)
    }
  }

  const attackCountry = (code) => activeGame?.countries?.find((country) => country.code === code)
  const maxTransfer = Math.max(1, (attackCountry(effectiveTransferFrom)?.soldiers ?? 1) - 1)

  return <main className="main">
    <PageHeader eyebrow="OPERATIONAL VIEW · COUNTRY INTELLIGENCE" title="Battle map.">
      Choose an order, select your forces, and command the campaign. Rival commanders act automatically after your turn.
    </PageHeader>
    <AuthGate><div className="route-layout">
      <section className="board">
        <header className="board-head">
          <div className="board-title">WORLD THEATRE<small>{activeGame ? 'Round ' + activeGame.round + ' · ' + (activeGame.countries?.length ?? 0) + ' countries' : 'No campaign selected'}</small></div>
          <select className="game-select" value={activeGame?.id ?? ''} onChange={(event) => setActiveGameId(event.target.value)} aria-label="Choose campaign">
            {games.map((game, index) => <option key={game.id} value={game.id}>Operation {String(game.id).slice(-6).toUpperCase() || index + 1}</option>)}
          </select>
        </header>
        <GameMap game={activeGame} selectedCode={selectedCountry?.code} onSelect={selectCountry} />
      </section>

      <aside className="side">
        <section className="side-card">
          <div className="side-head"><h2>Country intelligence</h2><span>{selectedCountry?.code ?? 'SELECT'}</span></div>
          <div className="country-intel">
            {selectedCountry ? <>
              <span className="intel-code">{selectedCountry.code}</span>
              <h3>{selectedCountry.name}</h3>
              <p>{selectedOwner?.name ?? 'Unknown commander'} · {selectedOwner?.id === human?.id ? 'Your country' : 'Rival territory'}</p>
              <div className="intel-stat"><small>SOLDIERS STATIONED</small><b>{selectedCountry.soldiers ?? 0}</b></div>
            </> : <p>Choose a campaign to see its map.</p>}
          </div>
        </section>

        <section className="side-card turn-card">
          <span className="turn-label">{status === 'Finished' ? 'CAMPAIGN COMPLETE' : 'ROUND ' + (activeGame?.round ?? '—') + ' · ' + phase.toUpperCase()}</span>
          <h2>{status === 'Finished' ? (activeGame?.winnerId === human?.id ? 'You control the world.' : 'Your campaign has ended.') : currentPlayer ? currentPlayer.id === human?.id ? 'Your turn, Commander.' : currentPlayer.name + ' is taking a turn.' : 'No operation selected'}</h2>
          <p>{activeGame ? isHumanTurn ? phase === 'Reinforce' ? 'Place all ' + activeGame.pendingReinforcements + ' reinforcements to begin your turn.' : phase === 'Attacking' ? 'Roll to resolve the battle, or retreat and end your turn.' : activeGame.attackUsedThisTurn ? 'Your attack is spent. Transfer troops or end your turn.' : 'Attack once, move troops, or end your turn.' : status === 'Finished' ? 'Start another campaign to play again.' : 'Rival commanders are taking their turns.' : 'Start a campaign to deploy your forces.'}</p>

          {activeGame && status === 'InProgress' && !isHumanTurn && <div className="form action-form">
            <button className="primary" disabled={busy} onClick={() => perform(advanceNpcTurns, () => 'Rival commanders finished their turns.')}>{busy ? 'Resolving rival turns…' : 'Continue to my turn'}</button>
          </div>}

          {activeGame && status === 'InProgress' && isHumanTurn && phase === 'Reinforce' && <form className="form action-form" onSubmit={(event) => {
            event.preventDefault()
            perform(
              (id) => placeReinforcements(id, { [reinforcementCountry || humanCountries[0]?.code]: activeGame.pendingReinforcements }),
              () => 'Reinforcements placed.',
            )
          }}>
            <label>Place all {activeGame.pendingReinforcements} soldiers<select value={reinforcementCountry || humanCountries[0]?.code || ''} onChange={(event) => setReinforcementCountry(event.target.value)} required>
              {humanCountries.map((country) => <option key={country.code} value={country.code}>{country.name} · {country.soldiers} soldiers</option>)}
            </select></label>
            <button className="primary" disabled={busy || !humanCountries.length}>{busy ? 'Placing…' : 'Place reinforcements'}</button>
          </form>}

          {activeGame && status === 'InProgress' && isHumanTurn && phase === 'Attacking' && <div className="form action-form">
            <div className="pending-battle"><b>{activeGame.pendingAttack?.attackerCode} attacks {activeGame.pendingAttack?.defenderCode}</b><small>Attacker: {attackCountry(activeGame.pendingAttack?.attackerCode)?.soldiers ?? 0} · Defender: {attackCountry(activeGame.pendingAttack?.defenderCode)?.soldiers ?? 0}</small></div>
            <button className="primary" disabled={busy} onClick={() => perform(resolveAttackRound, (response) => response.roundResult?.conquered ? 'Territory conquered!' : response.roundResult?.attackEnded ? 'The attack has ended.' : 'Dice resolved.')}>{busy ? 'Rolling…' : 'Roll dice'}</button>
            <button className="secondary" disabled={busy} onClick={() => perform(stopAttack, () => 'You retreated. The rival commanders have taken their turns.')}>Retreat and end turn</button>
          </div>}

          {activeGame && status === 'InProgress' && isHumanTurn && phase === 'Action' && <>
            {!activeGame.attackUsedThisTurn && <form className="form action-form" onSubmit={(event) => {
              event.preventDefault()
              perform((id) => startAttack(id, effectiveAttackFrom, effectiveAttackTo), () => 'Attack started. Roll the dice to resolve it.')
            }}>
              <strong className="form-heading">Attack a rival country</strong>
              <label>Attack from<select value={effectiveAttackFrom} onChange={(event) => setAttackFrom(event.target.value)} required>
                {attackCountries.map((country) => <option key={country.code} value={country.code}>{country.name} · {country.soldiers} soldiers</option>)}
              </select></label>
              <label>Target<select value={effectiveAttackTo} onChange={(event) => setAttackTo(event.target.value)} required>
                {enemyCountries.map((country) => <option key={country.code} value={country.code}>{country.name} · {country.soldiers} soldiers</option>)}
              </select></label>
              <button className="primary" disabled={busy || !attackCountries.length || !enemyCountries.length}>{busy ? 'Starting…' : 'Start attack'}</button>
            </form>}

            <form className="form action-form" onSubmit={(event) => {
              event.preventDefault()
              perform((id) => transferTroops(id, effectiveTransferFrom, effectiveTransferTo, Number(transferAmount)), () => 'Soldiers transferred.')
            }}>
              <strong className="form-heading">Transfer soldiers</strong>
              <label>From<select value={effectiveTransferFrom} onChange={(event) => setTransferFrom(event.target.value)} required>
                {humanCountries.map((country) => <option key={country.code} value={country.code}>{country.name} · {country.soldiers} soldiers</option>)}
              </select></label>
              <label>To<select value={effectiveTransferTo} onChange={(event) => setTransferTo(event.target.value)} required>
                {humanCountries.filter((country) => country.code !== effectiveTransferFrom).map((country) => <option key={country.code} value={country.code}>{country.name} · {country.soldiers} soldiers</option>)}
              </select></label>
              <label>Soldiers to move<input type="number" min="1" max={maxTransfer} value={transferAmount} onChange={(event) => setTransferAmount(event.target.value)} required /></label>
              <button className="secondary" disabled={busy || !canTransfer || !transferSource || transferSource.soldiers <= 1 || Number(transferAmount) > maxTransfer}>{busy ? 'Moving…' : 'Transfer soldiers'}</button>
            </form>
            <button className="primary end-turn-button" disabled={busy} onClick={() => perform(skipTurn, () => 'Turn ended. Rival commanders have acted.')}>{busy ? 'Ending turn…' : 'End turn'}</button>
          </>}

          {battleResult && <div className="battle-result" role="status">
            <b>{battleResult.conquered ? 'COUNTRY CONQUERED' : battleResult.attackEnded ? 'ATTACK ENDED' : 'DICE ROUND'}</b>
            <span>Attack dice: {battleResult.attackerDice?.join(', ') || '—'}</span>
            <span>Defence dice: {battleResult.defenderDice?.join(', ') || '—'}</span>
            <span>Losses · you {battleResult.attackerLosses}, rival {battleResult.defenderLosses}</span>
            {battleResult.soldiersGained > 0 && <span>Conquest garrison: {battleResult.soldiersGained}</span>}
            {commentary && <p className="commentary">“{commentary}”</p>}
          </div>}
          {notice && <p className="action-notice">{notice}</p>}
          {error && <p className="error-banner action-error" role="alert">{error}</p>}
        </section>

        <section className="side-card rules-quicklink">
          <div className="side-head"><h2>Turn reminder</h2><span>FIELD MANUAL</span></div>
          <div className="rules-facts"><p><b>Reinforce</b><span>Place every soldier before acting.</span></p><p><b>Attack</b><span>One attack each turn. Roll until you conquer, lose, or retreat.</span></p><p><b>Transfer</b><span>Keep at least one soldier behind.</span></p></div>
          <a className="rules-link" href="#/rules">Read all rules →</a>
        </section>
      </aside>
    </div></AuthGate>
  </main>
}
