<script setup lang="ts">
defineProps<{
  joinCode?: string
}>()

const emit = defineEmits(['close'])
const close = () => emit('close')
const stopPropagation = (e: Event) => e.stopPropagation()
</script>

<template>
  <div class="modal-overlay" @click="close">
    <div class="dp-modal" @click="stopPropagation">
      <div class="modal-header">
        <span class='d-block'>How To Play</span>
        <a role="button" class="text-uppercase text-primary text-decoration-none" @click="close">Close</a>
      </div>
      <div class="modal-body">
        <ol class="ms-0 ps-3">
          <li>
            <strong class="text-uppercase">Invite Your Friends</strong>
            <p class="mt-1 mb-0">
              Have your friends join the lobby by entering your lobby code
              <span v-if="joinCode"
                >(<strong>{{ joinCode }}</strong
                >)</span
              >
              at the home screen...
            </p>
            <p class="my-1 ms-2">
              <strong class="text-uppercase">or</strong>
            </p>
            <p>
              ...navigate to the
              <strong><i class="fi fi-rr-users-alt"></i> Lobby</strong> view and click
              <strong class="text-uppercase badge bg-primary text-stone-900">Invite</strong>.
            </p>
          </li>

          <li class="pt-3">
            <strong class="text-uppercase">Pick Your Players</strong>
            <p class="mt-1">
              Once picks become available for a game, click the
              <strong>Rosters<i class="fi fi-rr-caret-right"></i></strong>
              button to see a list of each team's players.
            </p>
            <p class="mt-1">
              Available players will have a
              <strong class="text-uppercase badge bg-primary text-stone-900">Pick</strong>
              button beside their name, whereas players with a username have already been chosen.
            </p>
            <p class="mt-1">Choose carefully! Once you've made your choice, only the lobby creator can undo it for you.</p>
            <p class="mt-1">
              <strong>Playing with bots?</strong> Bots can make their picks as soon as rosters become available. The lobby creator can initiate the
              bot picks by opening the <strong>ROSTERS</strong> and clicking
              <strong class="text-uppercase badge bg-primary text-stone-900">Make Bot Picks</strong>.
            </p>
          </li>

          <li class="pt-3">
            <strong class="text-uppercase">Cross your fingers</strong>
            <p class="mt-1">
              If one of your players scores, you get to make someone drink! When this happens, you'll receive a notification letting you know you've
              got a drink available. On mobile, a badge will also appear beside the
              <strong class="text-uppercase badge bg-stone-900 text-stone-0">Lobby</strong>
              button.
            </p>
            <p class="mt-1">
              Once you've earned a drink, you can dish it out by going to the
              <strong>Lobby</strong> view, clicking the desired recipient's name, and selecting <strong>"Give a drink!"</strong>.
            </p>
            <p class="mt-1">The recipient will receive a notification letting them know that you've sent a drink their way.</p>
            <p class="mt-1">
              Each player's number of drinks <i>pending</i>, drinks <i>given</i>, and drinks <i>taken</i>, are all listed next to their name in the
              lobby view.
            </p>
          </li>

          <li class="pt-3">
            <strong class="text-uppercase">Keep an eye on "the feed"</strong>
            <p class="mt-1">
              Last but certainly not least: <strong>🚨 The Feed</strong> will be your one-stop-shop for all lobby events, from penalties and goals, to
              drinks given and drinks taken.
            </p>
            <p class="mt-1">
              You can customize which events you want to see in your feed by clicking the
              <strong><i class="fi fi-sr-settings"></i> Settings</strong> icon at the top. You can also see a history of all drinks assigned by
              clicking the
              <strong><i class="fi fi-sr-list"></i> Drink Timeline</strong>
              button.
            </p>
          </li>
        </ol>

        <button class="btn btn-primary fw-bold w-100 p-3 mt-4" @click="close">Close</button>
      </div>
    </div>
  </div>
</template>