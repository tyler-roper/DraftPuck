// Size the app shell to the part of the screen you can see. Some browsers (notably iOS 26 home-screen apps) report the
// wrong height at launch and only correct it after a resize, so re-measure on every event that can change it.
//
// With the on-screen keyboard open, the visible part (the visual viewport) ends at the keyboard: sizing the app to it
// keeps the chat box just above the keyboard, with nothing else needed. (iOS Safari keeps window.innerHeight at the full
// height while its keyboard is up; the app used to pad the page as well, which pushed everything under the keyboard.)
(function () {
  var root = document.documentElement
  var vv = window.visualViewport
  var settleTimers = []

  function visibleHeight() {
    // Scaled back up, so pinch-zooming in doesn't count as a smaller screen.
    return vv ? Math.round(vv.height * vv.scale) : window.innerHeight
  }

  // The on-screen keyboard is only up while something you type into has focus.
  function isTyping() {
    var el = document.activeElement
    if (!el) return false
    if (el.isContentEditable) return true
    var tag = el.tagName
    if (tag === 'TEXTAREA' || tag === 'SELECT') return true
    if (tag !== 'INPUT') return false
    var type = (el.getAttribute('type') || 'text').toLowerCase()
    return ['button', 'checkbox', 'color', 'file', 'hidden', 'image', 'radio', 'range', 'reset', 'submit'].indexOf(type) === -1
  }

  // Where the app should sit in the page. With the keyboard up, iOS pans the visible area down inside the page instead
  // of scrolling it, and window.scrollTo can't undo that: the app moves to where the visible area is, so it stays in view
  // (pageTop is the visible area's place in the whole page, which also holds if iOS scrolls the page as well as panning).
  //
  // Only then. Otherwise the app stays at the top: following an ordinary scroll or bounce of the page made the page
  // taller by as much as it moved, so it could scroll on forever, and the app slid off the screen on the way back.
  function appTop() {
    return vv && isTyping() ? Math.max(0, Math.round(vv.pageTop)) : 0
  }

  function setAppHeight() {
    var top = appTop()
    root.style.setProperty('--app-height', visibleHeight() + 'px')
    root.style.setProperty('--app-top', top + 'px')
    // Undo any scroll of the page itself once the keyboard is gone: the app is the size of the screen, and only the
    // panels inside it scroll.
    if (top === 0 && (window.scrollY || window.pageYOffset)) window.scrollTo(0, 0)
  }

  // The keyboard animates in and out, and the events can fire mid-way: measure now, then again once it has settled.
  function measureAndSettle() {
    setAppHeight()
    settleTimers.forEach(clearTimeout)
    settleTimers = [150, 400, 800].map(function (ms) { return setTimeout(setAppHeight, ms) })
  }

  setAppHeight()
  ;['resize', 'orientationchange', 'pageshow', 'load', 'focusout'].forEach(function (e) { window.addEventListener(e, measureAndSettle) })
  document.addEventListener('visibilitychange', function () { if (!document.hidden) measureAndSettle() })
  if (vv) {
    vv.addEventListener('resize', measureAndSettle)
    vv.addEventListener('scroll', setAppHeight)
  }
  setTimeout(setAppHeight, 300)
  setTimeout(setAppHeight, 1000)
})()
