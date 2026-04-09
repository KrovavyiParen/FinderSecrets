<script setup>
import { ref, onMounted } from 'vue'
import { RouterView } from 'vue-router'
import TheHeader from "./components/TheHeader.vue"
import TheFooter from "./components/TheFooter.vue"

const isAuthorized = ref(false)
const isLoading = ref(true)

onMounted(async () => {
  try {
    const response = await fetch('/api/SecretsFinder/health')
    
    if (response.status === 401) {
      const retryResponse = await fetch('/api/SecretsFinder/health')
      
      if (retryResponse.ok) {
        isAuthorized.value = true
      }
    } else if (response.ok) {
      isAuthorized.value = true
    }
  } catch (err) {
    console.error('Ошибка:', err)
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div v-if="isLoading">
    Загрузка...
  </div>
  
  <el-main v-else-if="isAuthorized" class="elmain">
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