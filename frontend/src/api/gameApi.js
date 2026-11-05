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

export const startingCountries = [
  ['RSA', 'South Africa'], ['USA', 'United States of America'], ['AUS', 'Australia'],
  ['IND', 'India'], ['CHN', 'China'], ['BRA', 'Brazil'], ['RUS', 'Russia'],
  ['CAN', 'Canada'], ['FRA', 'France'], ['DEU', 'Germany'], ['JPN', 'Japan'],
  ['GBR', 'United Kingdom'], ['ITA', 'Italy'], ['ESP', 'Spain'], ['MEX', 'Mexico'],
  ['KOR', 'South Korea'], ['TUR', 'Turkey'], ['SAU', 'Saudi Arabia'],
  ['ARG', 'Argentina'], ['NGA', 'Nigeria'], ['EGY', 'Egypt'], ['IRN', 'Iran'], ['PAK', 'Pakistan'],
]
