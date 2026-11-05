export default function GameMap({ game, selectedCode, onSelect }) {
  if (!game) return <div className="empty">Start a campaign to see its countries and commanders.</div>
  const players = game.players ?? []
  return <div className="map"><div className="map-grid">{(game.countries ?? []).map((country) => {
    const owner = players.find((player) => player.id === country.ownerId)
    const mine = (owner?.type === 'Human' || owner?.type === 0) && country.ownerId === players.find((player) => player.type === 'Human' || player.type === 0)?.id
    return <button className={'territory ' + (mine ? 'mine' : 'enemy') + (selectedCode === country.code ? ' selected' : '')} key={country.code} onClick={() => onSelect?.(country.code)}>
      <span className="code">{country.code}</span><strong>{country.name}</strong><span className="army">♜ {country.soldiers ?? 0} soldiers</span><small>{owner?.name ?? 'Unassigned'}</small>
    </button>
  })}</div><div className="map-foot">Select a country to inspect its current strength and commander.</div></div>
}
