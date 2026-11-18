/** Another player in a friends list: enough to draw them. */
interface Friend {
  id: string
  nickname: string
  avatarPath?: string
  banner?: Banner
  title?: Title
}

/** An invite to a lobby, as the person invited sees it. */
interface LobbyInvite {
  joinCode: string
  sender: Friend
  gameCount: number
  memberCount: number
  created: Date
}

/** The current user's friends, friend requests both ways, and pending lobby invites. */
interface FriendsList {
  friends: Friend[]
  incoming: Friend[]
  outgoing: Friend[]
  invites: LobbyInvite[]
}
