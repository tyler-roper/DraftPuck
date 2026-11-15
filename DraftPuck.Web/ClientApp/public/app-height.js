// Size the app shell to the real viewport. Some browsers (notably iOS 26 home-screen apps) report the wrong
// height at launch and only correct it after a resize, so re-measure on every event that can change it.
(function () {
  var root = document.documentElement
  function setAppHeight() { root.style.setProperty('--app-height', window.innerHeight + 'px') }
  setAppHeight()
  ;['resize', 'orientationchange', 'pageshow', 'load'].forEach(function (e) { window.addEventListener(e, setAppHeight) })
  document.addEventListener('visibilitychange', function () { if (!document.hidden) setAppHeight() })
  if (window.visualViewport) window.visualViewport.addEventListener('resize', setAppHeight)
  setTimeout(setAppHeight, 300)
  setTimeout(setAppHeight, 1000)
})()
