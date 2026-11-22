import { describe, expect, it } from 'vitest'
import { lightTeamLogoUrl, teamLogoUrl } from '@/helpers/teamLogos'

describe('teamLogoUrl', () => {
  it("is the team's logo by abbreviation", () => {
    expect(teamLogoUrl({ abbreviation: 'TOR' })).toBe('/img/logos/TOR.png')
  })
})

describe('lightTeamLogoUrl', () => {
  it("is Tampa Bay's light logo, whose usual one is too dark for a dark background", () => {
    expect(lightTeamLogoUrl({ abbreviation: 'TBL' })).toBe('/img/logos/TBL_LIGHT.png')
  })

  it("is every other team's usual logo", () => {
    expect(lightTeamLogoUrl({ abbreviation: 'EDM' })).toBe('/img/logos/EDM.png')
  })
})
