import request from './client.js'

export const login = (credentials) => request('/auth/login', {
  method: 'POST', body: JSON.stringify(credentials), skipAuth: true,
})
export const register = (details) => request('/auth/register', {
  method: 'POST', body: JSON.stringify(details), skipAuth: true,
})
export const getGames = () => request('/games')
export const createGame = (chosenCountryCode) => request('/games', {
  method: 'POST', body: JSON.stringify({ chosenCountryCode }),
})
const gameAction = (gameId, action, body) => request('/games/' + gameId + '/' + action, {
  method: 'POST',
  ...(body === undefined ? {} : { body: JSON.stringify(body) }),
})
export const placeReinforcements = (gameId, allocation) =>
  gameAction(gameId, 'reinforcements', { allocation })
export const startAttack = (gameId, attackerCode, defenderCode) =>
  gameAction(gameId, 'attack', { attackerCode, defenderCode })
export const resolveAttackRound = (gameId) => gameAction(gameId, 'attack/roll')
export const stopAttack = (gameId) => gameAction(gameId, 'attack/stop')
export const transferTroops = (gameId, fromCode, toCode, amount) =>
  gameAction(gameId, 'transfer', { fromCode, toCode, amount })
export const skipTurn = (gameId) => gameAction(gameId, 'skip')
export const advanceNpcTurns = (gameId) => gameAction(gameId, 'advance-npcs')
export const phaseLabel = (value) =>
  typeof value === 'number' ? ['Reinforce', 'Action', 'Attacking'][value] ?? 'Unknown' : value ?? 'Unknown'
export const statusLabel = (value) =>
  typeof value === 'number' ? ['Setup', 'In progress', 'Finished'][value] ?? 'Unknown' : value ?? 'Unknown'

export const startingCountries = [
  ['RSA', 'South Africa'], ['USA', 'United States of America'], ['AUS', 'Australia'],
  ['IND', 'India'], ['CHN', 'China'], ['BRA', 'Brazil'], ['RUS', 'Russia'],
  ['CAN', 'Canada'], ['FRA', 'France'], ['DEU', 'Germany'], ['JPN', 'Japan'],
  ['GBR', 'United Kingdom'], ['ITA', 'Italy'], ['ESP', 'Spain'], ['MEX', 'Mexico'],
  ['KOR', 'South Korea'], ['TUR', 'Turkey'], ['SAU', 'Saudi Arabia'],
  ['ARG', 'Argentina'], ['NGA', 'Nigeria'], ['EGY', 'Egypt'], ['IRN', 'Iran'], ['PAK', 'Pakistan'],
]
