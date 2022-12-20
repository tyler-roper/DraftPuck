<template>
    <div id="app" style="height: 100%;">
        <router-view></router-view>
    </div>
</template>

<script lang="ts">
    import { Component, Vue } from 'vue-property-decorator';

    @Component
    export default class App extends Vue { }
</script>

<style scoped>
    #app {
        height: 100%;
    }
</style>