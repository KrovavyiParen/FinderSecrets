<script setup>
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <el-main class="elmain">
    <TheHeader/>
    <RouterView />
    <TheFooter/>
  </el-main>
</template>

<script>
import TheHeader from "./components/TheHeader.vue";
import TheFooter from "./components/TheFooter.vue";

export default {
  components: {
    TheHeader,
    TheFooter
  }
}
</script>

<style scoped>
.elmain {
  min-height: 100vh;
  padding: 0;
}
</style>