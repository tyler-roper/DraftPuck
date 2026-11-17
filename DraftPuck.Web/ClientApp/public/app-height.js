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

  function setAppHeight() {
    root.style.setProperty('--app-height', visibleHeight() + 'px')
    // With the keyboard up, iOS pans the visible area down inside the page instead of scrolling it, and window.scrollTo
    // can't undo that. Move the app to where the visible area is, so it stays in view, rather than fighting the pan
    // (which made the page jump, cut off the header and left a gap above the keyboard). pageTop is the visible area's
    // place in the whole page, so this also holds if iOS scrolls the page as well as panning it.
    root.style.setProperty('--app-top', (vv ? Math.max(0, Math.round(vv.pageTop)) : 0) + 'px')
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
