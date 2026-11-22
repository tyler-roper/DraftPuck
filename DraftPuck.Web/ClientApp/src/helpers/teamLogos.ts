/** A team's logo, for a light background. */
export function teamLogoUrl(team: Pick<Team, 'abbreviation'>): string {
  return `/img/logos/${team.abbreviation}.png`
}

/** A team's logo, for a dark background (or the team's color). Only Tampa Bay's needs a light version. */
export function lightTeamLogoUrl(team: Pick<Team, 'abbreviation'>): string {
  return team.abbreviation === 'TBL' ? `/img/logos/${team.abbreviation}_LIGHT.png` : teamLogoUrl(team)
}
