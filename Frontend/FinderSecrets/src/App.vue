<script setup>
import { RouterLink, RouterView } from 'vue-router'
import { onMounted } from 'vue'
import TheHeader from "./components/TheHeader.vue"
import TheFooter from "./components/TheFooter.vue"

onMounted(() => {
  fetch('/api/SecretsFinder/health')
    .then(response => {
      if (response.status === 401) {
        console.log('Требуется авторизация');
      }
    })
    .catch(err => console.error(err));
})
</script>

<template>
  <el-main class="elmain">
    <TheHeader/>
    <RouterView />
    <TheFooter/>
  </el-main>
</template>

<style scoped>
.elmain {
  min-height: 100vh;
  padding: 0;
}
</style>