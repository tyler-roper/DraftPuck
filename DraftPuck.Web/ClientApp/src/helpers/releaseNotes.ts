/** One area of a release: what changed, with screenshots where they help. */
export type ReleaseSection = {
  title: string
  icon: string
  /** Phone screenshots, in a row that scrolls sideways past two. */
  images?: { src: string; alt: string }[]
  items: { lead: string; text?: string }[]
}

export type Release = {
  /** When it shipped, as a month: '2026-11'. */
  month: string
  title: string
  summary: string
  sections: ReleaseSection[]
}

const image = (file: string) => new URL(`../assets/release-notes/${file}`, import.meta.url).href

/** "November 2026". */
export function releaseMonthName(month: string): string {
  const [year, monthNumber] = month.split('-').map(Number)
  return new Date(Date.UTC(year!, monthNumber! - 1, 1)).toLocaleDateString('en-US', { month: 'long', year: 'numeric', timeZone: 'UTC' })
}

const november2026: ReleaseSection[] = [
  {
    title: 'Friends',
    images: [{ src: image('friends.webp'), alt: 'The Friends page: requests, friends and people you have played with' }],
    icon: 'users-alt',
    items: [
      { lead: 'Friend requests:', text: "add people and accept their requests. If you both ask each other, you're friends straight away." },
      { lead: "Find people you've played with:", text: "a list of everyone you've shared a lobby with, ready to add." },
      { lead: 'Friend links:', text: 'share your profile link (/u/yourname) so people can add you.' },
      {
        lead: 'Invite friends to your lobby',
        text: "from its Share window. Invites wait on your Friends & Invites page, and a badge on your avatar shows when something's waiting."
      },
      { lead: 'Profiles are private:', text: 'your email and settings are only ever shown to you.' }
    ]
  },
  {
    title: 'After the game',
    images: [
      { src: image('awards.webp'), alt: 'Post-game awards' },
      { src: image('standings.webp'), alt: 'Post-game standings' },
      { src: image('stats.webp'), alt: 'Post-game stats for one player' }
    ],
    icon: 'trophy',
    items: [
      { lead: 'A new post-game page', text: 'with Awards, Standings and Stats tabs.' },
      { lead: 'A shareable recap image', text: 'for the group chat.' },
      {
        lead: 'A recap notification at noon Eastern',
        text: 'with your awards from last night. Lobby-end achievements no longer buzz your phone in the middle of the night. You can turn the recap off in settings.'
      }
    ]
  },
  {
    title: 'The lobby feed and chat',
    images: [
      { src: image('feed.webp'), alt: 'The redesigned feed with a goal and its drinks' },
      { src: image('timeline.webp'), alt: 'The drink Timeline' },
      { src: image('drinkmsg.webp'), alt: 'Giving a drink with a message' }
    ],
    icon: 'megaphone',
    items: [
      { lead: 'A darker, clearer feed:', text: 'game events and lobby events now look different.' },
      { lead: 'Each goal links to its drinks,', text: 'and following a link jumps to that goal and highlights it.' },
      { lead: 'A Timeline', text: "that shows at a glance who's handing out drinks and who's taking them." },
      { lead: 'Say something when you hand out a drink.', text: 'Drink messages show in the feed.' },
      { lead: 'Lobby callouts', text: 'with emoji rain, drink banners, and a beer pour when a drink is yours.' },
      { lead: 'About 140 new sayings.', text: 'The feed now opens with who created the lobby.' },
      { lead: 'Bot picks show in the feed,', text: 'one item per game.' },
      { lead: 'Scratched players are named in the feed,', text: 'and whoever picked them gets a notification.' },
      { lead: 'Team logos on "Goal removed" and "Goal changed"', text: 'items, and new feed items slide in.' },
      { lead: 'Better chat @mentions:', text: 'the popup fits its matches and matches the rest of the site.' },
      { lead: 'Long names fit', text: 'without overlapping anything.' }
    ]
  },
  {
    title: 'Achievements',
    icon: 'trophy-star',
    items: [
      {
        lead: '20+ new achievements,',
        text: "each with its own banner: Buzzer Beater, Sudden Death and Empty Netter for your player's big moments; Goose Egg and Designated Driver for a quiet night; Bottomless Cup for the opposite; Chirp Machine for the chat regulars; and Playoff Beard."
      },
      {
        lead: "Fixed achievements that weren't awarded when they should have been.",
        text: "Past games were re-checked, so achievements you'd already earned have been awarded."
      },
      { lead: 'Flip an earned banner or title over', text: 'to see how you earned it.' }
    ]
  },
  {
    title: 'Notifications',
    icon: 'bell',
    items: [
      { lead: "Push notifications reach every device you're signed in on,", text: 'not just the last one.' },
      { lead: 'Notification settings are per lobby,', text: 'with your defaults set in Account Settings.' },
      { lead: '"Time to pick" names the games,', text: 'and no longer arrives twice.' },
      { lead: 'Notifications show while the app is open.' }
    ]
  },
  {
    title: 'Accounts',
    icon: 'user',
    items: [
      { lead: 'Forgot your password?', text: 'You can now reset it by email.' },
      { lead: 'Emails look better,', text: 'with a proper DraftPuck design.' },
      { lead: 'You stay signed in.', text: 'We fixed a bug that could sign you out at startup, and signing out now only signs out that device.' },
      { lead: 'Your profile shows straight away', text: 'when the app opens, instead of briefly showing you as a guest.' },
      {
        lead: 'Straight back into your lobby:',
        text: "open the app mid-game and you land back in tonight's lobby, no rejoining. In more than one? The home page offers a one-tap rejoin."
      }
    ]
  },
  {
    title: 'iPhone and Android',
    icon: 'mobile-notch',
    items: [
      { lead: 'Chat works properly with the iPhone keyboard.' },
      { lead: 'Fixed the page scrolling forever', text: 'and sliding off the screen.' },
      { lead: 'The app icon and status bar are right', text: 'when DraftPuck is added to your iPhone home screen.' },
      { lead: 'Pull-to-refresh is back', text: 'on Android.' },
      { lead: 'The back arrow works', text: 'on pages you opened directly from a link.' }
    ]
  },
  {
    title: 'Live games: faster and more accurate',
    icon: 'hockey-puck',
    items: [
      {
        lead: 'Scores and goals are pushed to you the moment we see them,',
        text: "instead of your device checking every few seconds. Each update only carries what changed, so it's much lighter on your data."
      },
      {
        lead: 'Goal corrections are more dependable:',
        text: "scorer changes, called-back goals and glitches in the NHL's feed are now handled reliably."
      },
      { lead: 'Play times are more accurate,', text: "even for plays that happened while a game briefly wasn't being tracked." },
      { lead: 'Games that stop updating restart themselves', text: 'within a minute or two.' },
      { lead: 'Tabs you have open keep working after we ship an update.' }
    ]
  },
  {
    title: 'Behind the scenes',
    icon: 'gears',
    items: [
      { lead: 'Upgraded to .NET 10,', text: 'and to the latest versions of the tools that build the app.' },
      {
        lead: 'Security improvements:',
        text: 'tighter sign-in and input checks; rate limits on logins, guest accounts and chat; security headers and health checks; and every dependency with a known vulnerability is updated.'
      },
      { lead: 'A new automated test suite', text: 'with over 800 tests, and growing.' },
      { lead: 'Discord:', text: "if your Discord is linked to your DraftPuck account, you're verified automatically when you join the server." }
    ]
  }
]

/** The release notes page, newest first. Add a release at the top; the home page's announcement can point at it. */
export const releases: Release[] = [
  {
    month: '2026-11',
    title: 'The big update',
    summary:
      'Friends, a new post-game page, a redesigned feed, 20+ new achievements, and a lot of work on making live games faster and more reliable, especially on iPhone.',
    sections: november2026
  }
]
