<template>

    <div style="overflow-y: scroll;" class="bg-stone-300 text-stone-800 d-flex flex-column">
        <div class="bg-stone-150 p-3 ls-2 d-flex align-items-center" style="z-index: 2; position: sticky; top: 0; border-bottom: 1px solid rgba(0,0,0,0.1)">
            <div class="fs-3 mr-2">
                🏒
            </div>
            <div>
                <span class="d-block mb-n2">Lobby</span>
                <span class="fs-4 font-weight-bold d-block text-uppercase">{{ lobby.joinCode }}</span>
            </div>
            <div class="ml-auto" v-if="currentUserIsAdmin">
                <a role="button" class="p-3 text-stone-400 d-block m-n3" style="text-decoration: none !important;"><i class="fs-3 fi fi-sr-settings mb-n2 d-block"></i></a>
            </div>
        </div>
        <div class="bg-stone-100">
            <b-dropdown v-for="member in lobby.members" :key="member.id" role="button" class="member-link text-stone-700 text-decoration-none d-flex fs-6" no-caret>
                <template #button-content>
                    <i v-if="lobby.createdBy !== member.userId && !member.isBot" class="fi fi-sr-user mr-2 d-block text-blue" style="margin-top: 2px; height: 1px;"></i>
                    <i v-if="member.isBot" class="fi fi-sr-pulse mr-2 d-block text-stone-700" style="margin-top: 2px; height: 1px;"></i>
                    <i v-if="lobby.createdBy === member.userId" class="fi fi-sr-crown mr-2 d-block text-amber-500" style="margin-top: 2px; height: 1px;"></i>

                    <span class="d-block" :class="{ 'font-weight-bold': currentUserId === member.userId }">{{ member.name }}</span>

                    <span class="d-flex ml-auto" style="cursor: default;">
                        <span class="d-block" style="width: 50px;" v-b-tooltip.hover title="Drinks Not Yet Assigned">
                            <span>🚨</span>
                            <span class="font-weight-bold">{{ getPendingDrinksByMember(member).length }}</span>
                        </span>

                        <span class="d-block" style="width: 50px;" v-b-tooltip.hover title="Drinks Given">
                            <span>🍻</span>
                            <span class="font-weight-bold">{{ getDrinksGivenByMember(member).length }}</span>
                        </span>

                        <span class="d-block" style="width: 50px;" v-b-tooltip.hover title="Drinks Taken">
                            <span v-if="!member.isBot">
                                <span>🍺</span>
                                <span class="font-weight-bold">{{ getDrinksTakenByMember(member).length }}</span>
                            </span>
                        </span>
                    </span>
                </template>
                <template v-if="isCurrentMember(member)">
                    <!--<b-dropdown-item role="button" class="font-weight-bold">Change Name</b-dropdown-item>-->
                </template>
                <template v-if="!isCurrentMember(member)">
                    <b-dropdown-item v-if="getPendingDrinksForCurrentMember().length > 0 && !member.isBot" role="button" variant="blue" @click="giveDrink(member)">Give a drink!</b-dropdown-item>
                    <!--<b-dropdown-item v-if="isLobbyAdmin && member.isBot" role="button">Change Settings</b-dropdown-item>
                    <b-dropdown-item v-if="isLobbyAdmin" role="button" variant="primary">Remove</b-dropdown-item>-->
                </template>
            </b-dropdown>
        </div>
    </div>

</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';
    import { mapState, mapGetters, mapActions } from 'vuex';

    @Component({
        computed: {
            ...mapState('lobby', ['lobby', 'currentUserId']),
            ...mapGetters('lobby', ['isLobbyAdmin'])
        },
        methods: {
            ...mapActions('lobby', ['assignDrink'])
        }
    })
    export default class LobbyOverview extends Vue {
        lobby!: Lobby;
        currentUserId!: string;
        isLobbyAdmin!: boolean;
        assignDrink!: (args: { drink: Drink; recipient: LobbyMember }) => Promise<void>;

        getPendingDrinksByMember(member: LobbyMember) {
            return member.picks.flatMap(p => p.drinks.filter(d => d.recipientLobbyMemberId === null));
        }

        getPendingDrinksForCurrentMember() {
            const member = this.lobby.members.find(m => m.userId === this.currentUserId);
            if (!member) return [];

            return member.picks.flatMap(p => p.drinks.filter(d => d.recipientLobbyMemberId === null));
        }

        getDrinksGivenByMember(member: LobbyMember) {
            return member.picks.flatMap(p => p.drinks.filter(d => d.recipientLobbyMemberId != null));
        }

        getDrinksTakenByMember(member: LobbyMember) {
            return (this.lobby as Lobby).members.flatMap(m => m.picks.flatMap(p => p.drinks)).filter(d => d.recipientLobbyMemberId === member.id);
        }

        isCurrentMember(member: LobbyMember) {
            return this.currentUserId === member.userId;
        }

        async giveDrink(recipient: LobbyMember) {
            const pendingDrinks = this.getPendingDrinksForCurrentMember();
            if (pendingDrinks.length === 0) return;
            const drink = pendingDrinks[0];
            await this.assignDrink({ drink, recipient });
        }

        get currentUserIsAdmin() {
            return this.lobby.createdBy === this.currentUserId;
        }
    }
</script>

<style lang="scss">
    .btn.dropdown-toggle.btn-secondary {
        display: flex !important;
        padding: 10px !important;
        font-size: 0.9rem;
    }

    .dropdown-menu.show { 
        padding: 0 !important;
    }

    .dropdown-item {
        padding: 10px 5px !important;
    }
</style>