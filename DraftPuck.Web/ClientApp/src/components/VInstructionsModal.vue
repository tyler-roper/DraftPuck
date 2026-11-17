<script setup lang="ts">
import VIcon from '@/components/VIcon.vue'

defineProps<{
  joinCode?: string
}>()

const emit = defineEmits(['close'])
const close = () => emit('close')
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click.stop>
      <div class="modal-header justify-content-between">
        <span class="d-block">How To Play</span>
        <a role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>

      <div class="modal-body dark">
        <ol class="steps">
          <li class="step">
            <span class="step-number">1</span>
            <div>
              <span class="step-title"><VIcon icon="users-alt" prefix="sr" class="me-2" />Invite your friends</span>
              <p>
                Tap the lobby code at the top (<span class="chip amber">{{ joinCode ?? 'ABCD' }} <VIcon icon="share" prefix="sr" /></span>) to share an
                invite link, or have friends enter the code on the home screen.
              </p>
            </div>
          </li>

          <li class="step">
            <span class="step-number">2</span>
            <div>
              <span class="step-title"><VIcon icon="hockey-sticks" prefix="sr" class="me-2" />Pick your players</span>
              <p>
                Picks open <strong>30 minutes before puck drop</strong>. Open a game's <span class="chip">Rosters</span> and tap
                <span class="chip amber">Pick</span>. Each player can only be picked once per lobby, so first come, first served.
              </p>
              <p>Changed your mind? Remove your pick any time before puck drop. Bots pick for themselves once lineups are out.</p>
            </div>
          </li>

          <li class="step">
            <span class="step-number">3</span>
            <div>
              <span class="step-title"><VIcon icon="beer" prefix="sr" class="me-2" />Score, then pour</span>
              <p>When one of your players scores, you earn a drink to hand out. You'll get a notification, and a badge on the Lobby tab.</p>
              <p>
                In the <span class="chip">Lobby</span> tab, tap someone's name and choose <strong>Give a drink!</strong>, or add a message with it.
                They'll get the full-screen <strong>DRINK!</strong>
              </p>
            </div>
          </li>

          <li class="step">
            <span class="step-number">4</span>
            <div>
              <span class="step-title"><VIcon icon="megaphone" prefix="sr" class="me-2" />Watch the feed</span>
              <p>
                Goals, penalties, drinks and callouts (who's on fire, who's ice cold, the rivalries) as they happen. Choose what shows with the
                feed's settings, and see every drink handed out in its <span class="chip">Timeline</span>.
              </p>
            </div>
          </li>

          <li class="step">
            <span class="step-number">5</span>
            <div>
              <span class="step-title"><VIcon icon="trophy" prefix="sr" class="me-2" />Crown the night</span>
              <p>When the games are over, the post-game page hands out the awards. Share the recap with the group chat.</p>
            </div>
          </li>
        </ol>

        <button class="btn btn-primary fw-bold w-100 py-3 mt-2" @click="close">Let's go</button>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@import '@/assets/scss/custom-colors.scss';

// Dark, like the rest of the app (the shared modal body is white).
.modal-body.dark {
  background: map-get($custom-colors, 'stone-850');
  color: map-get($custom-colors, 'stone-300');
}

.steps {
  list-style: none;
  margin: 0;
  padding: 0;
}

.step {
  display: flex;
  gap: 14px;
  padding: 14px;
  margin-bottom: 10px;
  border-radius: 12px;
  background: map-get($custom-colors, 'stone-900');
  border: 1px solid map-get($custom-colors, 'stone-700');
}

.step-number {
  flex: none;
  display: grid;
  place-items: center;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  font-weight: 800;
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'primary');
}

.step-title {
  display: block;
  margin: 3px 0 6px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: map-get($custom-colors, 'stone-0');
}

.step p {
  margin: 0 0 6px;
  font-size: 0.92rem;
  line-height: 1.45;
}

.step p:last-child {
  margin-bottom: 0;
}

.step strong {
  color: map-get($custom-colors, 'stone-0');
}

// The app's buttons and labels, as they look on screen.
.chip {
  display: inline-block;
  padding: 0 7px;
  border-radius: 5px;
  font-size: 0.8rem;
  font-weight: 800;
  text-transform: uppercase;
  white-space: nowrap;
  color: map-get($custom-colors, 'stone-0');
  background: map-get($custom-colors, 'stone-700');
}

.chip.amber {
  color: map-get($custom-colors, 'stone-900');
  background: map-get($custom-colors, 'primary');
}
</style>
